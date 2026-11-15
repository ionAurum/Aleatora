# Aleatora: hand-off

Status (2026-11-15): **MIDI-1 (the port) is built on branch `midi-1-port`, waiting for Jon's
go-ahead on MIDI-2.** Jon ruled on the MIDI plan (Design, rulings 27 to 33): our own `winmm`
library, GS Wavetable hidden, the channel a dropdown for now, Loop plus an *Endless* mode, Run In
Background on (done), a *Panic!* button, virtual or mock ports in tests. Decisions 2, 5, 6, 8, 9
and 11 are built as their defaults. 166 EditMode tests pass (one opens Jon's loopMIDI port
"Greedo" and sends it only an All Notes Off); the menu check passes (99 checks).

Open for Jon:
- Decision 12, Panic as built: note offs on **all 16 channels** (2,048), and it **doesn't stop
  playback**. Both are readings of "send all 128 note offs"; say if either is wrong.
- *Endless* as planned (decision 7): a toggle beside *Loop*, at most one on; past the last note
  the generator steps and the strip grows. Built in MIDI-2 and MIDI-3.

## Next: MIDI-2, the player (branch `midi-2-player`, from `midi-1-port`)

On Jon's go-ahead.

Read: `Docs/Aleatora_Midi_Plan.md`: *Engineering rules*, *Decisions for Jon* 6, 7, 8, 12,
*Code layout* (`NotePlayer`, `MidiPlayback`), *MIDI-2*, *Risks* (timing).
`Docs/Aleatora_Tech.md`: *MIDI*. Code: `Scripts/Midi/` (all small), `Game/MidiPlayback.cs`,
`Music/MelodyGenerator.cs` (the public API, for *Endless*'s next note), and in
`View/MelodiesPage.cs` how *Step* steps the generator and voices the note.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
