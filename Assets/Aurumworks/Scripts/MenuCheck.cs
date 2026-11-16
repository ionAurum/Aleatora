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
    /// orientations) and that the melody tables came with the player and read, MIDI out (the MIDI page
    /// from Settings and from Output, the transport on the Melodies page, into a RecordingOutput: no
    /// port is opened), MIDI in (a FeedInput's chord held: no input is opened; the MIDI page's inputs,
    /// clock and monitor, following a fed clock and sending one, IN-3), then quits with exit code
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

            // The MIDI page from Settings' MIDI row (MIDI-3), closed with Escape.
            yield return CheckMidiPage(Menus.Top, "Button: MIDI", "Button: Back", true);

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

            // MIDI out (MIDI-1): no port opened in a self-test; Panic and closing, into a recording.
            // MIDI in (IN-1): no input opened; a fed chord held.
            CheckMidi();

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

        // MIDI out (MIDI-1): the bootstrap made MidiPlayback and, in a self-test, it opened no port
        // (the saved one leads into Reaper). Listing the ports doesn't throw and hides Microsoft GS
        // Wavetable Synth. With a RecordingOutput in its place, Panic sends a note off for every note
        // on every channel, and closing sends All Notes Off on the channel in use and closes it.
        private static void CheckMidi()
        {
            MidiPlayback midi = Aleatora.Instance != null ? Aleatora.Instance.Midi : null;
            if (!SelfTest.Check(midi != null, "the bootstrap made MidiPlayback"))
            {
                return;
            }

            SelfTest.Check(!midi.IsOpen, "a self-test opens no MIDI port");
            IReadOnlyList<string> outputs = MidiPorts.Outputs;
            Debug.Log("Menu check: MIDI outputs: " + (outputs.Count == 0 ? "none" : string.Join(", ", outputs)));
            bool hidden = true;
            foreach (string name in outputs)
            {
                hidden &= !MidiPorts.IsHidden(name);
            }

            SelfTest.Check(hidden, "the MIDI outputs don't list Microsoft GS Wavetable Synth");

            var recording = new RecordingOutput("Menu check");
            midi.Use(recording);
            recording.Send(MidiMessage.NoteOn(midi.Channel, 60, 100));
            midi.Panic();
            SelfTest.Check(recording.Count == 1 + MidiMessage.PanicCount && recording.SoundingCount == 0,
                $"Panic sends {MidiMessage.PanicCount} note offs and leaves nothing sounding: {recording.Count - 1}");
            midi.Close();
            RecordingOutput.Sent[] sent = recording.Messages;
            SelfTest.Check(recording.IsDisposed && !midi.IsOpen && sent.Length > 0
                && sent[sent.Length - 1].Message == MidiMessage.AllNotesOff(midi.Channel),
                $"closing sends All Notes Off on channel {midi.Channel} and closes the output");

            // MIDI in (IN-1): no input opened in a self-test; a fed chord shows as held, through the
            // clock thread, and closing the feed forgets it.
            SelfTest.Check(midi.InputNames.Count == 0, "a self-test opens no MIDI input");
            IReadOnlyList<string> inputs = MidiPorts.Inputs;
            Debug.Log("Menu check: MIDI inputs: " + (inputs.Count == 0 ? "none" : string.Join(", ", inputs)));
            var feed = new FeedInput("Menu check keys", () => midi.Now);
            SelfTest.Check(midi.UseInput(feed) && midi.IsClockRunning, "a fed input opens and starts the clock thread");
            feed.Feed(MidiMessage.NoteOn(1, 60, 100));
            feed.Feed(MidiMessage.NoteOn(1, 64, 100));
            feed.Feed(MidiMessage.NoteOn(1, 67, 100));
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (midi.Held.Count < 3 && watch.ElapsedMilliseconds < 500)
            {
                System.Threading.Thread.Sleep(1);
            }

            var held = new List<HeldNotes.Held>();
            midi.Held.CopyTo(held);
            string names = string.Join(" ", held.ConvertAll(h => h.ToString()));
            SelfTest.Check(names == "C4 E4 G4", $"the fed chord shows as held in {watch.ElapsedMilliseconds} ms: {names}");
            var received = new List<TimedMessage>();
            midi.DrainReceived(received);
            SelfTest.Check(received.Count == 3, "and goes to the monitor's queue: " + received.Count);
            midi.CloseInput(feed.Name);
            SelfTest.Check(feed.IsDisposed && midi.Held.Count == 0 && midi.InputNames.Count == 0,
                "closing the input forgets what it held");
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
            CheckReachesAll("the Melodies page", top);
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

                // The transport (MIDI-3), into a recording; it ends with Generate's 32 notes again.
                yield return CheckTransport(page, top);

                // MIDI in on the screens (IN-3): the inputs, the monitor, following a fed clock, sending one.
                yield return CheckMidiIn(page, top);

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

                // Escape while playing: the page closes with nothing sounding.
                Press(top, "Button: Play");
                yield return new WaitForSecondsRealtime(0.3f);
                yield return QueuedInput.Press(Key.Escape);
                yield return null; // the page's OnDestroy has run
                MidiPlayback midi = page != null ? page.Midi : Aleatora.Instance?.Midi;
                var recording = midi != null ? midi.Output as RecordingOutput : null;
                SelfTest.Check(midi != null && recording != null && !midi.Player.IsPlaying && recording.SoundingCount == 0
                    && recording.Count > 0, $"closing the page while playing stops it with nothing sounding: {recording?.SoundingCount}");
                if (midi != null)
                {
                    midi.Close(); // the recording out: CheckMidi finds no port open
                }
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

        // The MIDI page (MIDI-3), opened by a button on the menu under it: None first, then the ports,
        // never GS Wavetable; Refresh builds it again; Escape closes it, and the selection goes back to
        // mainAction under it. It presses no port: that would open one.
        private static IEnumerator CheckMidiPage(RectTransform under, string button, string mainAction, bool shoot)
        {
            int before = Menus.Count;
            Press(under, button);
            yield return null;
            RectTransform top = Menus.Count == before + 1 ? Menus.Top : null;
            if (!SelfTest.Check(top != null && top.name == "MIDI", $"{button} opens the MIDI page over the menu"))
            {
                yield break;
            }

            CheckMidiList(top);
            SelfTest.Check(!UiProbe.HasInteractable(under), "nothing under the MIDI page can be used");
            CheckNavigation("the MIDI page", top);
            CheckReachesAll("the MIDI page", top);
            Debug.Log("Menu check: MIDI page: " + UiProbe.Texts(top));
            if (shoot)
            {
                yield return new WaitForSecondsRealtime(0.2f);
                yield return SelfTest.Capture("5-Midi");
            }

            Press(top, "Button: Refresh");
            yield return null;
            SelfTest.Check(Menus.Count == before + 1 && Menus.Top != top && Menus.Top.name == "MIDI"
                && SelectedName() == "Button: Refresh", "Refresh builds the MIDI page again, Refresh selected: " + SelectedName());
            CheckMidiList(Menus.Top);

            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Count == before && Menus.Top == under, "Escape closes the MIDI page, and only it");
            SelfTest.Check(SelectedName() == mainAction, $"{mainAction} has the selection again: " + SelectedName());
        }

        private static void CheckMidiList(RectTransform page)
        {
            Button[] buttons = page.GetComponentsInChildren<Button>();
            var names = new List<string>();
            bool hidden = true;
            int accent = 0;
            foreach (Button button in buttons)
            {
                names.Add(button.name);
                hidden &= !button.name.Contains("Microsoft GS Wavetable");
                accent += button.targetGraphic != null && button.targetGraphic.color == UiKit.Theme.Accent ? 1 : 0;
            }

            SelfTest.Check(buttons.Length >= 3 && buttons[0].name == "Button: None" && names.Contains("Button: Refresh")
                && names.Contains("Button: Back"), "the MIDI page lists None first, then Refresh and Back: " + string.Join(" | ", names));
            SelfTest.Check(hidden, "the MIDI page doesn't list Microsoft GS Wavetable Synth");
            SelfTest.Check(accent == 1, "one port, the chosen one, is in the accent colour: " + accent);
        }

        // The transport on the Melodies page (MIDI-3), with a RecordingOutput in place of a port:
        // Channel 3, BPM 300, Play: the first four note ons are the melody's first four on channel 3,
        // about 0.2 s apart, each with its note off; the playhead framed; Stop leaves nothing sounding;
        // Loop with four notes wraps; Endless plays past the last note and the strip grows, the page's
        // notes the player's; Panic sends the 2,048 and playing goes on; Output opens the MIDI page,
        // which doesn't stop playback.
        private static IEnumerator CheckTransport(MelodiesPage page, RectTransform top)
        {
            MidiPlayback midi = page.Midi;
            if (!SelfTest.Check(midi != null && !midi.IsOpen, "the page has MidiPlayback, with no port open"))
            {
                yield break;
            }

            var recording = new RecordingOutput("Menu check");
            midi.Use(recording);
            yield return null;
            Button output = UiProbe.Find<Button>(top, "Button: Output");
            string shows = output != null ? output.GetComponentInChildren<TMP_Text>().text : null;
            SelfTest.Check(shows == "Menu check", "Output shows the output's name: " + shows);

            TMP_Dropdown channel = Control<TMP_Dropdown>(top, "Dropdown: Channel");
            if (SelfTest.Check(channel != null && channel.options.Count == 16, "the page has its Channel dropdown, 1 to 16"))
            {
                channel.value = 2;
            }

            SelfTest.Check(midi.Channel == 3, "Channel 3: " + midi.Channel);
            TMP_InputField bpm = UiProbe.Find<TMP_InputField>(top, "InputField: BPM");
            if (SelfTest.Check(bpm != null, "the page has its BPM field"))
            {
                bpm.text = "300";
                bpm.onEndEdit.Invoke("300");
                SelfTest.Check(midi.Bpm == 300 && bpm.text == "300", "BPM 300: " + midi.Bpm);
                bpm.text = "301";
                bpm.onEndEdit.Invoke("301");
                SelfTest.Check(midi.Bpm == 300 && bpm.text == "300", "BPM 301 puts 300 back: " + bpm.text);
            }

            // Play: the first four notes, a fifth of a second apart, each with its note off.
            var first = new List<int>(page.Notes);
            Press(top, "Button: Play");
            yield return new WaitForSecondsRealtime(0.95f);
            yield return null;
            string label = UiProbe.Find<Button>(top, "Button: Play")?.GetComponentInChildren<TMP_Text>().text;
            SelfTest.Check(midi.Player.IsPlaying && label == "Pause", "Play plays, and reads Pause: " + label);
            RecordingOutput.Sent[] sent = recording.Messages;
            var onAt = new List<int>(); // the note ons' indices in sent
            for (int i = 0; i < sent.Length; i++)
            {
                if (MidiMessage.IsNoteOn(sent[i].Message))
                {
                    onAt.Add(i);
                }
            }

            bool beats = onAt.Count >= 4;
            var said = new System.Text.StringBuilder();
            for (int i = 0; beats && i < 4; i++)
            {
                RecordingOutput.Sent on = sent[onAt[i]];
                beats &= MidiMessage.Channel(on.Message) == 3 && MidiMessage.Data1(on.Message) == first[i];
                if (i > 0)
                {
                    double gap = on.Time - sent[onAt[i - 1]].Time;
                    beats &= Math.Abs(gap - 0.2) < 0.015;
                    said.Append($"{gap * 1000d:0.0} ");
                }

                beats &= HasNoteOff(sent, onAt[i], 0.18);
            }

            SelfTest.Check(beats, $"the first four note ons are the melody's first four on channel 3, 0.2 s apart (ms: {said}), each with its note off 0.18 s on: {onAt.Count} on");
            int position = midi.Player.Position;
            SelfTest.Check(page.Strip.PlayheadColumn >= 0 && Math.Abs(page.Strip.Playhead - position) <= 1,
                $"the strip frames the note playing: {page.Strip.Playhead}, the player's {position}");
            yield return SelfTest.Capture("5-Playing");

            Press(top, "Button: Stop");
            yield return null;
            SelfTest.Check(!midi.Player.IsPlaying && recording.SoundingCount == 0 && page.Strip.PlayheadColumn == -1,
                "Stop leaves nothing sounding and nothing framed");

            // Loop, with four notes: the fifth note on is the first again.
            Toggle loop = Control<Toggle>(top, "Toggle: Loop");
            Toggle endless = Control<Toggle>(top, "Toggle: Endless");
            if (!SelfTest.Check(loop != null && endless != null, "the page has its Loop and Endless toggles"))
            {
                yield break;
            }

            loop.isOn = true;
            midi.SetNotes(first.GetRange(0, 4));
            recording.Clear();
            Press(top, "Button: Play");
            yield return new WaitForSecondsRealtime(1.1f);
            List<int> ons = NoteOns(recording);
            SelfTest.Check(midi.Player.Loop && midi.Player.IsPlaying && ons.Count >= 5 && ons[4] == first[0] && ons[3] == first[3],
                "Loop on, with four notes, wraps to the first: " + string.Join(" ", ons));
            Press(top, "Button: Stop");
            SelfTest.Progress();

            // Endless: past the last of Generate's 32, the page's generator makes the next.
            endless.isOn = true;
            SelfTest.Check(midi.Player.Endless && !midi.Player.Loop && !loop.isOn, "Endless on turns Loop off");
            Press(top, "Button: Generate");
            yield return null;
            recording.Clear();
            Press(top, "Button: Play");
            float start = Time.realtimeSinceStartup;
            while (midi.Player.Count < MelodiesPage.MelodyLength + 3 && Time.realtimeSinceStartup - start < 10f)
            {
                yield return null;
            }

            yield return null;
            SelfTest.Progress();
            ons = NoteOns(recording);
            bool same = page.Notes.Count == midi.Player.Count;
            for (int i = 0; same && i < page.Notes.Count; i++)
            {
                same = page.Notes[i] == midi.Player.NoteAt(i);
            }

            SelfTest.Check(midi.Player.IsPlaying && page.Degrees.Count > MelodiesPage.MelodyLength && page.Strip.Count == MelodyStrip.Columns
                && ons.Count > MelodiesPage.MelodyLength && ons[MelodiesPage.MelodyLength] == page.Notes[MelodiesPage.MelodyLength],
                $"Endless plays past the last note and the melody grows: {page.Degrees.Count} notes, {ons.Count} played");
            SelfTest.Check(same, "the page's melody is the player's, note for note: " + page.Notes.Count);
            SelfTest.Check(page.Strip.PlayheadColumn >= MelodyStrip.Columns - 2, "the playhead is at the strip's right: " + page.Strip.PlayheadColumn);

            // Panic! while playing: the 2,048, and playing goes on.
            recording.Clear();
            Press(top, "Button: Panic!");
            int offs = 0;
            foreach (RecordingOutput.Sent one in recording.Messages)
            {
                offs += MidiMessage.IsNoteOff(one.Message) ? 1 : 0;
            }

            SelfTest.Check(offs >= MidiMessage.PanicCount && midi.Player.IsPlaying,
                $"Panic! sends {MidiMessage.PanicCount} note offs and playing goes on: {offs}");

            // Output opens the MIDI page over the page; playing goes on under it.
            yield return CheckMidiPage(top, "Button: Output", "Button: Generate", false);
            SelfTest.Check(midi.Player.IsPlaying, "the MIDI page over the Melodies page doesn't stop playback");

            Press(top, "Button: Stop");
            endless.isOn = false;
            Press(top, "Button: Generate");
            yield return null;
            SelfTest.Check(!midi.Player.IsPlaying && recording.SoundingCount == 0 && page.Degrees.Count == MelodiesPage.MelodyLength,
                "stopped, with nothing sounding, and 32 notes again");
            SelfTest.Progress();
        }

        // MIDI in on the screens (IN-3), with a RecordingOutput and two FeedInputs, a keyboard and a
        // clock. The recording stands in as the output under the name of a listed input port, if any,
        // so the MIDI page greys that port. The MIDI page lists the inputs, the fed ones on, with
        // Clock, Send clock and Monitor, every control reachable (6-MidiIn). The fed keys show in the
        // monitor, newest first, and as held, the one let go under the pedal sustained (6-Monitor).
        // Clock follows the fed clock: at 150 BPM from Start, the BPM box reads 150.0 and can't be
        // edited, the first note on comes with the first tick, note n on beat n, and the playhead
        // moves (6-Following); the fed Stop leaves nothing sounding. Then Aleatora's own clock with
        // Send clock on, BPM 150, Play: SPP 0, Start, ticks about 16.7 ms apart, and Stop on Stop.
        // It ends with the feeds closed, the clock Aleatora's and Send clock off.
        private static IEnumerator CheckMidiIn(MelodiesPage page, RectTransform top)
        {
            MidiPlayback midi = page.Midi;
            if (!SelfTest.Check(midi != null, "the page has MidiPlayback for MIDI in"))
            {
                yield break;
            }

            var ports = new List<string>(MidiPorts.Inputs);
            string outputName = ports.Count == 0 ? "Menu check" : ports.Contains("Aleatora Test") ? "Aleatora Test" : ports[0];
            var recording = new RecordingOutput(outputName, () => midi.Now);
            midi.Use(recording);
            var keys = new FeedInput("Menu check keys", () => midi.Now);
            var reaper = new FeedInput("Menu check clock", () => midi.Now);
            SelfTest.Check(midi.UseInput(keys) && midi.UseInput(reaper), "two fed inputs open, a keyboard and a clock");
            Debug.Log($"Menu check: the recording stands in as the output '{outputName}'; MIDI inputs: " +
                (ports.Count == 0 ? "none" : string.Join(", ", ports)));

            // The MIDI page, over the Melodies page, with the inputs.
            Press(top, "Button: Output");
            yield return null;
            RectTransform midiPage = Menus.Top != null && Menus.Top.name == "MIDI" ? Menus.Top : null;
            if (!SelfTest.Check(midiPage != null, "Output opens the MIDI page"))
            {
                yield break;
            }

            var rows = new List<string>();
            bool listed = true;
            foreach ((string name, string label) in MidiPage.InputRows(midi))
            {
                Toggle toggle = UiProbe.Find<Toggle>(midiPage, "Toggle: " + label);
                listed &= toggle != null;
                rows.Add(label + (toggle == null ? " (missing)" : toggle.isOn ? " [on]" : toggle.IsInteractable() ? " [off]" : " [greyed]"));
            }

            Toggle keysToggle = UiProbe.Find<Toggle>(midiPage, "Toggle: Menu check keys");
            Toggle clockToggle = UiProbe.Find<Toggle>(midiPage, "Toggle: Menu check clock");
            SelfTest.Check(listed && keysToggle != null && keysToggle.isOn && clockToggle != null && clockToggle.isOn,
                "the MIDI page lists every input, the fed ones on: " + string.Join(" | ", rows));
            if (ports.Count > 0)
            {
                Toggle greyed = UiProbe.Find<Toggle>(midiPage, $"Toggle: {outputName} (the output)");
                SelfTest.Check(greyed != null && !greyed.isOn && !greyed.IsInteractable(), $"the output's own port, {outputName}, is greyed and can't be turned on");
            }
            else
            {
                Debug.Log("Menu check: no input port listed, so no greyed output's port to see.");
            }

            TMP_Dropdown clock = Control<TMP_Dropdown>(midiPage, "Dropdown: Clock");
            string options = clock != null ? string.Join(", ", clock.options.ConvertAll(o => o.text)) : "none";
            SelfTest.Check(clock != null && options == "Aleatora, Menu check keys, Menu check clock" && clock.value == 0,
                "Clock offers Aleatora, then each open input, Aleatora chosen: " + options);
            Toggle send = Control<Toggle>(midiPage, "Toggle: Send clock");
            SelfTest.Check(send != null && !send.isOn, "Send clock is there, off");
            SelfTest.Check(UiProbe.Find<Button>(midiPage, "Button: Monitor") != null, "Monitor is there");
            CheckMidiList(midiPage);
            CheckNavigation("the MIDI page with inputs", midiPage);
            CheckReachesAll("the MIDI page with inputs", midiPage);
            Debug.Log("Menu check: MIDI page with inputs: " + UiProbe.Texts(midiPage));
            yield return new WaitForSecondsRealtime(0.2f);
            yield return SelfTest.Capture("6-MidiIn");

            // The keyboard: C4, E4 and G4 down, the pedal down, G4 let go: G4 sustained.
            keys.Feed(MidiMessage.NoteOn(1, 60, 100));
            keys.Feed(MidiMessage.NoteOn(1, 64, 90));
            keys.Feed(MidiMessage.NoteOn(1, 67, 80));
            keys.Feed(MidiMessage.ControlChange(1, 64, 127));
            keys.Feed(MidiMessage.NoteOff(1, 67));
            float start = Time.realtimeSinceStartup;
            while (!midi.Held.IsSustained(keys.Name, 1, 67) && Time.realtimeSinceStartup - start < 0.5f)
            {
                yield return null;
            }

            Press(midiPage, "Button: Monitor");
            yield return null;
            MidiMonitorPage monitor = Menus.Top != null ? Menus.Top.GetComponent<MidiMonitorPage>() : null;
            if (!SelfTest.Check(monitor != null, "Monitor opens the monitor over the MIDI page"))
            {
                yield break;
            }

            IReadOnlyList<string> lines = monitor.MessageLines;
            Debug.Log("Menu check: the monitor: " + monitor.ClockLine + " / " + monitor.HeldLine + " / " + string.Join(" / ", lines));
            SelfTest.Check(lines.Count == 5 && lines[0].StartsWith("Menu check keys · ch 1 · Note off G4 · ", StringComparison.Ordinal)
                && lines[1].Contains(" · CC 64 127 · ") && lines[4].Contains(" · Note on C4 100 · "),
                "the monitor lists the five fed messages, newest first, with port, channel and time: " + lines.Count);
            SelfTest.Check(monitor.HeldLine == "C4 E4 G4 (sustained)", "the monitor shows the keys held, G4 sustained: " + monitor.HeldLine);
            SelfTest.Check(monitor.ClockLine == "Aleatora: " + midi.Bpm + " BPM", "the monitor shows Aleatora's clock: " + monitor.ClockLine);
            CheckNavigation("the monitor", Menus.Top);
            CheckReachesAll("the monitor", Menus.Top);
            yield return new WaitForSecondsRealtime(0.2f);
            yield return SelfTest.Capture("6-Monitor");
            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Top == midiPage, "Escape closes the monitor, back to the MIDI page");

            // Clock: follow the fed clock.
            if (clock != null)
            {
                clock.value = 2;
            }

            SelfTest.Check(midi.IsFollowing && midi.Followed == reaper.Name && midi.ClockName == reaper.Name,
                "Clock follows the fed clock: " + midi.Followed);
            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Top == top, "Escape closes the MIDI page, back to the Melodies page");
            yield return null;
            TMP_InputField bpm = UiProbe.Find<TMP_InputField>(top, "InputField: BPM");
            SelfTest.Check(bpm != null && !bpm.IsInteractable(), "while following, the BPM box can't be edited");

            // 150 BPM from Start, eight beats, then Stop, fed on a thread of its own as a port would
            // deliver them, each tick stamped with the time it was due.
            var first = new List<int>(page.Notes);
            recording.Clear();
            const double gap = 60d / (150d * ClockFollower.TicksPerBeat);
            const int beats = 8;
            var tickTimes = new double[beats * ClockFollower.TicksPerBeat];
            var feeder = new System.Threading.Thread(() =>
            {
                double begin = midi.Now + 0.05;
                reaper.Feed(MidiMessage.Start());
                for (int i = 0; i < tickTimes.Length; i++)
                {
                    double due = begin + i * gap;
                    while (midi.Now < due)
                    {
                        System.Threading.Thread.Sleep(due - midi.Now > 0.003 ? 1 : 0);
                    }

                    tickTimes[i] = due;
                    reaper.Feed(MidiMessage.Clock(), due);
                }

                reaper.Feed(MidiMessage.Stop());
            })
            {
                Name = "Menu check clock feed",
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.AboveNormal,
            };
            feeder.Start();
            yield return new WaitForSecondsRealtime(1.05f); // about two and a half beats
            int earlier = page.Strip.Playhead;
            string shows = bpm != null ? bpm.text : null;
            SelfTest.Check(shows == "150.0", "following a fed clock at 150 BPM, the BPM box reads 150.0: " + shows);
            string says = MidiMonitorPage.ClockText(midi);
            SelfTest.Check(says.StartsWith("Following Menu check clock: 150.0 BPM, playing, beat ", StringComparison.Ordinal),
                "the monitor would say: " + says);
            yield return SelfTest.Capture("6-Following");
            yield return new WaitForSecondsRealtime(0.8f);
            int later = page.Strip.Playhead;
            SelfTest.Check(earlier >= 1 && later > earlier && page.Strip.PlayheadColumn >= 0,
                $"the playhead moves with the clock: note {earlier}, then {later}");

            start = Time.realtimeSinceStartup;
            while ((feeder.IsAlive || midi.Follower.IsRunning || recording.SoundingCount > 0) && Time.realtimeSinceStartup - start < 6f)
            {
                yield return null;
            }

            RecordingOutput.Sent[] sent = recording.Messages;
            var ons = new List<RecordingOutput.Sent>();
            foreach (RecordingOutput.Sent one in sent)
            {
                if (MidiMessage.IsNoteOn(one.Message))
                {
                    ons.Add(one);
                }
            }

            bool onBeats = ons.Count == beats;
            double worst = 0d;
            for (int n = 0; onBeats && n < ons.Count; n++)
            {
                double late = ons[n].Time - tickTimes[n * ClockFollower.TicksPerBeat];
                worst = Math.Max(worst, late);
                onBeats &= MidiMessage.Data1(ons[n].Message) == first[n] && late >= 0d && late < 0.005;
            }

            double firstLate = ons.Count > 0 ? (ons[0].Time - tickTimes[0]) * 1000d : double.NaN;
            SelfTest.Check(onBeats, $"the first note on comes with the first tick ({firstLate:0.00} ms after it), and note n with beat n's tick, the melody's notes: {ons.Count} of {beats}, worst {worst * 1000d:0.00} ms");
            SelfTest.Check(!feeder.IsAlive && !midi.Follower.IsRunning && recording.SoundingCount == 0, "the fed Stop leaves nothing sounding");
            SelfTest.Progress();

            // Aleatora's own clock with Send clock on: Start, ticks at 150 BPM, Stop.
            Press(top, "Button: Stop");
            Press(top, "Button: Output");
            yield return null;
            midiPage = Menus.Top != null && Menus.Top.name == "MIDI" ? Menus.Top : null;
            clock = midiPage != null ? Control<TMP_Dropdown>(midiPage, "Dropdown: Clock") : null;
            send = midiPage != null ? Control<Toggle>(midiPage, "Toggle: Send clock") : null;
            if (!SelfTest.Check(clock != null && send != null && clock.value == 2, "the MIDI page again, Clock on the fed clock"))
            {
                yield break;
            }

            clock.value = 0;
            send.isOn = true;
            SelfTest.Check(!midi.IsFollowing && midi.ClockName.Length == 0 && midi.SendClock, "Clock on Aleatora, Send clock on");
            yield return QueuedInput.Press(Key.Escape);
            yield return null;
            string own = midi.Bpm.ToString(System.Globalization.CultureInfo.InvariantCulture);
            SelfTest.Check(bpm != null && bpm.IsInteractable() && bpm.text == own, "the BPM box is Aleatora's again, and editable: " + bpm?.text);
            if (bpm != null)
            {
                bpm.text = "150";
                bpm.onEndEdit.Invoke("150");
            }

            recording.Clear();
            Press(top, "Button: Play");
            yield return new WaitForSecondsRealtime(1.05f);
            Press(top, "Button: Stop");
            yield return null;
            sent = recording.Messages;
            int startAt = -1;
            int sppAt = -1;
            var ticks = new List<double>();
            int lastSystem = 0;
            foreach (RecordingOutput.Sent one in sent)
            {
                int message = one.Message;
                if (MidiMessage.IsSongPosition(message) && startAt < 0 && sppAt < 0 && MidiMessage.SongPositionOf(message) == 0)
                {
                    sppAt = ticks.Count;
                }
                else if (MidiMessage.IsStart(message) && startAt < 0)
                {
                    startAt = ticks.Count;
                }
                else if (MidiMessage.IsClock(message) && startAt >= 0)
                {
                    ticks.Add(one.Time);
                }

                if (MidiMessage.IsSystem(message))
                {
                    lastSystem = message;
                }
            }

            double mean = ticks.Count > 1 ? (ticks[ticks.Count - 1] - ticks[0]) / (ticks.Count - 1) : 0d;
            double spread = 0d;
            for (int i = 1; i < ticks.Count; i++)
            {
                spread = Math.Max(spread, Math.Abs(ticks[i] - ticks[i - 1] - gap));
            }

            SelfTest.Check(midi.Bpm == 150 && sppAt == 0 && startAt == 0 && ticks.Count >= 55 && Math.Abs(mean - gap) < 0.0005 && spread < 0.005,
                $"Send clock with Aleatora's clock at 150 BPM: SPP 0, Start, then {ticks.Count} ticks {mean * 1000d:0.00} ms apart (worst {spread * 1000d:0.00} ms off {gap * 1000d:0.00})");
            SelfTest.Check(MidiMessage.IsStop(lastSystem) && recording.SoundingCount == 0, "and Stop on Stop, with nothing sounding");

            // As it was: no feeds, Aleatora's clock, Send clock off.
            midi.ChooseSendClock(false);
            midi.CloseInput(keys.Name);
            midi.CloseInput(reaper.Name);
            midi.DrainReceived(new List<TimedMessage>()); // the fed clock's Start and Stop: CheckMidi counts the queue afresh
            SelfTest.Check(!midi.SendClock && midi.InputNames.Count == 0 && keys.IsDisposed && reaper.IsDisposed && midi.Held.Count == 0,
                "the feeds closed, Send clock off");
            SelfTest.Progress();
        }

        // Whether the note on at sent[index] has its note off, on its channel, length seconds after it (to 15 ms):
        // the first message after it that ends that note.
        private static bool HasNoteOff(RecordingOutput.Sent[] sent, int index, double length)
        {
            RecordingOutput.Sent on = sent[index];
            int note = MidiMessage.Data1(on.Message);
            int channel = MidiMessage.Channel(on.Message);
            for (int i = index + 1; i < sent.Length; i++)
            {
                int message = sent[i].Message;
                if (MidiMessage.IsNoteOff(message) && MidiMessage.Data1(message) == note && MidiMessage.Channel(message) == channel)
                {
                    return Math.Abs(sent[i].Time - on.Time - length) < 0.015;
                }
            }

            return false;
        }

        private static List<int> NoteOns(RecordingOutput recording)
        {
            var notes = new List<int>();
            foreach (RecordingOutput.Sent one in recording.Messages)
            {
                if (MidiMessage.IsNoteOn(one.Message))
                {
                    notes.Add(MidiMessage.Data1(one.Message));
                }
            }

            return notes;
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

        // Every usable control on the menu on top can be reached by navigation from the selected one.
        private static void CheckReachesAll(string what, RectTransform menu)
        {
            List<Selectable> reached = UiProbe.Reachable();
            var unreached = new List<string>();
            foreach (Selectable control in menu.GetComponentsInChildren<Selectable>())
            {
                if (control.IsInteractable() && !reached.Contains(control))
                {
                    unreached.Add(control.transform.parent.name + "/" + control.name);
                }
            }

            SelfTest.Check(unreached.Count == 0, $"navigation in {what} reaches every control from {SelectedName()}: " +
                (unreached.Count == 0 ? reached.Count + " controls" : "not " + string.Join(", ", unreached)));
        }

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
