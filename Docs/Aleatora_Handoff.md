# Aleatora: hand-off

Status (2026-11-16): **The emitter plan is written and revised; it waits for Jon's go-ahead**
(`Docs/Aleatora_Emitter_Plan.md`; Design, rulings 59 to 71). Not committed yet: the plan, rulings
59 to 71 in Design, this doc, the plan's line in `CLAUDE.md`, and Jon's own changes: the
`Emitter` re-enabled in `SampleScene`, the fixed timestep 0.005 s (`TimeManager.asset`), and
`DynamicsManager.asset` re-saved by Unity in its newer format. Finished on `main`: the Markov
plan, MIDI out, MIDI in with clock and sync, the theory plan (22 sets; EditMode tests 297 of 297;
the menu check `th2pd`, 186 checks held).

Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).
Jon re-enabled the `Emitter` cone in `SampleScene` (ruling 59).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Next: Jon rules on the emitter plan's decisions, then EM-1

Decided: 2, 6, 7, the axes layout, the cells' colours (scaled to the most likely). Still open,
each with its default: 1, 3, 4, 5, 8, 9, 10 (all but the colours) and 11. Jon says which to
change and gives the go-ahead. EM-5 (designing the real
controls) comes after the panel.
Then EM-1, the board's notes (branch `em-1-board` from `main`): `Music/CellNotes`,
`Game/EmitterTuning`, `Game/BoardNotes`, the tests, `CheckBoard`.

Read for it: the emitter plan, whole (*How it works*, *Decisions*, *Code layout*, *EM-1*); Tech,
*Grid* (*As built (M0)*) and *Melody tables* (`Unigram`, `ModeScale`); Design, rulings 59 to 71.

Still open, each its own plan later (Design, *Open*): how the generator drives the grid, OSC out
(ruling 39), the keyboard's notes (ruling 37), rhythm for the *Melodies* page.

Left open, not pressing: the Lydian pull is the weakest (after ♯4: 5 43%, 3 31%); raise `pull`
in `Tools/Markov/Theory/virtuoso.json`, or have pulls apply inside figures too (his call).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
