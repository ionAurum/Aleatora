# Aleatora: hand-off

Status (2026-11-16): **EM-4, the panel, is built on `em-4-panel`** (from `em-3-notes`; the
emitter plan, `Docs/Aleatora_Emitter_Plan.md`). Jon tried EM-3 into Reaper: it works (ruling 75). EditMode
tests 363 of 363; the menu check `em4` exit 0, 293 checks held, 24 screenshots (Tech, *Emitter*,
*As built (EM-4)*, *Checks*). Committed; waiting for Jon to try the panel in the Editor before
EM-5.

Finished on `main`: the Markov plan, MIDI out, MIDI in with clock and sync, the theory plan.
`em-1-board` to `em-4-panel` aren't merged; the plan merges at EM-7.
Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).
Reaper sends MIDI clock only while its transport runs (ruling 70).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Open, for Jon

- **From EM-4** (Tech, *As built (EM-4)*), C2's readings where the plan is silent:
  - **The panel writes `AleatoraTuning` itself**, so in the Editor what it changes stays on the
    asset after play (as an Inspector change in play does). The alternative is a copy for the run,
    which would cut the Inspector off from the board in play. Say if you want it otherwise.
  - The panel is hidden while any menu is open; H hides and shows it otherwise.
  - With the pointer over the panel the camera neither orbits nor zooms (a drag begun on the board
    carries on over it).
  - On the panel beyond the plan's short list: *Speed jitter*, *Max spheres*, *Quantize length* and
    decision 11's speeds, velocities, curve, lengths and *Max voices*.
- **From EM-2, waiting for EM-5** (ruling 74): moving the emitter carries the aim point; no raise
  or lower on the gamepad; *Spray* is two actions (Space toggles, the right trigger's pressure).
  The starting looks on `AleatoraTuning` (Emitter: look) are Jon's to change.
- **From EM-3** (Tech, *As built (EM-3)*): the grid sixteenth and strength 1 are C2's starting
  values. A hit is placed on the beat line when Unity reports it, so at strength 0 the hits of one
  frame go out together. When the line changes the notes waiting go to its next grid point. Merged
  notes take the louder velocity and the longer length.

## Next: EM-5, designing the controls (once Jon has tried EM-4)

C2 writes `Docs/Aleatora_Emitter_Controls.md`: whole control schemes for mouse and keyboard,
gamepad and a MIDI controller, each with its cost; small prototypes on a branch where reading
isn't enough; Jon picks and rules, and the choice becomes EM-6 in the plan. Jon is thinking about
the user interaction himself (Design, ruling 74): ask what he has in mind before writing.

Read for it: the emitter plan, *How it works* (*The first controls*), decisions 8 and 9, *EM-5*;
Tech, *Emitter* (the three As builts: the controls, the beat line, the panel); Design, rulings 37,
38, 68 and 74; `Game/Emitter.cs` (`ReadControls`), `View/EmitterPanel.cs`.

Later, each its own plan (Design, *Open*): how the generator drives the grid, OSC out (ruling
39), the keyboard's notes (ruling 37), rhythm for the *Melodies* page.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
