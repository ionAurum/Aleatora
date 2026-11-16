# Aleatora: hand-off

Status (2026-11-15): **the MIDI in plan is finished and merged to `main`** (IN-1 to IN-4;
Design, rulings 37 to 47). Jon ran IN-4: following Reaper, leading it with *Send clock*, and a
keyboard into the *Monitor* all worked as expected; he accepted decisions 1, 6, 7 and 10 and the
IN-2 and IN-3 readings as built. On `main`: the Markov plan, MIDI out, MIDI in with clock and
sync. 268 EditMode tests; the menu check at 185 checks at both sizes.

Jon's loopMIDI ports are now "Aleatora to Reaper" (was Greedo), "Reaper to Aleatora" (was Han)
and "Aleatora Test" (ruling 45). "Greedo" and "Han" in the docs, comments and tests are examples
only.

Still open from IN-4:
- **The shared keyboard port** (MIDI in plan, *Risks*, and IN-4 step 3.3): not run. The keyboard
  open in Reaper and Aleatora at once. Worth a try when convenient; if Windows refuses the
  second program, Reaper can pass the keyboard on through a loopMIDI port.

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Next: Jon picks the next plan

Nothing is planned. The candidates (Design, *Open, each its own plan later*): OSC (Jon means to
teach Aleatora Reaper's OSC, ruling 39), what the keyboard's notes do, what the grid means
musically, how the generator drives the grid. When Jon picks one, write its plan as
`Docs/Aleatora_<Name>_Plan.md`, in the shape of `Docs/Aleatora_MidiIn_Plan.md` (*Read first*,
*Decisions for Jon* with defaults, milestones, *Risks*), and stop for his rulings.

Read for it:
- `Docs/Aleatora_Design.md`: *Open, each its own plan later*, and rulings 37 to 47.
- `Docs/Aleatora_MidiIn_Plan.md`: *Later*, as a model of a plan.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
