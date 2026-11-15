# Aleatora — MIDI out, first proof: plan

What to build, in what order, and what "done" means for each milestone: Aleatora's first MIDI. A
port to send through, a screen to choose it, and the *Melodies* page playing its melody out as
note on and note off, one quarter note a beat at a BPM, on a chosen channel. The proof is Jon's:
notes from Aleatora reach Reaper through loopMIDI. One direction only; MIDI in comes later.

Jon's brief (2026-11-15; Design, ruling 26): decide on and integrate a MIDI library, or build our
own; a MIDI device selection screen (Jon uses loopMIDI between Aleatora and Reaper); a MIDI channel
button, a BPM box and the usual playback controls, loop included, on the *Melodies* page; the MVP
emits note on and off on that channel, driven by the generated melody, as quarter notes at the BPM.

**Not in this plan:** MIDI in, MIDI clock or sync, OSC, what the grid means or drives, rhythm other
than quarter notes, platforms other than Windows. *Later* lists them.

Written 2026-11-15 from what was on disk that day: `main` after M4 (the Markov plan finished).

---

## Read first

1. `Aleatora/CLAUDE.md`, then `Docs/Aleatora_Handoff.md`.
2. `Docs/Aleatora_Markov_Plan.md`: *How we work* and *Engineering rules*. They govern here too,
   with the additions below.
3. `Docs/Aleatora_Tech.md`: *Melodies page*, *Checks*.
4. `Framework/README.md`: *Menu shell* (`OpenDialog`, `SettingsPage.More`), *Settings* (a game's
   own values by key), *UI kit*.
5. This plan's *Decisions for Jon*, then the milestone in hand.

## How we work

As the Markov plan's, with these milestones and branches. **Hard stop** after MIDI-3: Jon plays
into Reaper (MIDI-4). Rulings go into `Docs/Aleatora_Design.md`, what was built into
`Docs/Aleatora_Tech.md` under a new *MIDI* section.

## Engineering rules (added to the Markov plan's)

- **Still no sound.** Aleatora sends MIDI; it never makes sound, not even through Windows' own
  synthesiser (decision 3).
- **`Scripts/Midi/` is plain C#**, no `UnityEngine`, like `Music/`: the messages, the port, the
  player. EditMode tests run them whole against a recording output.
- **Timing is off the main thread.** A frame is 16 ms at 60 fps and stops while the window lacks
  focus, so notes are sent from a clock thread on a `Stopwatch`, never from `Update`. The thread
  touches no Unity API: it reads a copy of the notes under a lock. The main thread only reads the
  playhead.
- **No stuck notes.** Every note on gets its note off: on Stop, Pause, a change of channel or port,
  closing the page, quitting, and leaving play mode in the Editor. Closing a port first sends All
  Notes Off (CC 123) on the channel in use.
- **A port is always closed.** On quit, on `OnDisable`, and when the Editor leaves play mode or
  reloads scripts. A leaked handle keeps the port busy until Unity restarts.
- **Tests and the menu check never open a real port.** Jon's ports lead into Reaper. They play into
  a recording output.

## Decisions for Jon

Each has the default this plan builds if Jon doesn't say otherwise.

1. **The library.** **Default: our own**, a small wrapper over Windows' `winmm.dll` (the six calls
   that list, open, send to, reset and close an output port, and `timeBeginPeriod` for the clock).
   loopMIDI's ports are ordinary Windows MIDI ports, so it sees them. Nothing to install: no
   package, no `manifest.json` change, no native binary, no licence to credit. It sits behind an
   interface (`IMidiOutput`), so a library can replace it. What it costs: Windows only (macOS
   would need a CoreMIDI wrapper or a library), and MIDI in later is a few more `winmm` calls, with
   a callback. The alternatives:
   - **DryWetMIDI** 8.0.3 (Melanchall, MIT): devices on Windows and macOS, MIDI files, its own
     playback. A DLL and its native binaries in `Assets/Plugins/`. Worth it if MIDI files
     (export to Reaper by file) or macOS come soon.
   - **RtMidi for Unity** (keijiro, MIT): Windows, macOS, Linux, iOS, Android; a scoped registry
     and a package in `manifest.json`.
2. **Where the device screen lives.** **Default:** a *MIDI* page (a dialog) opened from a *MIDI*
   row on the Settings page and from an *Output* button on the *Melodies* page that shows the
   port's name. It lists the output ports as buttons, the chosen one in accent, with *None* first,
   then *Refresh* and *Back*. The choice is this machine's, saved in `Settings`
   (`Aurumworks.Midi.Output`, by name) and opened again at start. A saved port that's gone shows
   as "loopMIDI Port (not found)" and nothing opens. A port that fails to open (one that another
   program has open alone) shows Windows' error under the list. Or: a dropdown on the *Melodies*
   page only.
3. **Microsoft GS Wavetable Synth**, Windows' built-in output, is a synthesiser: Aleatora would
   sound through it. **Default: hidden from the list.** Or listed.
4. **The channel control.** Jon said "button". **Default:** a *Channel* dropdown, 1 to 16 (the
   kit's, like *Tonic*), default 1. Or a button that steps 1 → 16 and wraps.
5. **The BPM box.** A text field like *Seed*: whole numbers, 20 to 300, **default 120**; anything
   else puts the last value back. Its default is on the tuning asset.
6. **The note.** A quarter note at the BPM; the note off at **90%** of the beat (the gate), at
   velocity **100**. Both are on the tuning asset (a new `Midi` section), not on the page.
7. **The transport.** **Default:** *Play* (it becomes *Pause* while playing; *Play* again resumes),
   *Stop* (all notes off, back to the first note) and *Loop* (a toggle, off by default). It plays
   the melody as it stands, first note to last: 32 after *Generate*, more after *Step*. Loop off
   stops after the last note; loop on goes back to the first. Or loop as *endless*: keep stepping
   the generator.
8. **Changes while playing.** **Default:** *Generate* replaces the melody, and its first note
   comes on the next beat. *Step* adds a note at the end. *Tonic* re-voices, and the next note
   sent is in the new key. *BPM* applies from the next note. *Channel* ends the sounding note on
   the old channel. *Set*, *Order* and *Temperature* change only notes not yet made, as now. *Back*
   stops playback. A page opened over it (the *MIDI* page) doesn't.
9. **The playhead.** **Default:** the strip frames the sounding note's column. After *Step* past
   32, a note that is off the strip's left edge plays with nothing framed.
10. **Run In Background** (Player settings, off now). **Default: on**, so the page keeps up, with
    its playhead, while Reaper has focus. Notes keep playing either way: the clock thread doesn't
    pause.
11. **Where the transport goes on the page.** **Default:** a row across the top of the *Melody*
    strip's panel: *Play*, *Stop*, *Loop*, *Channel*, *BPM*, *Output*. The left column keeps the
    melody's controls. Jon looks at it in MIDI-4.

## Code layout

- **`Scripts/Midi/`** (plain C#):
  - `MidiMessage`: packs a short message the way `winmm` takes it: `NoteOn`, `NoteOff`,
    `ControlChange`, `AllNotesOff`. Channels are 1–16 everywhere but on the wire.
  - `IMidiOutput`: `Name`, `Send(int message)`, `Dispose()`.
  - `WinMm` (internal): the `DllImport`s and their error text (`midiOutGetErrorTextW`).
  - `MidiPorts`: `Outputs` (the ports' names, minus decision 3's), `TryOpen(name, out output,
    out error)`. An empty list on a platform other than Windows.
  - `RecordingOutput`: an `IMidiOutput` that keeps what it was sent and when, for the tests and
    the menu check.
  - `NotePlayer`: the schedule, with no clock of its own. It holds a copy of the notes, the BPM,
    gate, channel, velocity and loop. `Play(now)`, `Pause(now)`, `Stop()`, `SetNotes`, `SetBpm`,
    `SetChannel`, and `Advance(now, output)`, which sends everything due by `now` in order (a note
    off before a note on at the same time, so a repeated note sounds twice); `Position`, the
    sounding note. A test drives it with made-up times.
- **`Scripts/Game/`**:
  - `MidiTuning` (on `AleatoraTuning` as `Midi`): `Channel` 1, `Bpm` 120, `Velocity` 100, `Gate`
    0.9.
  - `MidiPlayback` (a MonoBehaviour, made by the `Aleatora` bootstrap): owns the open port and the
    clock thread; opens the saved port at start; `Play`, `Pause`, `Stop`, `Position`; closes it all
    as the rules say. The thread runs only while playing: `timeBeginPeriod(1)`, then
    `Advance` on the `Stopwatch` and `Sleep(1)`, about a millisecond late at worst.
- **`Scripts/View/`**: `MidiPage` (the device screen); `MelodiesPage` gains the transport;
  `MelodyStrip` gains the playhead.

## MIDI-1 — The port (branch `midi-1-port`)

**Tasks**

1. `MidiMessage`, `IMidiOutput`, `WinMm`, `MidiPorts`, `RecordingOutput` (decision 1).
2. `MidiTuning` on `AleatoraTuning` (added to `AleatoraTuning.asset` by `TuningMaker`'s way).
3. `MidiPlayback` with the port only: the saved choice (`Settings`), open, close, All Notes Off on
   close, closing on quit, `OnDisable` and leaving play mode.
4. EditMode tests: every message's bytes (note on and off on channels 1 and 16, CC 123), the
   channel clamped; `MidiPorts.Outputs` doesn't throw and has no GS Wavetable Synth. No port is
   opened.

**Done when** the tests pass and the menu check still passes.

## MIDI-2 — The player (branch `midi-2-player`)

**Tasks**

1. `NotePlayer` (decisions 6, 7, 8).
2. The clock thread in `MidiPlayback`.
3. EditMode tests, on `NotePlayer` with made-up times and a `RecordingOutput`: four notes at 120
   BPM go on at 0, 0.5, 1 and 1.5 s and off at 0.45 s after each; the channel and velocity are in
   the status and data bytes; a repeated note goes off, then on; loop off ends after the last note
   with nothing sounding; loop on wraps to the first; Pause and Stop leave nothing sounding and Play
   resumes or restarts; a BPM change applies from the next note; a channel change ends the note on
   the old one; `SetNotes` while playing starts the new notes on the next beat. And one on the
   real thread with a `RecordingOutput`: 16 notes at 300 BPM, each note on within 5 ms of its time
   (the spread logged).

**Done when** the tests pass.

## MIDI-3 — The screens (branch `midi-3-page`)

**Tasks**

1. `View/MidiPage` (decision 2): opened from Settings (`More`) and from *Output*. The kit names
   its buttons (`Button: loopMIDI Port`, `Button: None`, `Button: Refresh`) for the self-test.
2. The transport on the *Melodies* page (decisions 4, 5, 7, 11): `Button: Play`, `Button: Stop`,
   `Toggle: Loop`, `Dropdown: Channel`, `InputField: BPM`, `Button: Output`, in the navigation.
3. The playhead on `MelodyStrip` (decision 9), its frame's colour the theme's.
4. Run In Background on (decision 10; `ProjectSettings.asset`, approved by Jon's go-ahead).
5. **The menu check grows** (`MenuCheck.cs`), with a `RecordingOutput` in place of a port: the
   *MIDI* page from Settings and from *Output*, *None* first, no GS Wavetable Synth, screenshot
   (`5-Midi`); on *Melodies*, seed 1, *Generate*, *Channel* 3, *BPM* 300, *Play*: the first four
   note ons are the strip's first four notes on channel 3, each about 0.2 s apart, each followed
   by its note off; the playhead framed; screenshot (`5-Playing`); *Stop* leaves nothing sounding;
   *Loop* on with four notes wraps. Cancel closes the page with nothing sounding. `UiProbe.Reachable`
   reaches the new controls.
6. Look at the screenshots: the transport legible, nothing overlapping at the small size.

**Done when** the menu check passes with its new checks. **Hard stop.**

## MIDI-4 — Jon plays into Reaper

1. loopMIDI running, with a port (say "loopMIDI Port").
2. Reaper: *Preferences > MIDI Inputs*: enable loopMIDI Port. Under *MIDI Outputs*, leave it
   disabled, or Reaper's output feeds back into its input.
3. A track with an instrument (ReaSynth will do), armed, its input MIDI: loopMIDI Port, the
   channel Aleatora sends on (or all), monitoring on.
4. Aleatora: *Settings > MIDI*, or *Output* on the page: loopMIDI Port. On *Melodies*: the channel,
   Reaper's tempo as the BPM, *Play*. The notes sound in Reaper; recorded, they land a quarter note
   apart. Without clock sync they drift from Reaper's grid, and the first note isn't on a bar line;
   that's expected (*Later*).

Jon rules on the decisions he hasn't yet, the page's transport and the timing; `midi-3-page`
merges to `main`.

## Milestone summary

| # | Branch | Lands | Checked by |
|---|---|---|---|
| MIDI-1 | `midi-1-port` | The messages, the `winmm` port, the saved choice, `MidiTuning` | EditMode tests |
| MIDI-2 | `midi-2-player` | `NotePlayer`, the clock thread | EditMode tests |
| MIDI-3 | `midi-3-page` | The *MIDI* page, the transport, the playhead | Menu check with screenshots |
| MIDI-4 | merge | Jon's Reaper test, rulings | **Jon** |

Each milestone branches from the last; they merge together after MIDI-4.

## Later (each its own plan)

- **MIDI in** (bi-directional), on the same `winmm` wrapper or the library chosen then.
- **Clock and sync:** MIDI clock, Start and Stop out, so Reaper follows Aleatora's tempo, or
  following Reaper's.
- OSC; the grid driving notes; rhythm beyond quarter notes; MIDI files; macOS.

## Risks

- **Stuck notes and held ports**, above all across Editor play sessions and script reloads. The
  rules above, and a test that Stop and quitting leave nothing sounding.
- **Timing.** `Sleep(1)` is about 15 ms on Windows without `timeBeginPeriod(1)`; MIDI-2 measures
  the spread and reports it.
- **Port names:** `winmm` cuts them at 31 characters, and two ports can share a name. Saved by
  name, the first match opens.
- **Single-client ports.** Hardware ports another program holds won't open. The page shows
  Windows' error. loopMIDI's ports take many clients.
- **Windows MIDI Services** (Windows 11's new MIDI stack) keeps `winmm` working for older programs;
  if it changes how loopMIDI's ports appear, decision 1 is where to look again.
- **The page grows.** The *Melodies* page is full; the transport row takes height from the strip.
