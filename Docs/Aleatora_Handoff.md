# Aleatora: hand-off

Status (2026-11-16): **EM-5, designing the controls, is written**: `Docs/Aleatora_Emitter_Controls.md`
(the emitter plan, `Docs/Aleatora_Emitter_Plan.md`, *EM-5*). Jon tried EM-4: it works, its
readings stand (ruling 76). Jon gave the mouse scheme himself (ruling 77); the doc works it out
(scheme A), proposes the gamepad (B) and MIDI controllers (M), each decision with a default, each
scheme with its cost. Committed on `em-4-panel` (docs only). **Waiting for Jon's rulings on it.**

Finished on `main`: the Markov plan, MIDI out, MIDI in with clock and sync, the theory plan.
`em-1-board` to `em-4-panel` aren't merged; the plan merges at EM-7.
Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).
Reaper sends MIDI clock only while its transport runs (ruling 70).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Open, for Jon

- **The controls doc**, *For Jon to rule*: the defaults A1–A11, B1–B3, M1–M5, and which schemes
  EM-6 builds (C2's suggestion: A, B with it if wanted, M as its own milestone after). The ones
  most likely to matter: A1 (G and a click on the cone take the emitter; Escape releases), A4 (the
  target is where the spray lands, gravity included, not the straight cone), A5 (*Spread* becomes
  the whole angle), A7 (no camera orbit or zoom while the emitter is held), A8 (Space stays as a
  latch), A9 (Shift glides along the grid's six directions and stops on a cell's middle), M5 (which
  MIDI controllers Jon has).
- **From EM-3** (Tech, *As built (EM-3)*): the grid sixteenth and strength 1 are C2's starting
  values; readings for EM-7.

## Next: EM-6, the controls (once Jon has ruled on the doc)

Write EM-6 into the plan from Jon's rulings (tasks, checks), branch `em-6-controls` from
`em-4-panel`, build it. Read for it: `Docs/Aleatora_Emitter_Controls.md` (whole, with Jon's
rulings), Design rulings 76 and 77; Tech, *Emitter* (*As built (EM-2)*: the controls, the aim
marker; *As built (EM-4)*: the panel, `UiInput`, `GridSelection`); `Game/Emitter.cs`
(`ReadControls`, `Orient`, `PlaceMarker`), `Grid/GridSelection.cs`, `Game/SprayDraws.cs`
(*Spread* as the half-angle today), `Game/EmitterTuning.cs`, `Assets/InputSystem_Actions.inputactions`
(`Player`), `Scripts/MenuCheck.cs` (the emitter's and the panel's checks).

Later, each its own plan (Design, *Open*): how the generator drives the grid, OSC out (ruling
39), the keyboard's notes (ruling 37), rhythm for the *Melodies* page.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
