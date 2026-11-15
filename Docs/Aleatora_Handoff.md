# Aleatora: hand-off

Status (2026-11-15): **the MIDI plan is written (`Docs/Aleatora_Midi_Plan.md`), waiting for
Jon's go-ahead and his answers to its *Decisions for Jon*.** Jon's brief is Design ruling 26: MIDI
out first, one direction, the *Melodies* melody as quarter notes at a BPM on a channel, into
Reaper through loopMIDI. The Markov plan is finished and on `main` (M0 to M4; rulings to 25).
131 EditMode tests pass; the menu check passes.

Open for Jon:
- The MIDI plan's eleven decisions, each with a default. The ones most worth a look: 1 (our own
  `winmm` wrapper rather than DryWetMIDI or RtMidi), 3 (hide Windows' GS Wavetable Synth), 4
  (the channel as a dropdown, though he said "button"), 7 (loop repeats the melody) and 10 (Run In
  Background on).
- Confirm the default window, 1920 × 1080 (Design, ruling 23; he wrote "1920x180").

## Next: MIDI-1, the port (branch `midi-1-port`, from `main`)

On Jon's go-ahead, with his rulings written into `Docs/Aleatora_Design.md` and the plan.

Read: `Docs/Aleatora_Midi_Plan.md` (all of it: it is short); `Docs/Aleatora_Markov_Plan.md`
(*How we work*, *Engineering rules*); `Assets/Aurumworks/Scripts/Game/AleatoraTuning.cs`,
`Game/Aleatora.cs`, `Editor/TuningMaker.cs`; `../Framework/README.md` (*Settings*).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) now lists `Aleatora/`; that folder is not a repository.
