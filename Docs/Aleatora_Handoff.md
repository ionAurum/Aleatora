# Aleatora: hand-off

Status (2026-11-15): **MIDI-2 (the player) is built on branch `midi-2-player` (from
`midi-1-port`), waiting for Jon's go-ahead on MIDI-3.** `NotePlayer` (the schedule, *Endless*
included) and the clock thread in `MidiPlayback`. 193 EditMode tests pass; through the thread, 16
notes at 300 BPM land mean 0.57 ms late, worst 1.18 ms. The menu check still passes (99 checks;
it grows in MIDI-3).

Open for Jon (readings where the plan was silent or loose; Tech, *MIDI*, *As built (MIDI-2)*):
- Decision 12, Panic as built: note offs on **all 16 channels** (2,048), and it **doesn't stop
  playback**.
- *Pause* then *Play* resumes with the **next note at once**, not after the rest of the beat.
- A *BPM* change while playing: the next note comes **a beat at the new tempo after the last
  note went on** (at once if that's past).
- Choosing another port while playing **plays on**, on the new port.

## Next: MIDI-3, the screens (branch `midi-3-page`, from `midi-2-player`)

On Jon's go-ahead. **Hard stop** after it: Jon plays into Reaper (MIDI-4).

Read: `Docs/Aleatora_Midi_Plan.md`: *Decisions for Jon* 2, 4, 5, 7, 8, 9, 11, 12, *Code layout*
(`View/`), *MIDI-3*, *Risks* (the page grows). `Docs/Aleatora_Tech.md`: *Melodies page*, *MIDI*
(both *As built*), *Checks* (the menu check). `../Framework/README.md`: *Menu shell*
(`OpenDialog`, `SettingsPage.More`), *UI kit*. Code: `View/MelodiesPage.cs`,
`View/MelodyStrip.cs`, `Game/MidiPlayback.cs`, `Midi/NotePlayer.cs`, `Midi/INoteSource.cs`,
`MenuCheck.cs` (`CheckMelodies`, `CheckMidi`).

The one design point carried over: *Endless*'s source runs on the clock thread. The page's
source needs its own lock around the generator and voicing, a queue the main thread drains to
grow the strip, the bars and the lists, and must not call `MidiPlayback` while holding its lock
(Tech, *MIDI*, `INoteSource`).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
