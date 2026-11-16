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

            // The board's notes (EM-1): each cell's degree, octave, colour and label; the three layouts.
            yield return CheckBoard();

            // The emitter (EM-2): the cone sprays spheres, hits glow, the cap holds, a menu stops it.
            // Its notes (EM-3): the hits as quantized MIDI notes on the clock thread.
            yield return CheckEmitter();

            // The emitter's panel (EM-4): its controls each change what they name, H hides it, clicks on it stay on it.
            yield return CheckPanel();

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

        // The board's notes (the emitter plan, EM-1), set through the tuning as Jon would in the
        // Inspector: each of the three layouts, with a screenshot; then a rebuild to 3 × 5 × 7 and
        // back, every cell noted each time.
        private static IEnumerator CheckBoard()
        {
            Aleatora game = Aleatora.Instance;
            if (!SelfTest.Check(game != null && game.Board != null && game.Board.Notes != null,
                "the bootstrap made the board's notes, from the tuning's set"))
            {
                yield break;
            }

            EmitterTuning tuning = game.Tuning.Emitter;
            CellLayout before = tuning.Layout;
            foreach (CellLayout layout in new[] { CellLayout.Scattered, CellLayout.Clustered, CellLayout.Axes })
            {
                tuning.Layout = layout;
                yield return null;
                CheckNotes(game, layout.ToString());
                yield return new WaitForSecondsRealtime(0.5f);
                yield return SelfTest.Capture("3-Board-" + layout);
            }

            tuning.Layout = before;
            yield return ShowBoard(game.Grid, game.Tuning.Grid, 3, 5, 7, HexOrientation.Flat, 57, null);
            CheckNotes(game, "3 × 5 × 7");
            yield return ShowBoard(game.Grid, game.Tuning.Grid, 5, 5, 5, HexOrientation.Flat, 61, null);
            CheckNotes(game, "5 × 5 × 5 again");
        }

        // The emitter (the emitter plan, EM-2): the bootstrap gave the scene's cone its Emitter; aimed
        // at the centre cell, Space sprays for two seconds and spheres hit cells, which glow; at a
        // high rate Max Spheres holds; under the session menu nothing is launched, and spraying goes
        // on when it closes; Space stops it. EM-3: the two seconds' hits go out as notes into a
        // RecordingOutput (CheckEmitterNotes), which is closed again for the MIDI checks.
        private static IEnumerator CheckEmitter()
        {
            Aleatora game = Aleatora.Instance;
            Emitter emitter = game != null ? game.Emitter : null;
            GameObject cone = GameObject.Find("Emitter");
            if (!SelfTest.Check(emitter != null && cone != null && emitter.gameObject == cone && game.Board.Notes != null,
                "the bootstrap gave the scene's Emitter object its emitter"))
            {
                yield break;
            }

            EmitterTuning tuning = game.Tuning.Emitter;
            HexGrid grid = game.Grid;
            HexCoord centre = game.Board.Notes.Centre;
            emitter.Clear();
            emitter.AimAt(grid.TopOf(centre));
            yield return null;
            Vector3 toAim = emitter.AimPoint - emitter.transform.position;
            SelfTest.Check(Vector3.Angle(-emitter.transform.up, toAim) < 0.5f && emitter.Nozzle.y < emitter.transform.position.y
                && emitter.Nozzle.y > grid.TopOf(centre).y,
                $"the cone points at the centre cell {centre}, its nozzle at {emitter.Nozzle} below it and above the board");

            int strays = 0;
            foreach (Collider collider in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                strays += collider.GetComponentInParent<HexCellView>() == null && collider.GetComponent<EmitterSphere>() == null ? 1 : 0;
            }

            SelfTest.Check(strays == 0, $"the only colliders are the cells' and the spheres': {strays} others");

            var hits = new List<EmitterHit>();
            int glowed = 0, noted = 0, near = 0, hard = 0;
            void OnHit(EmitterHit hit)
            {
                hits.Add(hit);
                glowed += grid.TryGetCell(hit.Cell, out HexCellView cell) && cell.HitAmount >= 1f ? 1 : 0;
                noted += hit.Note >= 0 && hit.Note == game.Board.NoteAt(hit.Cell) ? 1 : 0;
                near += HexCoord.Distance(hit.Cell, centre) <= 2 ? 1 : 0;
                hard += hit.Speed >= tuning.MinSpeed ? 1 : 0;
            }

            SelfTest.Check(!emitter.Spraying && emitter.Spheres.Count == 0, "the emitter isn't spraying at the start");

            // EM-3: the hits' notes into a RecordingOutput, at 120 BPM on Aleatora's own line, sixteenths at strength 1.
            MidiPlayback midi = game.Midi;
            var recording = new RecordingOutput("Menu check emitter", () => midi.Now);
            midi.Use(recording);
            int bpmBefore = midi.Bpm;
            QuantizeGrid gridBefore = tuning.Grid;
            float strengthBefore = tuning.Strength;
            midi.Bpm = 120;
            tuning.Grid = QuantizeGrid.Sixteenth;
            tuning.Strength = 1f;

            emitter.Hit += OnHit;
            yield return QueuedInput.Press(Key.Space);
            SelfTest.Check(emitter.Spraying, "Space turns spraying on");

            int most = 0;
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 2f)
            {
                most = Math.Max(most, emitter.Spheres.Count);
                yield return null;
            }

            yield return SelfTest.Capture("3-Emitter");
            Debug.Log($"Menu check: emitter at {tuning.Rate}/s for 2 s: {emitter.Launched} launched, {hits.Count} hits, at most {most} spheres; "
                + "the first: " + string.Join("; ", hits.GetRange(0, Math.Min(8, hits.Count))));
            SelfTest.Check(emitter.Launched >= 4 && most <= tuning.MaxSpheres, $"spraying launched spheres: {emitter.Launched}, at most {most} at once");
            SelfTest.Check(hits.Count > 0, $"spheres hit cells: {hits.Count} hits");
            SelfTest.Check(glowed == hits.Count, $"every cell hit glowed: {glowed} of {hits.Count}");
            SelfTest.Check(noted == hits.Count, $"every hit carries its cell's note: {noted} of {hits.Count}");
            SelfTest.Check(hard == hits.Count, $"every hit was at Min Speed or faster: {hard} of {hits.Count}");
            SelfTest.Check(near * 2 > hits.Count, $"most hits are within two cells of the centre cell aimed at: {near} of {hits.Count}");
            CheckEmitterNotes(midi, recording, tuning, hits, 0.25d, "a sixteenth");
            midi.Close();
            midi.Bpm = bpmBefore;
            tuning.Grid = gridBefore;
            tuning.Strength = strengthBefore;

            // Max Spheres holds at a high rate.
            float rateBefore = tuning.Rate;
            int capBefore = tuning.MaxSpheres;
            tuning.Rate = 50f;
            tuning.MaxSpheres = 20;
            most = 0;
            start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 1.5f)
            {
                most = Math.Max(most, emitter.Spheres.Count);
                yield return null;
            }

            SelfTest.Check(most == 20, $"at 50 a second, never more than Max Spheres (20) at once: at most {most}");
            tuning.Rate = rateBefore;
            tuning.MaxSpheres = capBefore;
            SelfTest.Progress();

            // Under the session menu nothing is launched; closing it, spraying goes on.
            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Count == 1 && MenuFocus.IsAnyOpen, "Escape opens the session menu over the spray");
            int launched = emitter.Launched;
            yield return new WaitForSecondsRealtime(0.75f);
            SelfTest.Check(emitter.Launched == launched && emitter.CurrentRate == 0f && emitter.Spraying,
                $"under the session menu spraying pauses: {emitter.Launched - launched} launched");
            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Count == 0 && !MenuFocus.IsAnyOpen, "Escape closes the session menu");
            yield return new WaitForSecondsRealtime(0.75f);
            SelfTest.Check(emitter.Launched > launched, $"with the menu closed spraying goes on: {emitter.Launched - launched} launched");

            yield return QueuedInput.Press(Key.Space);
            launched = emitter.Launched;
            yield return new WaitForSecondsRealtime(0.5f);
            SelfTest.Check(!emitter.Spraying && emitter.Launched == launched, "Space turns spraying off");

            emitter.Hit -= OnHit;
            emitter.Clear();
            SelfTest.Check(emitter.Spheres.Count == 0, "Clear puts every sphere away");
            SelfTest.Progress();

            // The physics' cost (the plan's Risks): Max Spheres on the board, on the default board and on the largest.
            yield return MeasureSpray(game, "5 × 5 × 5");
            yield return ShowBoard(grid, game.Tuning.Grid, 40, 40, 40, HexOrientation.Flat, 4681, null);
            emitter.AimAt(grid.TopOf(game.Board.Notes.Centre));
            yield return MeasureSpray(game, "40 × 40 × 40");
            yield return ShowBoard(grid, game.Tuning.Grid, 5, 5, 5, HexOrientation.Flat, 61, null);
            emitter.AimAt(grid.TopOf(game.Board.Notes.Centre));
            midi.Panic(); // the emitter's notes still waiting or sounding, forgotten before the MIDI checks
            SelfTest.Check(!midi.Scheduler.HasWork && !midi.IsOpen, "after the emitter's checks no note waits or sounds, and no output is open");
        }

        // EM-3: the hits of two seconds' spray went out as notes, on the clock thread, into the
        // RecordingOutput: note ons arrived, each within 5 ms of a grid point (step beats apart: a
        // sixteenth, ¼) on the scheduler's beat line at 120 BPM; each pitch a struck cell's; each
        // velocity in Min to Max Velocity; each on the tuning's channel. Panic leaves nothing
        // sounding and forgets what waits.
        private static void CheckEmitterNotes(MidiPlayback midi, RecordingOutput recording, EmitterTuning tuning, List<EmitterHit> hits,
            double step, string stepName)
        {
            RecordingOutput.Sent[] sent = recording.Messages;
            var pitches = new HashSet<int>();
            foreach (EmitterHit hit in hits)
            {
                pitches.Add(hit.Note);
            }

            int ons = 0, offGrid = 0, wrongPitch = 0, wrongVelocity = 0, wrongChannel = 0;
            double worst = 0d;
            foreach (RecordingOutput.Sent one in sent)
            {
                if (!MidiMessage.IsNoteOn(one.Message))
                {
                    continue;
                }

                ons++;
                double beat = midi.Scheduler.BeatAt(one.Time);
                double error = Math.Abs(beat - (Math.Round(beat / step) * step)) * 60d / 120d;
                worst = Math.Max(worst, error);
                offGrid += error > 0.005d ? 1 : 0;
                wrongPitch += pitches.Contains(MidiMessage.Data1(one.Message)) ? 0 : 1;
                int velocity = MidiMessage.Data2(one.Message);
                wrongVelocity += velocity >= tuning.MinVelocity && velocity <= tuning.MaxVelocity ? 0 : 1;
                wrongChannel += MidiMessage.Channel(one.Message) == tuning.Channel ? 0 : 1;
            }

            HitScheduler scheduler = midi.Scheduler;
            Debug.Log($"Menu check: emitter notes: {hits.Count} hits, {ons} note ons, {scheduler.Merged} merged, {scheduler.Dropped} dropped, "
                + $"the worst {worst * 1000d:0.00} ms off {stepName}; the first: "
                + string.Join("; ", Array.ConvertAll(sent, one => one.ToString()), 0, Math.Min(12, sent.Length)));
            SelfTest.Check(ons > 0, $"the hits went out as note ons: {ons} for {hits.Count} hits");
            SelfTest.Check(offGrid == 0, $"each note on within 5 ms of {stepName} at 120 BPM: {offGrid} off, the worst {worst * 1000d:0.00} ms");
            SelfTest.Check(wrongPitch == 0, $"each note on is a struck cell's note: {wrongPitch} not");
            SelfTest.Check(wrongVelocity == 0, $"each velocity in {tuning.MinVelocity} to {tuning.MaxVelocity}: {wrongVelocity} not");
            SelfTest.Check(wrongChannel == 0, $"each on the emitter's channel {tuning.Channel}: {wrongChannel} not");

            midi.Panic();
            SelfTest.Check(recording.SoundingCount == 0 && !scheduler.HasWork, "Panic leaves nothing of the emitter's sounding or waiting");
        }

        // The emitter's panel (the emitter plan, EM-4): the bootstrap made it at the right of the
        // screen, it isn't a menu, H hides and shows it and a menu hides it, with a screenshot with
        // it and without. A click on it selects no cell, though a cell lies under it (the board grown
        // to 21 × 21 × 21 for that), and a click on a cell beside it still does. Each control changes
        // what it names: Set, Tonic, Layout and Seed the board's notes, Show degrees the labels, each
        // slider its value on the tuning (and the panel follows a value set on the tuning, as the
        // Inspector would); Spray, Grid and Channel together: two seconds' spray at 120 BPM whose
        // note ons land on eighths, on channel 3.
        private static IEnumerator CheckPanel()
        {
            Aleatora game = Aleatora.Instance;
            EmitterPanel panel = game != null ? game.Panel : null;
            if (!SelfTest.Check(panel != null && panel.IsShowing && game.Emitter != null, "the bootstrap made the emitter's panel, and it shows"))
            {
                yield break;
            }

            RectTransform box = panel.Box;
            EmitterTuning tuning = game.Tuning.Emitter;
            Emitter emitter = game.Emitter;
            var corners = new Vector3[4];
            box.GetWorldCorners(corners); // an overlay canvas: world is screen
            SelfTest.Check(corners[0].x > Screen.width * 0.6f && corners[2].x <= Screen.width + 0.5f && corners[0].y >= 0f
                && corners[2].y <= Screen.height + 0.5f,
                $"the panel is at the right of the screen: x {corners[0].x:0} to {corners[2].x:0} of {Screen.width}");
            SelfTest.Check(!MenuFocus.IsAnyOpen, "the panel isn't a menu: with it showing, no menu is open");
            yield return SelfTest.Capture("3-Panel");
            ScrollRect scroll = box.GetComponentInChildren<ScrollRect>();
            scroll.verticalNormalizedPosition = 0f;
            yield return null;
            yield return SelfTest.Capture("3-Panel-Notes");
            scroll.verticalNormalizedPosition = 1f;

            yield return QueuedInput.Press(Key.H);
            SelfTest.Check(panel.Hidden && !panel.IsShowing, "H hides the panel");
            yield return new WaitForSecondsRealtime(0.3f);
            yield return SelfTest.Capture("3-Panel-Hidden");
            yield return QueuedInput.Press(Key.H);
            SelfTest.Check(!panel.Hidden && panel.IsShowing, "H shows it again");
            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Count == 1 && !panel.IsShowing, "under the session menu the panel is hidden");
            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Count == 0 && panel.IsShowing, "with the menu closed it shows again");

            // Clicks, through the input system as the mouse's own.
            GridSelection selection = game.Selection;
            HexGrid grid = game.Grid;
            yield return ShowBoard(grid, game.Tuning.Grid, 21, 21, 21, HexOrientation.Flat, 1261, null);
            var onPanel = new Vector2(corners[0].x + 6f, (corners[0].y + corners[2].y) * 0.5f); // its left margin, halfway up: no control
            bool cellUnder = selection.PickAt(onPanel) && selection.HasSelection;
            SelfTest.Check(cellUnder, $"a cell lies under the panel's left edge at ({onPanel.x:0}, {onPanel.y:0}): "
                + (cellUnder ? selection.Selected.ToString() : "none"));
            selection.Deselect();
            yield return ClickAt(onPanel);
            SelfTest.Check(!selection.HasSelection, "a click on the panel selects no cell");
            HexCoord centre = game.Board.Notes.Centre;
            Vector2 onCell = Camera.main.WorldToScreenPoint(grid.TopOf(centre));
            yield return ClickAt(onCell);
            SelfTest.Check(selection.HasSelection && selection.Selected == centre,
                "a click on the centre cell beside the panel selects it: " + (selection.HasSelection ? selection.Selected.ToString() : "nothing"));
            selection.Deselect();
            yield return MoveMouse(new Vector2(2f, 2f));
            yield return ShowBoard(grid, game.Tuning.Grid, 5, 5, 5, HexOrientation.Flat, 61, null);
            SelfTest.Progress();

            // The board's controls.
            centre = game.Board.Notes.Centre;
            string setBefore = tuning.Set;
            int tonicBefore = tuning.Tonic;
            CellLayout layoutBefore = tuning.Layout;
            ulong seedBefore = tuning.Seed;
            TMP_Dropdown set = Control<TMP_Dropdown>(box, "Dropdown: Set");
            TMP_Dropdown tonic = Control<TMP_Dropdown>(box, "Dropdown: Tonic");
            TMP_Dropdown layout = Control<TMP_Dropdown>(box, "Dropdown: Layout");
            TMP_InputField seed = Control<TMP_InputField>(box, "InputField: Seed");
            Toggle degrees = Control<Toggle>(box, "Toggle: Show degrees");
            if (!SelfTest.Check(set != null && tonic != null && layout != null && seed != null && degrees != null,
                "the panel has Set, Tonic, Layout, Seed and Show degrees"))
            {
                yield break;
            }

            IReadOnlyList<string> sets = MarkovLibrary.Names;
            int other = 0;
            while (other < sets.Count - 1 && sets[other] == setBefore)
            {
                other++;
            }

            set.value = other;
            yield return null;
            SelfTest.Check(tuning.Set == sets[other] && game.Board.Table != null && game.Board.Table.Name == sets[other],
                $"Set on the panel gives the board {sets[other]}: " + (game.Board.Table != null ? game.Board.Table.Name : "none"));
            set.value = Math.Max(0, IndexOf(sets, setBefore));
            yield return null;
            SelfTest.Check(game.Board.Table != null && game.Board.Table.Name == setBefore, $"and back to {setBefore}");

            int noteBefore = game.Board.NoteAt(centre);
            tonic.value = (tonicBefore + 2) % 12;
            yield return null;
            int rose = (((game.Board.NoteAt(centre) - noteBefore) % 12) + 12) % 12;
            SelfTest.Check(tuning.Tonic == (tonicBefore + 2) % 12 && rose == 2, $"Tonic on the panel moves the board's notes up a tone: {rose} semitones");
            tonic.value = tonicBefore;

            tuning.Layout = CellLayout.Scattered;
            yield return null;
            int[] scattered = Degrees(game);
            layout.value = (int)CellLayout.Clustered;
            yield return null;
            SelfTest.Check(tuning.Layout == CellLayout.Clustered && game.Board.Notes.Layout == CellLayout.Clustered && !Same(scattered, Degrees(game)),
                "Layout on the panel lays the board out clustered");
            layout.value = (int)CellLayout.Axes;
            yield return null;
            SelfTest.Check(game.Board.Notes.Layout == CellLayout.Axes && game.Board.DegreeAt(centre) == 0,
                "and along the axes, 1 on the centre cell");
            layout.value = (int)CellLayout.Scattered;
            yield return null;

            seed.text = "7";
            seed.onEndEdit.Invoke("7");
            yield return null;
            SelfTest.Check(tuning.Seed == 7UL && !Same(scattered, Degrees(game)), $"Seed on the panel gives another board: seed {tuning.Seed}");
            seed.text = "seven";
            seed.onEndEdit.Invoke("seven");
            SelfTest.Check(tuning.Seed == 7UL && seed.text == "7", $"a seed that isn't a whole number puts the last back: {seed.text}");
            seed.text = seedBefore.ToString(System.Globalization.CultureInfo.InvariantCulture);
            seed.onEndEdit.Invoke(seed.text);
            yield return null;
            SelfTest.Check(tuning.Seed == seedBefore && Same(scattered, Degrees(game)), "the seed back, the board as it was");
            tuning.Layout = layoutBefore;

            degrees.isOn = false;
            yield return null;
            SelfTest.Check(!tuning.ShowDegrees && game.Board.Labels.Count > 0 && !game.Board.Labels[0].gameObject.activeSelf,
                "Show degrees off on the panel hides the cells' labels");
            degrees.isOn = true;
            yield return null;
            SelfTest.Check(game.Board.Labels[0].gameObject.activeSelf, "on again shows them");

            // Each slider sets its value; then back.
            var sliders = new (string Name, float Value, Func<float> Read)[]
            {
                ("Rate", 20f, () => tuning.Rate), ("Spread", 30f, () => tuning.Spread), ("Speed", 12f, () => tuning.Speed),
                ("Speed jitter", 25f, () => tuning.SpeedJitter * 100f), ("Min size", 0.3f, () => tuning.MinSize),
                ("Max size", 0.8f, () => tuning.MaxSize), ("Bounciness", 0.3f, () => tuning.Bounciness),
                ("Lifetime", 5f, () => tuning.Lifetime), ("Max spheres", 150f, () => tuning.MaxSpheres),
                ("Strength", 0.5f, () => tuning.Strength), ("Min speed", 2f, () => tuning.MinSpeed),
                ("Max speed", 20f, () => tuning.MaxSpeed), ("Min velocity", 40f, () => tuning.MinVelocity),
                ("Max velocity", 100f, () => tuning.MaxVelocity), ("Velocity curve", 2f, () => tuning.VelocityCurve),
                ("Shortest", 0.5f, () => tuning.Shortest), ("Longest", 3f, () => tuning.Longest), ("Max voices", 16f, () => tuning.MaxVoices),
            };
            var before = new float[sliders.Length];
            var wrong = new List<string>();
            for (int i = 0; i < sliders.Length; i++)
            {
                Slider slider = Control<Slider>(box, "Slider: " + sliders[i].Name);
                before[i] = sliders[i].Read();
                if (slider == null)
                {
                    wrong.Add(sliders[i].Name + " missing");
                    continue;
                }

                slider.value = sliders[i].Value;
                if (Mathf.Abs(sliders[i].Read() - sliders[i].Value) > 1e-3f)
                {
                    wrong.Add($"{sliders[i].Name} {sliders[i].Read()}");
                }
            }

            SelfTest.Check(wrong.Count == 0, $"each of the panel's {sliders.Length} sliders sets its value on the tuning: "
                + (wrong.Count == 0 ? "all" : string.Join(", ", wrong)));
            for (int i = sliders.Length - 1; i >= 0; i--) // back in reverse, so Min size's push on Max size is undone last
            {
                Slider slider = Control<Slider>(box, "Slider: " + sliders[i].Name);
                if (slider != null)
                {
                    slider.value = before[i];
                }
            }

            wrong.Clear();
            for (int i = 0; i < sliders.Length; i++)
            {
                if (Mathf.Abs(sliders[i].Read() - before[i]) > 1e-3f)
                {
                    wrong.Add($"{sliders[i].Name} {sliders[i].Read()} not {before[i]}");
                }
            }

            SelfTest.Check(wrong.Count == 0, "and back as they were: " + (wrong.Count == 0 ? "all" : string.Join(", ", wrong)));

            // The smallest size pushes the largest up; the panel follows a value set on the tuning.
            Slider minSize = Control<Slider>(box, "Slider: Min size");
            Slider maxSize = Control<Slider>(box, "Slider: Max size");
            float minSizeBefore = tuning.MinSize, maxSizeBefore = tuning.MaxSize, rateBefore = tuning.Rate;
            minSize.value = 0.9f;
            tuning.Rate = 7f;
            yield return null;
            Slider rate = Control<Slider>(box, "Slider: Rate");
            SelfTest.Check(Mathf.Approximately(tuning.MaxSize, 0.9f) && Mathf.Approximately(maxSize.value, 0.9f),
                $"Min size past Max size pushes it up: {tuning.MaxSize:0.00}, the slider {maxSize.value:0.00}");
            SelfTest.Check(Mathf.Approximately(rate.value, 7f), $"the panel follows a Rate set on the tuning: {rate.value:0.00}");
            tuning.MinSize = minSizeBefore;
            tuning.MaxSize = maxSizeBefore;
            tuning.Rate = rateBefore;

            // Spray, Grid and Channel: two seconds' spray from the panel, its notes on eighths on channel 3.
            Toggle spray = Control<Toggle>(box, "Toggle: Spray");
            TMP_Dropdown gridChoice = Control<TMP_Dropdown>(box, "Dropdown: Grid");
            TMP_Dropdown channel = Control<TMP_Dropdown>(box, "Dropdown: Channel");
            Toggle quantizeLength = Control<Toggle>(box, "Toggle: Quantize length");
            if (!SelfTest.Check(spray != null && gridChoice != null && channel != null && quantizeLength != null,
                "the panel has Spray, Grid, Channel and Quantize length"))
            {
                yield break;
            }

            quantizeLength.isOn = !tuning.QuantizeLength;
            SelfTest.Check(tuning.QuantizeLength == quantizeLength.isOn, "Quantize length on the panel sets it");
            quantizeLength.isOn = !quantizeLength.isOn;

            yield return QueuedInput.Press(Key.Space);
            yield return null;
            SelfTest.Check(emitter.Spraying && spray.isOn, "Space turns spraying on, and the panel's Spray shows it");
            yield return QueuedInput.Press(Key.Space);
            yield return null;
            SelfTest.Check(!emitter.Spraying && !spray.isOn, "Space off, and Spray shows it");
            emitter.Clear();

            MidiPlayback midi = game.Midi;
            var recording = new RecordingOutput("Menu check panel", () => midi.Now);
            midi.Use(recording);
            int bpmBefore = midi.Bpm;
            QuantizeGrid gridBefore = tuning.Grid;
            int channelBefore = tuning.Channel;
            float strengthBefore = tuning.Strength;
            midi.Bpm = 120;
            gridChoice.value = (int)QuantizeGrid.Eighth;
            channel.value = 3 - MidiMessage.LowestChannel;
            Control<Slider>(box, "Slider: Strength").value = 1f;
            SelfTest.Check(tuning.Grid == QuantizeGrid.Eighth && tuning.Channel == 3 && tuning.Strength == 1f,
                $"Grid, Channel and Strength on the panel set them: {tuning.Grid}, {tuning.Channel}, {tuning.Strength}");

            var hits = new List<EmitterHit>();
            void OnHit(EmitterHit hit) => hits.Add(hit);
            emitter.Hit += OnHit;
            emitter.AimAt(grid.TopOf(centre));
            spray.isOn = true;
            SelfTest.Check(emitter.Spraying, "Spray on the panel turns spraying on");
            yield return new WaitForSecondsRealtime(2f);
            spray.isOn = false;
            SelfTest.Check(!emitter.Spraying, "and off");
            emitter.Hit -= OnHit;
            CheckEmitterNotes(midi, recording, tuning, hits, 0.5d, "an eighth");
            midi.Close();
            midi.Bpm = bpmBefore;
            tuning.Grid = gridBefore;
            tuning.Channel = channelBefore;
            tuning.Strength = strengthBefore;
            emitter.Clear();
            yield return null;
            SelfTest.Check(gridChoice.value == (int)gridBefore && channel.value == channelBefore - MidiMessage.LowestChannel,
                "the panel follows the grid and channel put back on the tuning");
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            SelfTest.Check(selected == null || !selected.transform.IsChildOf(box), "the panel holds no selection: " + SelectedName());
            SelfTest.Progress();
        }

        private static int[] Degrees(Aleatora game)
        {
            CellNotes notes = game.Board.Notes;
            var degrees = new int[notes.Shape.Count];
            for (int i = 0; i < degrees.Length; i++)
            {
                degrees[i] = notes.DegreeAt(i);
            }

            return degrees;
        }

        private static bool Same(int[] a, int[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static int IndexOf(IReadOnlyList<string> list, string item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == item)
                {
                    return i;
                }
            }

            return -1;
        }

        // The mouse moved to a point of the screen, queued as the device's own, with frames for the UI to see it.
        private static IEnumerator MoveMouse(Vector2 screen)
        {
            Mouse mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            if (!mouse.enabled)
            {
                InputSystem.EnableDevice(mouse);
            }

            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen });
            yield return null;
            yield return null;
        }

        // A left click at a point of the screen through the input system: there, pressed, released.
        private static IEnumerator ClickAt(Vector2 screen)
        {
            yield return MoveMouse(screen);
            Mouse mouse = Mouse.current;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen });
            yield return null;
            yield return null;
        }

        // Times the frames for a second with no spheres, then sprays at 50 a second until Max Spheres
        // are out (or 10 s) and times two seconds more, with the hits a second. A reading, not a check.
        private static IEnumerator MeasureSpray(Aleatora game, string what)
        {
            Emitter emitter = game.Emitter;
            EmitterTuning tuning = game.Tuning.Emitter;
            var idle = new float[2];
            yield return new WaitForSecondsRealtime(0.5f);
            yield return TimeFrames(1f, idle);

            float rateBefore = tuning.Rate;
            tuning.Rate = 50f;
            emitter.Spraying = true;
            float start = Time.realtimeSinceStartup;
            while (emitter.Spheres.Count < tuning.MaxSpheres && Time.realtimeSinceStartup - start < 10f)
            {
                yield return null;
            }

            int spheres = emitter.Spheres.Count;
            int hitsBefore = emitter.Hits;
            var spraying = new float[2];
            yield return TimeFrames(2f, spraying);
            Debug.Log($"Menu check: spray cost on {what} ({game.Grid.CellCount} cells), fixed step {Time.fixedDeltaTime * 1000f:0.#} ms: "
                + $"no spheres {idle[0]:0.0} ms a frame (the longest {idle[1]:0.0}); {spheres} of {tuning.MaxSpheres} spheres at 50 a second "
                + $"{spraying[0]:0.0} ms a frame (the longest {spraying[1]:0.0}), {(emitter.Hits - hitsBefore) / 2f:0} hits a second");
            emitter.Spraying = false;
            tuning.Rate = rateBefore;
            emitter.Clear();
            SelfTest.Progress();
        }

        // The mean and the longest frame over some seconds, in milliseconds, into result[0] and result[1].
        private static IEnumerator TimeFrames(float seconds, float[] result)
        {
            int frames = 0;
            float total = 0f, longest = 0f;
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < seconds)
            {
                yield return null;
                frames++;
                total += Time.unscaledDeltaTime;
                longest = Mathf.Max(longest, Time.unscaledDeltaTime);
            }

            result[0] = total / Math.Max(1, frames) * 1000f;
            result[1] = longest * 1000f;
        }

        // Every cell: a degree of the set's mode; its octave by its ring (the centre lowest); its MIDI
        // note from the tonic, the lowest octave, its octave and its degree; its colour at rest its
        // degree's (the root red, the likeliest other degree full blue); its label its degree's name.
        private static void CheckNotes(Aleatora game, string what)
        {
            BoardNotes board = game.Board;
            CellNotes notes = board.Notes;
            HexGrid grid = game.Grid;
            EmitterTuning tuning = game.Tuning.Emitter;
            if (!SelfTest.Check(notes != null && notes.Shape == grid.Shape && notes.Layout == tuning.Layout && board.Labels.Count == grid.CellCount,
                $"{what}: the notes and labels are for the board as it is"))
            {
                return;
            }

            int mask = ModeScale.Mask(board.Table.Mode);
            int count = grid.CellCount;
            int inMode = 0, octaves = 0, midi = 0, colours = 0, labels = 0;
            var perDegree = new int[Degree.Count];
            for (int i = 0; i < count; i++)
            {
                HexCoord coord = grid.Shape.Cells[i];
                int degree = notes.DegreeAt(i);
                perDegree[degree]++;
                inMode += ModeScale.Has(mask, degree) ? 1 : 0;
                int octave = HexCoord.Distance(coord, notes.Centre) * notes.Octaves / notes.Rings;
                octaves += notes.OctaveAt(i) == octave ? 1 : 0;
                int note = ((tuning.LowestOctave + 1 + octave) * Degree.Count) + tuning.Tonic + degree;
                midi += board.NoteAt(coord) == note ? 1 : 0;
                colours += grid.TryGetCell(coord, out HexCellView cell) && cell.OwnRestColor == board.ColorOf(degree) ? 1 : 0;
                TextMeshPro label = board.Labels[i];
                labels += label.text == ViewText.DegreeName(degree) && label.gameObject.activeSelf == tuning.ShowDegrees ? 1 : 0;
            }

            int likeliest = 1;
            for (int d = 2; d < Degree.Count; d++)
            {
                likeliest = notes.Probability(d) > notes.Probability(likeliest) ? d : likeliest;
            }

            var shares = new System.Text.StringBuilder();
            for (int d = 0; d < Degree.Count; d++)
            {
                if (perDegree[d] > 0)
                {
                    shares.Append(Degree.Name(d)).Append('×').Append(perDegree[d]).Append(' ');
                }
            }

            Debug.Log($"Menu check: board {what}, {notes.Layout}, {board.Table.Name}, centre {notes.Centre}, {notes.Rings} rings: {shares}");
            SelfTest.Check(inMode == count, $"{what}: every cell's degree is in the mode: {inMode} of {count}");
            SelfTest.Check(octaves == count, $"{what}: every cell's octave is its ring's: {octaves} of {count}");
            SelfTest.Check(midi == count, $"{what}: every cell's note is its tonic, octave and degree: {midi} of {count}");
            SelfTest.Check(colours == count, $"{what}: every cell is coloured for its degree: {colours} of {count}");
            SelfTest.Check(board.ColorOf(0) == tuning.RootColor && board.ColorOf(likeliest) == tuning.LikelyColor,
                $"{what}: the root's cells are the root colour, {Degree.Name(likeliest)}'s (the likeliest but the root) the likely colour");
            SelfTest.Check(labels == count, $"{what}: every cell's label is its degree: {labels} of {count}");
        }

        // The tables are in the player's Resources: the index lists them and each reads (a set that
        // doesn't logs an error, which fails the run by itself). Each makes a melody at order 4 that
        // keeps to its mode at ChromaticWeight 0 (TH-1); the theory sets' melodies are logged.
        private static void CheckTables()
        {
            IReadOnlyList<string> names = MarkovLibrary.Names;
            SelfTest.Check(names.Count == 22, $"the index lists the 22 sets: {names.Count}");
            int read = 0;
            int inMode = 0;
            GeneratorSettings settings = GeneratorSettings.Default;
            settings.Order = 4;
            settings.ChromaticWeight = 0f;
            float[] distribution = new float[Degree.Count];
            foreach (string name in names)
            {
                if (!MarkovLibrary.TryGet(name, out MarkovTable table) || table.Name != name || table.MaxOrder != 4)
                {
                    continue;
                }

                read++;
                int mask = ModeScale.Mask(table.Mode);
                var generator = new MelodyGenerator(table, settings, 2112);
                var melody = new System.Text.StringBuilder();
                bool kept = true;
                for (int i = 0; i < 200; i++)
                {
                    int degree = generator.Next(distribution).Degree;
                    kept &= ModeScale.Has(mask, degree);
                    if (i < 24)
                    {
                        melody.Append(Degree.Name(degree)).Append(' ');
                    }
                }

                inMode += kept ? 1 : 0;
                if (table.Corpus == "virtuoso")
                {
                    Debug.Log($"Menu check: {name}, seed 2112, order 4: {melody}…");
                }
            }

            SelfTest.Check(read == names.Count, $"every set reads, with its four orders: {read} of {names.Count}");
            SelfTest.Check(inMode == names.Count, $"every set's order-4 melody keeps to its mode at chromatic weight 0: {inMode} of {names.Count}");
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
