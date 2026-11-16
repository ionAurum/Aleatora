# Aleatora: hand-off

Status (2026-11-16): **EM-2, the emitter, is built on `em-2-emitter`** (from `em-1-board`; the
emitter plan, `Docs/Aleatora_Emitter_Plan.md`). Jon approved EM-1's look with near-black labels
(Design, ruling 73). EditMode tests 319 of 319; the menu check `em2` exit 0, 244 checks held, 21
screenshots (Tech, *Emitter*, *Checks*). Waiting for Jon to try it in the Editor before EM-3.

Finished on `main`: the Markov plan, MIDI out, MIDI in with clock and sync, the theory plan.
`em-1-board` and `em-2-emitter` aren't merged; the plan merges at EM-7.
Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).
Reaper sends MIDI clock only while its transport runs (ruling 70).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Open from EM-2, for Jon

- **Moving the emitter carries the aim point with it** (C2's reading of decision 8, which
  doesn't say), so W A S D sweep the spray; Shift + pointer and the right stick place the aim.
  The gamepad has no raise or lower (decision 8 gives none).
- **Starting looks, Jon's to change** on `AleatoraTuning` (Emitter: look): the spheres and the
  aim marker in `Plastic.mat` (the cone's), the marker yellow, the hit glow white.
- **The plan's *Spray* is two actions**: *Spray* (Space, toggles) and *Spray Pressure* (the
  right trigger).
- Readings (Tech, *Emitter*): a sphere on the seam between two cells hits both (two notes);
  hits come thick (164 a second on 5 × 5 × 5 at 50 spheres a second). The spheres cost about
  0.5 ms a frame at 300.

## Next: EM-3, notes out (branch `em-3-notes`, from `em-2-emitter`)

`Music/Quantize` and `Midi/HitScheduler`; velocity and length from the hit; `MidiPlayback` runs
the scheduler on the clock thread with the beat line (own or followed, ruling 70), *Panic!*
clears it; the tests; `CheckEmitter` goes on with a `RecordingOutput`. Hits arrive as
`Emitter.Hit` (`EmitterHit`: cell, note, speed, size, time).

Read for it: the emitter plan, *How it works* (*A hit*, *The clock*), *Decisions* 6, 7 and 11,
*EM-3*, *Risks* (*The time a hit lands*, *Two beat lines*); Tech, *MIDI*, *As built (MIDI-2)* and
*As built (IN-2)* (the clock thread, `NotePlayer`'s lock order, `IBeatSource`,
`InternalBeatSource`, `ClockFollower`), and *Emitter*; `Game/MidiPlayback.cs`,
`Game/Emitter.cs`.

Later, each its own plan (Design, *Open*): how the generator drives the grid, OSC out (ruling
39), the keyboard's notes (ruling 37), rhythm for the *Melodies* page.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
