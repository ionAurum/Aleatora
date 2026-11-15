# Aleatora: hand-off

Status (2026-11-15): **M0 built, at its hard stop, waiting for Jon.** Aleatora is in git, brought
up to the framework without sound (on `main`), and its grid is rebuilt on k × m × n with a tuning
asset, 21 EditMode tests and a grown menu check (on `m0-grid`, not merged). The plan is
`Docs/Aleatora_Markov_Plan.md`; Jon's rulings are in `Docs/Aleatora_Design.md`; what was built is
in `Docs/Aleatora_Tech.md` ("As built").

## Waiting on Jon (M0's hard stop)

Open Aleatora in the Editor on `m0-grid` and:

1. Check the Console is free of errors.
2. Press Play, then Play on the menu. Click cells to select; Tab focuses the camera on the
   selected cell, or the board's middle with none; right-drag orbits, the wheel zooms; Escape
   opens the session menu, and under it clicks and the camera do nothing.
3. On `Assets/Aurumworks/Resources/AleatoraTuning.asset`, in play: change K, M, N (the board
   rebuilds), Orientation (Flat, Pointy), Size, Gap, Height, the two colours, the cell prefab.
4. Tune the look. Values set in play on the asset stay.

`m0-grid` merges to `main` when Jon is happy with how it works.

Open questions for Jon are in `Docs/Aleatora_Design.md` (*Open, from M0's report*): the
AudioListener on the Main Camera, the default window size, the *Hexagons* layer, the unused
prefab and collider mesh.

## Next: M1, the tables in (branch `m1-tables`, from `main` after `m0-grid` merges)

Read: `Docs/Aleatora_Markov_Plan.md` (*The data*, *The tables on disk*, *M1*);
`Tools/markov.py`'s docstring; `Docs/Aleatora_Tech.md` (*Code layout*, *Checks*). All 17 sets
ship (ruling 6).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) now lists `Aleatora/`; that folder is not a repository.
