using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Static: the menus' self-test, on the framework's harness (SelfTest). A built player started
    /// with -aleatoraMenuCheck plays through its own menus: the main menu over the scene, Settings
    /// over it, Play, the session menu and Settings over that, and Quit to Menu. On the way it
    /// checks what the menu shell promises (what is under the top menu can't be used or navigated
    /// to, Escape and the gamepad's East and Start close one menu at a time, the selection comes
    /// back) and that Settings lists no volumes (Aleatora makes no sound), then quits with exit code 1 if anything didn't
    /// hold or any error was logged. -aleatoraScreenshots folder saves a screenshot of each menu. It
    /// saves no setting. Tools/menucheck.sh builds the player and runs it. Without the flag none of
    /// this runs.
    /// </summary>
    public static class MenuCheck
    {
        private const float WaitSeconds = 60f;

        private static bool _arrived;
        private static int _scenesLoaded;

        private static MenuStack Menus => SessionMenu.Menus;

        // Before the first scene loads, so the run answers for everything from the start.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Begin()
        {
            if (!CommandLine.Has("-aleatoraMenuCheck"))
            {
                return;
            }

            SelfTest.Begin();
            SelfTest.ScreenshotFolder = CommandLine.Value("-aleatoraScreenshots");
            QueuedInput.IgnoreFocus();
            _scenesLoaded = 0;
            SceneManager.sceneLoaded += (_, _) => _scenesLoaded++;
            SelfTest.Run(Steps());
        }

        //----------

        private static IEnumerator Steps()
        {
            // The main menu, over the scene.
            yield return WaitFor(() => Menus != null && Menus.Screen != null, "the main menu");
            if (!_arrived)
            {
                yield break;
            }

            yield return null; // its Start has built it; a frame for the layout
            RectTransform mainMenu = Menus.Screen;
            Debug.Log($"Menu check: theme {UiKit.Theme.name}; scene {SceneManager.GetActiveScene().name}; selected at start: {SelectedName()}");
            Debug.Log("Menu check: main menu: " + UiProbe.Texts(mainMenu));
            SelfTest.Check(MainMenu.IsShowing && Menus.Count == 0, "the game starts on the main menu, with no menu over it");
            SelfTest.Check(MenuFocus.IsAnyOpen, "the main menu holds the controls");
            SelfTest.Check(SelectedName() == "Button: Play", "Play has the selection: " + SelectedName());
            CheckNavigation("the main menu", mainMenu);
            yield return SelfTest.Capture("1-MainMenu");

            // Settings over it, closed with Escape.
            Press(mainMenu, "Button: Settings");
            yield return null;
            SelfTest.Check(Menus.Count == 1, "Settings opens over the main menu");
            if (Menus.Count != 1)
            {
                SelfTest.Finish();
                yield break;
            }

            SelfTest.Check(!UiProbe.HasInteractable(mainMenu), "nothing under Settings over the main menu can be used");
            CheckNavigation("Settings over the main menu", Menus.Top);
            Debug.Log("Menu check: Settings: " + UiProbe.Texts(Menus.Top));
            SelfTest.Check(UiProbe.Find<Toggle>(Menus.Top, "Toggle: Fullscreen") != null, "Settings has the Fullscreen toggle");
            // Aleatora makes no sound: no volumes, and so no sliders.
            SelfTest.Check(Settings.Volumes.Count == 0 && Menus.Top.GetComponentsInChildren<Slider>().Length == 0,
                "Settings lists no volumes and has no slider");
            SelfTest.Check(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
                "nothing in the game has an AudioSource");
            yield return SelfTest.Capture("2-Settings");

            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Count == 0 && MainMenu.IsShowing, "Escape closes Settings, back to the main menu");
            SelfTest.Check(UiProbe.HasInteractable(mainMenu), "the main menu can be used again");
            SelfTest.Check(SelectedName() == "Button: Play", "Play has the selection again: " + SelectedName());

            // Escape on the main menu opens nothing: there is no game to come back from.
            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Count == 0 && MainMenu.IsShowing, "Escape on the main menu opens no session menu");

            // Into the game.
            Press(mainMenu, "Button: Play");
            yield return null;
            SelfTest.Check(!MainMenu.IsShowing && !MenuFocus.IsAnyOpen, "Play hides the main menu and gives the controls back");
            yield return new WaitForSecondsRealtime(1f);
            yield return SelfTest.Capture("3-Game");

            // The session menu, opened with Escape.
            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Count == 1 && MenuFocus.IsAnyOpen, "Escape opens the session menu");
            if (Menus.Count != 1)
            {
                SelfTest.Finish();
                yield break;
            }

            RectTransform sessionMenu = Menus.Top;
            Debug.Log("Menu check: session menu: " + UiProbe.Texts(sessionMenu));
            SelfTest.Check(SelectedName() == "Button: Resume", "Resume has the selection: " + SelectedName());
            CheckNavigation("the session menu", sessionMenu);
            yield return SelfTest.Capture("4-SessionMenu");

            // Settings over it, closed with the gamepad's East (Cancel): one menu per press.
            Press(sessionMenu, "Button: Settings");
            yield return null;
            SelfTest.Check(Menus.Count == 2, "Settings opens over the session menu");
            SelfTest.Check(!UiProbe.HasInteractable(sessionMenu), "nothing under Settings over the session menu can be used");
            CheckNavigation("Settings over the session menu", Menus.Top);
            yield return SelfTest.Capture("5-SessionMenu-Settings");
            yield return QueuedInput.Press(GamepadButton.East);
            SelfTest.Check(Menus.Count == 1 && Menus.Top == sessionMenu, "the gamepad's East closes Settings only");
            SelfTest.Check(SelectedName() == "Button: Resume", "Resume has the selection again: " + SelectedName());

            // The gamepad's Start (Menu) closes the session menu and opens it again.
            yield return QueuedInput.Press(GamepadButton.Start);
            SelfTest.Check(Menus.Count == 0 && !MenuFocus.IsAnyOpen, "the gamepad's Start closes the session menu");
            yield return QueuedInput.Press(GamepadButton.Start);
            SelfTest.Check(Menus.Count == 1, "the gamepad's Start opens it again");

            // Quit to Menu: the scene loads afresh under the main menu.
            int loadedBefore = _scenesLoaded;
            if (Menus.Count == 1)
            {
                Press(Menus.Top, "Button: Quit to Menu");
            }
            else
            {
                MainMenu.Instance.EndGame();
            }

            yield return WaitFor(() => _scenesLoaded > loadedBefore && MainMenu.IsShowing, "the main menu after quitting to it");
            if (!_arrived)
            {
                yield break;
            }

            yield return null;
            SelfTest.Check(Menus.Count == 0 && MenuFocus.IsAnyOpen, "back on the main menu, which holds the controls");
            SelfTest.Check(SelectedName() == "Button: Play", "Play has the selection on the main menu again: " + SelectedName());
            SelfTest.Check(EventSystem.current != null, "the EventSystem outlived the scene");
            yield return new WaitForSecondsRealtime(1f); // anything the fresh scene has to say
            yield return SelfTest.Capture("6-MainMenu-After");

            // And the game plays again from it.
            Press(mainMenu, "Button: Play");
            yield return null;
            SelfTest.Check(!MainMenu.IsShowing && !MenuFocus.IsAnyOpen, "Play starts the game again");
            SelfTest.Finish();
        }

        //----------

        // Waits for something that takes a scene load. If it never comes the run is over: _arrived
        // is false, and the steps stop.
        private static IEnumerator WaitFor(Func<bool> arrived, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (!arrived() && Time.realtimeSinceStartup - start < WaitSeconds)
            {
                yield return null;
            }

            _arrived = SelfTest.Check(arrived(), "waited for " + what);
            if (_arrived)
            {
                SelfTest.Progress();
            }
            else
            {
                SelfTest.Finish();
            }
        }

        private static void Press(RectTransform menu, string buttonName) =>
            UiProbe.Press(UiProbe.Find<Button>(menu, buttonName), buttonName);

        // What the stack promises of the menu on top, and of the main menu with none over it:
        // keyboard and gamepad navigation, from its selected control, stays in it.
        private static void CheckNavigation(string what, RectTransform menu)
        {
            List<Selectable> reached = UiProbe.Reachable();
            bool inside = reached.Count > 0;
            var names = new List<string>();
            foreach (var control in reached)
            {
                inside &= control.transform.IsChildOf(menu);
                names.Add(control.name);
            }

            SelfTest.Check(inside, $"navigation in {what} stays in it: {string.Join(" | ", names)}");
        }

        private static string SelectedName()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null ? selected.name : "nothing";
        }
    }
}
