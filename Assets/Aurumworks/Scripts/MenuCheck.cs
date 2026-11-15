using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    /// back), that Settings lists no volumes (Aleatora makes no sound), the Melodies page from both
    /// menus, and the board behind the menus (its 61 cells, picking them, its shapes in both
    /// orientations) and that the melody tables came with the player and read, then quits with exit code
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

            // The Melodies page from the main menu, all through; Escape closes it.
            yield return CheckMelodies(mainMenu, "Button: Play", true);

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

            // The Melodies page from the session menu too, closed with the gamepad's East.
            yield return CheckMelodies(sessionMenu, "Button: Resume", false);

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

        // The Melodies page (M3), opened from a menu by its Melodies button: it opens over it with
        // Generate selected, and navigation reaches every control from there and nothing under it.
        // All through (from the main menu): seed 1 and Generate make 32 notes, the same again; the
        // graph draws arcs, none under the tuning's minimum; Order 4 and eight Steps, each within
        // order 4 and its bars summing to 1; a click on a node and on a grid row shows that row;
        // the Tonic moves every note; another set; then Escape. Else the gamepad's East closes it.
        // Either way the selection goes back to the menu under it, to its main action.
        private static IEnumerator CheckMelodies(RectTransform under, string mainAction, bool full)
        {
            int before = Menus.Count;
            Press(under, "Button: Melodies");
            yield return null;
            yield return null; // a frame for the layout
            MelodiesPage page = Menus.Count == before + 1 ? Menus.Top.GetComponent<MelodiesPage>() : null;
            if (!SelfTest.Check(page != null, "Melodies opens its page over the menu"))
            {
                yield break;
            }

            RectTransform top = Menus.Top;
            SelfTest.Check(SelectedName() == "Button: Generate", "Generate has the selection: " + SelectedName());
            SelfTest.Check(!UiProbe.HasInteractable(under), "nothing under the Melodies page can be used");
            SelfTest.Check(MenuFocus.IsAnyOpen, "the Melodies page holds the controls");
            CheckNavigation("the Melodies page", top);
            List<Selectable> reached = UiProbe.Reachable();
            var unreached = new List<string>();
            foreach (Selectable control in top.GetComponentsInChildren<Selectable>())
            {
                if (control.IsInteractable() && !reached.Contains(control))
                {
                    unreached.Add(control.transform.parent.name + "/" + control.name);
                }
            }

            SelfTest.Check(unreached.Count == 0, "navigation reaches every control from Generate: " +
                (unreached.Count == 0 ? reached.Count + " controls" : "not " + string.Join(", ", unreached)));
            SelfTest.Check(page.Strip.Count == MelodiesPage.MelodyLength, $"it opens with a melody of 32 notes: {page.Strip.Count}");

            if (full)
            {
                Debug.Log("Menu check: Melodies: " + UiProbe.Texts(top));
                MelodyViewTuning view = Aleatora.Instance != null ? Aleatora.Instance.Tuning.MelodyView : null;

                TMP_InputField seed = UiProbe.Find<TMP_InputField>(top, "InputField: Seed");
                if (SelfTest.Check(seed != null, "the page has its Seed field"))
                {
                    seed.text = "1";
                    seed.onEndEdit.Invoke("1");
                }

                SelfTest.Check(page.Work.Seed == 1, "the seed is 1: " + page.Work.Seed);
                Press(top, "Button: Generate");
                yield return null;
                var first = new List<int>(page.Degrees);
                SelfTest.Check(page.Strip.Count == 32 && first.Count == 32, $"Generate makes 32 notes, all in the strip: {first.Count}, {page.Strip.Count}");
                Press(top, "Button: Generate");
                yield return null;
                SelfTest.Check(SameDegrees(first, page.Degrees), "Generate again from seed 1 makes the same melody");
                CheckNewestFramed(page);
                SelfTest.Check(page.Graph.ArcCount > 0 && view != null && page.Graph.SmallestShare >= view.MinimumArcShare - 1e-6f,
                    $"the graph draws {page.Graph.ArcCount} arcs, the smallest {page.Graph.SmallestShare:0.000}, none under the minimum {view?.MinimumArcShare:0.00}");
                yield return new WaitForSecondsRealtime(0.3f);
                yield return SelfTest.Capture("4-Melodies");

                Slider order = Control<Slider>(top, "Slider: Order");
                if (SelfTest.Check(order != null, "the page has its Order slider"))
                {
                    order.value = 4f;
                }

                SelfTest.Check(page.Work.Generator.Order == 4, "Order is 4: " + page.Work.Generator.Order);
                bool steps = true;
                var said = new System.Text.StringBuilder();
                for (int i = 0; i < 8; i++)
                {
                    Press(top, "Button: Step");
                    yield return null;
                    float sum = 0f;
                    foreach (float value in page.NextBars.Values)
                    {
                        sum += value;
                    }

                    bool stepped = page.TryGetLastStep(out MelodyStep step);
                    steps &= stepped && step.OrderUsed <= 4 && step.OrderAsked == 4 && Mathf.Abs(sum - 1f) < 1e-4f
                        && page.NextBars.Outlined == step.Degree && page.Degrees.Count == 33 + i;
                    said.Append(step.OrderUsed).Append(' ');
                }

                SelfTest.Check(steps, $"eight Steps at order 4: each order used ≤ 4 ({said}), the bars sum to 1, the note outlined");
                SelfTest.Check(page.Strip.Count == 32, "the strip shows the last 32 notes of 40: " + page.Strip.Count);
                Debug.Log("Menu check: Next note says: " + page.NextBars.Caption);
                CheckNewestFramed(page);

                // A click on the node of 5, then on the grid's row of ♭3: Next note shows what follows each.
                Click(page.Graph.gameObject, page.Graph.transform.TransformPoint(page.Graph.NodeCentre(7)));
                SelfTest.Check(page.NextBars.Caption.StartsWith("after " + ViewText.DegreeName(7) + " · order 1", StringComparison.Ordinal)
                    && page.NextBars.Outlined == -1, "a click on the node of 5 shows what follows 5: " + page.NextBars.Caption);
                var gridRect = (RectTransform)page.Grid.transform;
                Rect cells = gridRect.rect;
                Click(page.Grid.gameObject, gridRect.TransformPoint(new Vector2(cells.center.x, cells.yMax - 3.5f * cells.height / Degree.Count)));
                SelfTest.Check(page.NextBars.Caption.StartsWith("after " + ViewText.DegreeName(3) + " · order 1", StringComparison.Ordinal),
                    "a click on the grid's row of ♭3 shows what follows it: " + page.NextBars.Caption);

                // The tonic to D: every note moves with it.
                TMP_Dropdown tonic = Control<TMP_Dropdown>(top, "Dropdown: Tonic");
                if (SelfTest.Check(tonic != null, "the page has its Tonic dropdown"))
                {
                    tonic.value = 2;
                }

                yield return null;
                bool moved = page.Work.Tonic % 12 == 2 && page.Strip.Count == 32;
                IReadOnlyList<int> notes = page.Strip.Notes;
                for (int i = 0; i < notes.Count; i++)
                {
                    int degree = page.Degrees[page.Degrees.Count - notes.Count + i];
                    moved &= notes[i] % 12 == (2 + degree) % 12;
                }

                SelfTest.Check(moved, $"with the tonic on D every note is its degree above D: tonic {page.Work.Tonic}");

                // Another set: irish_dorian.
                TMP_Dropdown set = Control<TMP_Dropdown>(top, "Dropdown: Set");
                int dorian = -1;
                for (int i = 0; i < MarkovLibrary.Names.Count; i++)
                {
                    if (MarkovLibrary.Names[i] == "irish_dorian")
                    {
                        dorian = i;
                    }
                }

                if (SelfTest.Check(set != null && dorian >= 0, "the page has its Set dropdown, listing irish_dorian"))
                {
                    set.value = dorian;
                }

                yield return null;
                SelfTest.Check(page.Table != null && page.Table.Name == "irish_dorian" && page.Strip.Count == 32 && page.Graph.ArcCount > 0,
                    $"the set is irish_dorian, with a melody and {page.Graph.ArcCount} arcs: {page.Table?.Name}");
                SelfTest.Check(set != null && set.captionText.text == "Irish · dorian", "the set reads 'Irish · dorian': " + set?.captionText.text);
                SelfTest.Check(Aleatora.Instance == null || Aleatora.Instance.Tuning.Melodies.Set == "european_ionian",
                    "the tuning asset's set is unchanged: the page works on a copy");
                SelfTest.Progress();
                yield return new WaitForSecondsRealtime(0.3f);
                yield return SelfTest.Capture("4-Dorian");
                yield return QueuedInput.Press(Key.Escape);
            }
            else
            {
                yield return QueuedInput.Press(GamepadButton.East);
            }

            SelfTest.Check(Menus.Count == before && Menus.Top == (before > 0 ? under : null),
                "Cancel closes the Melodies page, and only it");
            SelfTest.Check(UiProbe.HasInteractable(under), "the menu under it can be used again");
            SelfTest.Check(SelectedName() == mainAction, $"{mainAction} has the selection again: " + SelectedName());
        }

        private static bool SameDegrees(List<int> a, IReadOnlyList<int> b)
        {
            bool same = a.Count == b.Count;
            for (int i = 0; same && i < a.Count; i++)
            {
                same = a[i] == b[i];
            }

            return same;
        }

        private static void CheckNewestFramed(MelodiesPage page)
        {
            int newest = page.Degrees.Count > 0 ? page.Degrees[page.Degrees.Count - 1] : -1;
            SelfTest.Check(newest >= 0 && page.Grid.Framed == newest && page.Graph.Framed == newest,
                $"the newest note's row and node are framed: {newest}, {page.Grid.Framed}, {page.Graph.Framed}");
        }

        // A click through the event system on a point of the canvas (an overlay: world is screen).
        private static void Click(GameObject target, Vector3 world)
        {
            var click = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, world),
            };
            ExecuteEvents.Execute(target, click, ExecuteEvents.pointerClickHandler);
        }

        // A kit control's component, on a child of the row the kit names ("Slider: Order" holds its Slider on "Track").
        private static T Control<T>(RectTransform root, string name) where T : Component
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
            {
                if (child.name == name)
                {
                    return child.GetComponentInChildren<T>();
                }
            }

            return null;
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
