# Aleatora: hand-off

Status (2026-11-15): **M0 built and played by Jon: the grid and the mouse work as expected.**
`m0-grid` is not merged; it waits for Jon's word. Aleatora is in git, brought up to the framework
without sound (on `main`), and its grid is rebuilt on k × m × n with a tuning asset, 21 EditMode
tests and a grown menu check (on `m0-grid`). The plan is
`Docs/Aleatora_Markov_Plan.md`; Jon's rulings are in `Docs/Aleatora_Design.md`; what was built is
in `Docs/Aleatora_Tech.md` ("As built").

## Waiting on Jon

Jon's feedback on M0 is in (Design, rulings 9 to 14) and applied on `m0-grid`: the asset folders
are under `Assets/Aurumworks/`, the AudioListener is off the Main Camera, `Hexagon.prefab` is
deleted. Not yet run since: the EditMode tests and the menu check (the Editor was open). Run both
before merging.

1. Say whether `m0-grid` merges to `main`.
2. `Docs/Aleatora_Design.md` (*Open, from M0's report*): the window size, the unused *Hexagons*
   layer, the collider when `Gap` is above 0, Disable Unity Audio.

## Next: M1, the tables in (branch `m1-tables`, from `main` after `m0-grid` merges)

Read: `Docs/Aleatora_Markov_Plan.md` (*The data*, *The tables on disk*, *M1*);
`Tools/markov.py`'s docstring; `Docs/Aleatora_Tech.md` (*Code layout*, *Checks*). All 17 sets
ship (ruling 6).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) now lists `Aleatora/`; that folder is not a repository.
