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
    /// back), that Settings lists no volumes (Aleatora makes no sound), and the board behind the
    /// menus (its 61 cells, picking them, its shapes in both orientations) and that the melody tables
    /// came with the player and read, then quits with exit code
    /// 1 if anything didn't hold or any error was logged. -aleatoraScreenshots folder saves a screenshot of each menu. It
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

            // The board behind the menus: its cells, picking them, its shapes and orientations.
            yield return CheckGrid();

            // The melody tables came with the player and read.
            CheckTables();

            // The generator makes melodies from the tuning's set, the same for the same seed, in range.
            CheckGenerator();

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
            CheckNoPickUnderAMenu();
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

        // The default board has its 61 cells; a pick at three cells' places on the screen selects
        // each, and one off the board selects nothing; then the other shapes and the other
        // orientation, set through the tuning as Jon would in the Inspector, each with a screenshot.
        private static IEnumerator CheckGrid()
        {
            Aleatora game = Aleatora.Instance;
            if (!SelfTest.Check(game != null && game.Grid != null && game.Selection != null, "the bootstrap made the grid and its selection"))
            {
                yield break;
            }

            HexGrid grid = game.Grid;
            GridSelection selection = game.Selection;
            GridTuning tuning = game.Tuning.Grid;
            Camera camera = Camera.main;
            Debug.Log($"Menu check: grid {tuning.K} × {tuning.M} × {tuning.N}, {tuning.Orientation}, {grid.CellCount} cells, tops at {grid.TopHeight:0.00}");
            SelfTest.Check(tuning.K == 5 && tuning.M == 5 && tuning.N == 5 && tuning.Orientation == HexOrientation.Flat,
                "the tuning's board is 5 × 5 × 5, flat");
            SelfTest.Check(grid.CellCount == 61 && grid.Shape.Count == 61, $"the default board has 61 cells: {grid.CellCount}");
            SelfTest.Check(!selection.HasSelection, "nothing is selected at the start");

            foreach (HexCoord coord in new[] { new HexCoord(0, 0), new HexCoord(0, 4), new HexCoord(4, 4) })
            {
                Vector3 screen = camera.WorldToScreenPoint(grid.TopOf(coord));
                bool onScreen = screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width && screen.y >= 0f && screen.y <= Screen.height;
                SelfTest.Check(onScreen, $"the cell {coord} is on the screen, at ({screen.x:0}, {screen.y:0})");
                bool picked = selection.PickAt(screen);
                SelfTest.Check(picked && selection.HasSelection && selection.Selected == coord
                    && grid.TryGetCell(coord, out HexCellView cell) && cell.IsSelected,
                    $"a pick at the cell {coord} selects it: {(selection.HasSelection ? selection.Selected.ToString() : "nothing")}");
            }

            yield return null;
            yield return SelfTest.Capture("3-Grid");

            selection.PickAt(new Vector2(2f, Screen.height - 2f)); // a corner of the screen, off the board
            SelfTest.Check(!selection.HasSelection, "a pick off the board selects nothing");

            yield return ShowBoard(grid, tuning, 7, 4, 1, HexOrientation.Flat, 28, "3-Grid-Parallelogram");
            yield return ShowBoard(grid, tuning, 3, 5, 7, HexOrientation.Flat, 57, "3-Grid-Uneven");
            yield return ShowBoard(grid, tuning, 5, 5, 5, HexOrientation.Pointy, 61, "3-Grid-Pointy");
            yield return ShowBoard(grid, tuning, 5, 5, 5, HexOrientation.Flat, 61, null);
        }

        // The tables are in the player's Resources: the index lists them and each reads (a set that
        // doesn't logs an error, which fails the run by itself).
        private static void CheckTables()
        {
            IReadOnlyList<string> names = MarkovLibrary.Names;
            SelfTest.Check(names.Count == 17, $"the index lists the 17 sets: {names.Count}");
            int read = 0;
            foreach (string name in names)
            {
                if (MarkovLibrary.TryGet(name, out MarkovTable table) && table.Name == name && table.MaxOrder == 4)
                {
                    read++;
                }
            }

            SelfTest.Check(read == names.Count, $"every set reads, with its four orders: {read} of {names.Count}");
            bool found = MarkovLibrary.TryGet("european_ionian", out MarkovTable ionian);
            SelfTest.Check(found && ionian.Tunes == 7136 && ionian.Notes == 481199 && ionian.Total(new[] { 11 }) == 35562,
                "european_ionian has its 7,136 melodies, 481,199 notes and 35,562 notes after 7");
            SelfTest.Check(!MarkovLibrary.TryGet("no_such_set", out _), "a set the index doesn't list is not found");
        }

        // A melody from the tuning's Melodies section, as M3's page will make one: 200 steps, twice
        // from the same seed; each degree a degree, each order used no more than asked, each note in
        // the voicing's range.
        private static void CheckGenerator()
        {
            MelodyTuning tuning = Aleatora.Instance != null ? Aleatora.Instance.Tuning.Melodies : null;
            if (!SelfTest.Check(tuning != null, "the tuning has its Melodies section")
                || !SelfTest.Check(MarkovLibrary.TryGet(tuning.Set, out MarkovTable table), $"the tuning's set '{tuning.Set}' reads"))
            {
                return;
            }

            var a = new MelodyGenerator(table, tuning.Generator, tuning.Seed);
            var b = new MelodyGenerator(table, tuning.Generator, tuning.Seed);
            Voicing voicing = tuning.MakeVoicing();
            float[] distribution = new float[Degree.Count];
            var melody = new System.Text.StringBuilder();
            bool same = true;
            bool sound = true;
            for (int i = 0; i < 200; i++)
            {
                MelodyStep step = a.Next(distribution);
                same &= b.Next(distribution).Degree == step.Degree;
                int note = voicing.Place(step.Degree);
                sound &= Degree.IsValid(step.Degree) && step.OrderUsed <= step.OrderAsked && note >= voicing.Low && note <= voicing.High;
                if (i < 16)
                {
                    melody.Append(Degree.Name(step.Degree)).Append('/').Append(note).Append(' ');
                }
            }

            Debug.Log($"Menu check: {table.Name}, seed {tuning.Seed}, order {tuning.Generator.Order}: {melody}…");
            SelfTest.Check(same, "the same seed gives the same melody");
            SelfTest.Check(sound, $"200 steps: degrees, orders and notes in {voicing.Low} to {voicing.High}");
        }

        // Sets the board's shape on the tuning, which the grid sees on its next Update and rebuilds from.
        private static IEnumerator ShowBoard(HexGrid grid, GridTuning tuning, int k, int m, int n, HexOrientation orientation,
            int cells, string shot)
        {
            tuning.K = k;
            tuning.M = m;
            tuning.N = n;
            tuning.Orientation = orientation;
            yield return null;
            yield return null; // the old cells are gone by the end of the frame that rebuilt
            SelfTest.Check(grid.Shape.K == k && grid.Shape.M == m && grid.Shape.N == n && grid.Layout.Orientation == orientation
                && grid.CellCount == cells && grid.GetComponentsInChildren<HexCellView>().Length == cells,
                $"the board rebuilds as {k} × {m} × {n}, {orientation}, with {cells} cells: {grid.CellCount}");
            SelfTest.Progress();
            if (shot != null)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                yield return SelfTest.Capture(shot);
            }
        }

        // With a menu open a click is the menu's: a pick at a cell's place selects nothing.
        private static void CheckNoPickUnderAMenu()
        {
            Aleatora game = Aleatora.Instance;
            if (game == null || game.Selection == null || Camera.main == null)
            {
                SelfTest.Check(false, "the grid is there under the session menu");
                return;
            }

            game.Selection.Deselect();
            bool picked = game.Selection.PickAt(Camera.main.WorldToScreenPoint(game.Grid.TopOf(new HexCoord(0, 4))));
            SelfTest.Check(!picked && !game.Selection.HasSelection, "under the session menu a pick selects nothing");
        }

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
