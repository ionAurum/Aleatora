# Aleatora — MIDI in, clock and sync: plan

What to build, in what order, and what "done" means for each milestone: the layers under MIDI
in. Ports to listen on, messages arriving with their times, the keys held on a keyboard, and
MIDI clock both ways, so that Aleatora either follows Reaper's tempo and transport or leads
them. **What the incoming notes mean is not in this plan.** Jon will use a keyboard to play
chords into something he hasn't worked out yet. This plan only makes the data arrive, be
readable and be visible.

Jon's brief (2026-11-15; Design, ruling 37): build MIDI in and clock and sync with Reaper. The
technical base comes before any musical use. The keyboard is for chords and the like, later.

**Not in this plan:** anything the keyboard's notes do to the melody, the grid or the output;
MIDI thru (echoing input to the output); SysEx; MIDI Time Code; MIDI 2.0 and the Windows MIDI
Services API; OSC; platforms other than Windows. *Later* lists them.

Written 2026-11-15 from what was on disk that day: `main` after the MIDI plan (rulings to 36).

---

## Read first

1. `Aleatora/CLAUDE.md`, then `Docs/Aleatora_Handoff.md`.
2. `Docs/Aleatora_Midi_Plan.md`: *Engineering rules*. They hold here, with the additions below.
3. `Docs/Aleatora_Tech.md`: *MIDI* (all three *As built* sections), *Checks*.
4. This plan's *Decisions for Jon*, then the milestone in hand.

## How we work

As the MIDI plan's. Milestones IN-1 to IN-4, each on its own branch from the last, merged
together after IN-4. **Hard stop** after IN-3: Jon plays with Reaper and a keyboard (IN-4).
Rulings go into `Docs/Aleatora_Design.md`; what was built goes into `Docs/Aleatora_Tech.md`,
*MIDI*.

## Engineering rules (added to the MIDI plan's)

- **The input callback does almost nothing.** Windows calls it on its own thread. It stamps the
  message with `MidiPlayback`'s `Stopwatch`, queues it under a lock and signals an event. It
  makes no winmm call and touches no Unity API. It is a static method marked
  `[AOT.MonoPInvokeCallback]` that finds its port through the instance pointer (a `GCHandle`).
  That way it works under Mono, which the player uses today, and under IL2CPP.
- **The clock thread reads the input.** Today it runs only while Aleatora plays. From IN-1 it
  also runs while any input is open. It waits on the input's event with a 1 ms timeout
  (`WaitOne(1)` in place of `Sleep(1)`), so a clock tick is handled as it arrives, not up to a
  millisecond later. It feeds clock messages to the follower, notes to the held-notes model, and
  everything to a queue that the main thread drains each frame for the monitor.
- **An input is always closed:** `midiInStop`, `midiInReset`, `midiInClose`. This happens
  wherever an output closes (quitting, `OnDisable`, leaving play mode, a script reload), and
  never from inside the callback.
- **Tests and the menu check open no hardware input.** They feed made-up messages to a stand-in
  input. Proving the winmm wrapper needs a message sent and received through a loopMIDI port,
  and only one port is used for that: one named **"Aleatora Test"**, which Jon makes and
  nothing else reads (decision 9). Without that port, the test is skipped.
- **The schedule runs in beats, not seconds.** For Aleatora to follow Reaper, `NotePlayer`'s
  times become beats. A time source turns the `Stopwatch` into beats. It is either internal
  (seconds × BPM ÷ 60, from *Play*) or the follower (clock ticks ÷ 24, plus the time since the
  last tick at the measured tempo). The MIDI-2 tests stay, run through the internal source.

## Decisions for Jon

Each has the default this plan builds if Jon doesn't say otherwise.

**Ruled (2026-11-15):** Jon accepted 2, 3, 4, 5, 8 and 9 as written (Design, rulings 38 to 42)
and gave the go-ahead for IN-1 (ruling 43). 1, 6, 7 and 10 are built as their defaults until
IN-4.

1. **The library.** **Default:** our `winmm` wrapper grows the input calls (`midiInGetNumDevs`,
   `midiInGetDevCapsW`, `midiInOpen` with a callback, `midiInStart`, `midiInStop`,
   `midiInReset`, `midiInClose`, `midiInGetErrorTextW`), behind an `IMidiInput`. This is ruling
   27 carried on. Nothing to install.
2. **How many inputs.** A keyboard and Reaper's clock come in on different ports, so one input
   isn't enough. **Default:** any number of inputs open at once. Each is a toggle on the *MIDI*
   page, saved by name (`Aurumworks.Midi.Inputs`), and opened again at start. Every message
   carries the name of the port it came from. The page doesn't offer the chosen output's own
   loopMIDI port as an input, because Aleatora would hear its own notes. It shows that port
   greyed, with "(the output)".
3. **Who leads the tempo.** **Default:** a *Clock* choice on the *MIDI* page, saved
   (`Aurumworks.Midi.Clock`):
   - *Aleatora*: Aleatora's own BPM, as now.
   - *Follow* one of the open inputs (Reaper's): the tempo and transport come from that port.

   Following Reaper is the usual studio arrangement and is the main case this plan builds for.
   Leading is decision 5.
4. **What following means.** **Default:**
   - Reaper's *Start* plays Aleatora from its first note, its *Stop* ends the note sounding and
     holds the place, and its *Continue* resumes.
   - Aleatora's note *n* plays on Reaper's quarter note *n*, counted from the project's start
     through Song Position Pointer (SPP). The first note lands on bar 1, beat 1, and a note
     always lands on the same beat when played back. If Reaper starts mid-project, Aleatora
     starts at the note for that beat. With *Loop* on, the melody wraps. With *Endless*, the
     generator steps forward to that beat (the same notes as playing from the start). With
     neither, nothing plays past the last note.
   - The *BPM* box shows Reaper's tempo to one decimal and can't be edited.
   - Aleatora's *Play* and *Stop* stay. *Stop* silences Aleatora until Reaper next starts, and
     *Play* lets it join on the next beat.
   - If the ticks stop for half a second while playing, Aleatora treats that as Reaper's *Stop*.
5. **Leading: clock out.** **Default:** a *Send clock* toggle on the *MIDI* page, saved, off by
   default. With it on and *Clock* set to *Aleatora*, Aleatora sends on its output port: 24
   ticks a quarter note, *Start* on *Play* from the first note, *Continue* on a resume, *Stop*
   on *Pause* and *Stop*, and SPP before each start. Ticks are sent only while playing. Reaper
   follows through its external sync (*Transport*, right-click: *Timecode synchronization
   settings*, MIDI clock/SPP; IN-4 confirms the menu path). Reaper is known to follow MIDI
   clock less tightly than it leads, which is one more reason decision 3 favours following.
6. **Reading the tempo.** **Default:** the tempo is the average over the last 24 ticks (one
   beat), so a jitter of a millisecond moves it by well under 1 BPM. Each note on is sent as
   its beat's tick arrives. The note off comes at the note length (0.9) of a beat at the
   measured tempo.
7. **The keyboard's base layer.** **Default:**
   - A `HeldNotes` model per input and channel: the keys down, with velocities.
   - The sustain pedal (CC 64) is kept separately. *Held* means keys down plus notes sustained.
   - A note on at velocity 0 counts as a note off.
   - Every channel is listened to.
   - SysEx is dropped. Active Sensing (0xFE) is dropped before the monitor.
   - Nothing in Aleatora responds to the keyboard yet: the notes are kept and shown, nothing
     more.
8. **Seeing it: a MIDI monitor.** **Default:** a *Monitor* page (a dialog) opened from the
   *MIDI* page. It shows:
   - The last 16 messages, newest first. Each line gives the port, the channel, what the message
     is (`Note on E4 96`, `CC 64 127`, `Clock: Start`) and its time.
   - The held notes as names: `C4 E4 G4`, with sustained notes muted.
   - The clock: *Following Han: 120.0 BPM, playing, beat 17*, or *Aleatora: 120 BPM*.

   Clock ticks themselves aren't listed, because there are 48 a second. Note names use C4 = 60,
   as Reaper does by default.
9. **The loopback test port.** This changes ruling 33, which says a test opens a loopMIDI port
   but sends it no note. **Default:** the one test that sends and receives does so only
   through a loopMIDI port named "Aleatora Test", which Jon makes in loopMIDI and which nothing
   else reads. The test sends it a CC 119 on channel 16 and a clock tick, then checks that both
   come back. It is skipped if that port doesn't exist. Greedo and Han are never sent to.
10. **What changes on the *Melodies* page.** **Default:** nothing new on the page. The *BPM* box
    follows decision 4, and the *Output* button still opens the *MIDI* page, where inputs, clock
    and the monitor live. The page is full (MIDI plan, *Risks*).

## Code layout

- **`Scripts/Midi/`** (plain C#):
  - `MidiMessage` gains readers for system messages: `IsClock`, `IsStart`, `IsContinue`,
    `IsStop`, `IsSongPosition` (and its value in sixteenths), `IsActiveSensing`; writers
    `Clock`, `Start`, `Continue`, `Stop`, `SongPosition(sixteenths)`; and `Describe`, which names
    notes (C4 = 60).
  - `IMidiInput`: `Name`, `TryRead(out TimedMessage)`, `Signal` (a `WaitHandle`), `Dispose()`.
    `TimedMessage` is the message, its time on the `Stopwatch`, and its port.
  - `WinMm` gains the input calls; `WinMmInput` (internal) is an open input with its callback.
  - `MidiPorts` gains `Inputs` and `TryOpenInput(name, out input, out error)`.
  - `FeedInput`: a stand-in `IMidiInput` that a test or the menu check feeds with made-up
    messages at made-up times.
  - `HeldNotes` (decision 7).
  - `ClockFollower`: fed ticks, Start, Stop, Continue and SPP with their times. It gives
    `Tempo`, `IsRunning`, and `BeatAt(time)` (decision 6). It declares a stop after half a
    second without ticks.
  - `ClockSender`: given the BPM and the transport, `Advance(now, output)` sends the ticks due
    by now, plus Start, Stop, Continue and SPP (decision 5).
  - `NotePlayer`: its schedule moves into beats (*Engineering rules*), driven through an
    `IBeatSource` (`Internal` or the follower).
- **`Scripts/Game/`**:
  - `MidiPlayback` gains the open inputs, the clock choice, *Send clock*, the follower, the
    sender and the held notes. The clock thread runs while playing or while an input is open.
    A queue of messages goes to the main thread for the monitor.
- **`Scripts/View/`**:
  - `MidiPage` gains *Inputs* (toggles), *Clock* (a dropdown: *Aleatora*, then each open
    input), *Send clock*, and a *Monitor* button.
  - `MidiMonitorPage` (decision 8) is new.
  - `MelodiesPage`'s *BPM* box shows the followed tempo and is read-only while following.

## IN-1 — Input (branch `in-1-input`, from `main`)

**Tasks**

1. The input calls in `WinMm`, `WinMmInput`, `IMidiInput`, `MidiPorts.Inputs` and
   `TryOpenInput`, and `FeedInput` (decisions 1, 9).
2. `MidiMessage`'s system messages and note names; `HeldNotes` (decision 7).
3. `MidiPlayback`: the saved inputs (decision 2), opened at start (none in a self-test) and
   closed as the rules say. The clock thread runs while an input is open and waits on the
   inputs' events. The held notes, and the queue for the main thread.
4. EditMode tests:
   - Every new message's bytes, and SPP's value.
   - `HeldNotes`: on, off, velocity 0, the sustain pedal holding and releasing, and notes on
     two channels kept apart.
   - `MidiPorts.Inputs` doesn't throw, and a port that isn't there doesn't open and says why.
   - Through `FeedInput` and the real clock thread, a fed note shows as held within 5 ms.
   - Closing leaves the thread ended.
   - With "Aleatora Test" listed, its output and input open, the CC and the tick sent come back
     with times about now, and both close (skipped without the port).
5. The menu check: the bootstrap opened no input. A `FeedInput` with C4, E4 and G4 on shows them
   held.

**Done when** the tests pass and the menu check passes.

## IN-2 — Clock (branch `in-2-clock`)

**Done** (2026-11-15): 265 EditMode tests pass, the MIDI-2 ones unchanged. What was built, and
the readings where this plan was silent, are in Tech, *As built (IN-2)*. One change from *Code
layout*: `ClockSender.Advance` takes the player's beat, not the time.

**Tasks**

1. `NotePlayer` in beats, through `IBeatSource`. Every MIDI-2 test still passes unchanged in
   what it checks.
2. `ClockFollower` (decisions 4, 6) and `ClockSender` (decision 5).
3. `MidiPlayback`: the clock choice and *Send clock* (saved), the follower fed from the chosen
   input, and the player driven by it while following.
4. EditMode tests, with made-up times:
   - The follower reads ticks at 120 BPM as 120.0, and a tempo change within a beat.
   - With ±1 ms jitter, the tempo stays within 0.5 BPM.
   - SPP 16 puts the next note at index 4.
   - Start plays note 0 on the first tick and note *n* on tick 24*n*.
   - Stop ends the note sounding. Continue after SPP resumes at its beat.
   - Half a second without ticks counts as a stop.
   - *Loop* wraps, and *Endless* steps to a beat far ahead with the same notes as playing
     there.
   - The sender sends 24 ticks a beat at 120 BPM (20.83 ms apart), Start, Stop and Continue in
     their places, and SPP.
   - On the real thread with a `FeedInput` clock at 300 BPM, each note on comes within 2 ms of
     its tick (the spread logged).

**Done when** the tests pass.

## IN-3 — The screens (branch `in-3-page`)

**Done** (2026-11-15): the menu check passes at both sizes (185 checks, 17 screenshots); 268
EditMode tests pass. What was built, and the readings where this plan was silent, are in Tech,
*As built (IN-3)*.

**Tasks**

1. `MidiPage`: *Inputs*, *Clock*, *Send clock*, *Monitor* (decisions 2, 3, 5). The output's own
   port is shown greyed.
2. `MidiMonitorPage` (decision 8).
3. `MelodiesPage`: the *BPM* box while following (decisions 4, 10).
4. **The menu check grows**, with a `RecordingOutput` and a `FeedInput`:
   - The *MIDI* page shows the inputs, with screenshot `6-MidiIn`.
   - The fed keyboard's notes appear in the monitor, held, with screenshot `6-Monitor`.
   - *Clock* follows the fed input. A fed clock at 150 BPM, Start: the *BPM* box reads 150.0,
     the first note on comes with the first tick, and the playhead moves, with screenshot
     `6-Following`.
   - A fed Stop leaves nothing sounding.
   - *Clock* set to *Aleatora* with *Send clock* on, then *Play*: Start, then ticks about 16.7 ms
     apart at 150 BPM, then *Stop*.
   - `UiProbe.Reachable` reaches the new controls.
5. Look at the screenshots at both sizes: the page legible, nothing overlapping.

**Done when** the menu check passes with its new checks. **Hard stop.**

## IN-4 — Jon plays with Reaper and a keyboard

Two loopMIDI ports: **Greedo** (Aleatora to Reaper, as now) and **Han** (Reaper to Aleatora),
and a third, **Aleatora Test**, for the tests only.

1. **Following Reaper:**
   1. In Reaper: *Preferences > MIDI Outputs*, enable Han, then right-click it and turn on
      *Send clock/SPP*.
   2. In Aleatora: *MIDI* page, turn on input Han, set *Clock* to Han.
   3. Press Reaper's *Play*. Aleatora plays at Reaper's tempo, note 1 on bar 1. Recorded on a
      track from Greedo, the notes sit on Reaper's grid.
   4. Change Reaper's tempo, start mid-project, stop: Aleatora goes with it.
2. **Leading Reaper:**
   1. In Aleatora: set *Clock* to *Aleatora* and turn on *Send clock*.
   2. In Reaper: enable Greedo as a MIDI input, then turn on external sync to MIDI clock (decision
      5's menu path).
   3. Press Aleatora's *Play*: Reaper starts and runs at Aleatora's BPM.
3. **The keyboard:**
   1. Plug it in and turn on its port on the *MIDI* page.
   2. Open the *Monitor*: chords played show as held notes, and the sustain pedal holds them.
   3. Also try it with Reaper holding the same port. With Windows MIDI Services the port is
      shared; without it, the second program to open it gets Windows' error (*Risks*).

Jon rules on the decisions he hasn't yet, and the three branches merge to `main`.

## Milestone summary

| # | Branch | Lands | Checked by |
|---|---|---|---|
| IN-1 | `in-1-input` | Input ports, system messages, held notes, the thread reading input | EditMode tests (one through "Aleatora Test"), menu check |
| IN-2 | `in-2-clock` | `NotePlayer` in beats, following Reaper's clock, sending ours | EditMode tests |
| IN-3 | `in-3-page` | Inputs, clock and *Send clock* on the *MIDI* page; the monitor | Menu check with screenshots |
| IN-4 | merge | Jon's Reaper and keyboard test, rulings | **Jon** |

## Later (each its own plan)

- **What the keyboard does:** chords into the generator, the grid, or what Jon has in mind.
  It builds on `HeldNotes` and the message queue.
- MIDI thru; SysEx; MIDI Time Code; a channel filter per input.
- The Windows MIDI Services API (MIDI 2.0, ports that are multi-client by design), if winmm
  falls short.
- From the MIDI plan: a richer channel choice, rhythm beyond quarter notes, MIDI files, OSC.

## Risks

- **Single-client keyboard ports.** Under the old Windows MIDI stack, a USB keyboard's input
  opens in one program at a time, so Reaper and Aleatora can't both hold it. Windows MIDI
  Services, which is on Jon's machine (Tech, *As built (MIDI-1)*), shares ports. If it doesn't
  for his keyboard, Reaper can pass the keyboard on to Aleatora through a loopMIDI port.
- **Feedback loops.** If a loopMIDI port is both Aleatora's output and one of its inputs, or if
  Reaper echoes Aleatora's notes back on Han, Aleatora hears itself. Decision 2 guards the first
  case. For the second, IN-4's Reaper set-up leaves Han's output to clock, with no track sending
  to it.
- **The callback's thread.** Managed code on a Windows multimedia thread must not block. The
  callback takes one lock, briefly, and nothing else (*Engineering rules*). A stuck callback
  would hang `midiInClose`.
- **The `NotePlayer` change.** Moving its schedule from seconds to beats touches the heart of
  MIDI-2. The MIDI-2 tests guard it, and IN-2 changes nothing they check.
- **Reaper following MIDI clock** may be loose or need settings. Following Reaper is the main
  case (decision 3); leading is checked in IN-4 and may be left to *Later* if Reaper won't follow
  well.
