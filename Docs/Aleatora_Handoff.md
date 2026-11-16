# Aleatora: hand-off

Status (2026-11-16): **EM-1, the board's notes, is built on `em-1-board`** (the emitter plan,
`Docs/Aleatora_Emitter_Plan.md`; Design, rulings 59 to 72, every decision ruled). EditMode tests
310 of 310; the menu check `em1` exit 0, 224 checks held, 20 screenshots (Tech, *Board notes*,
*Checks*). Waiting for Jon to look at it in the Editor before EM-2.

Finished on `main`: the Markov plan, MIDI out, MIDI in with clock and sync, the theory plan.
Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).
Reaper sends MIDI clock only while its transport runs (ruling 70).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Open from EM-1, for Jon

- The labels in the theme's text colour (decision 10's default) are near-white and can't be read
  on the pale cells; *Label Color From Theme* off uses *Label Color*. Jon's call (the look is his).
- Clustered is carved one region at a time, not grown all at once as the plan first said (the
  plan and Tech, *Board notes*, say why).

## Next: EM-2, the emitter (branch `em-2-emitter`, from `em-1-board`)

`Game/Emitter` and `Game/EmitterSphere` (the pool, the draws from `Rng`, spraying, hits that
glow), the emitter's parameters on `EmitterTuning`, the first controls (decision 8) in the input
actions, the tests, `CheckEmitter`. The bootstrap gives the scene's `Emitter` object (the cone,
at (0, 10, 0); its child is the cone model) the component. The fixed step is 5 ms (ruling 69).

Read for it: the emitter plan, *How it works* (*The emitter*, *A hit*, *The first controls*),
*Decisions* 5, 8, 10 and 11, *EM-2*, *Risks*; Tech, *Board notes* and *Grid* (*As built (M0)*:
`HexGrid`, `HexCellView`, `GridSelection`); `Game/BoardNotes.cs`, `Grid/HexCellView.cs`.

Later, each its own plan (Design, *Open*): how the generator drives the grid, OSC out (ruling
39), the keyboard's notes (ruling 37), rhythm for the *Melodies* page.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
