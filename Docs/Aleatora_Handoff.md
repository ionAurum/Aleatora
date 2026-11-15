# Aleatora: hand-off

Status (2026-11-15): **M0 done and merged to `main`.** Jon played it: the grid and the mouse work
as expected. His feedback is applied (Design, rulings 9 to 16); the 21 EditMode tests and the menu
check passed after it. The plan is `Docs/Aleatora_Markov_Plan.md`; Jon's rulings are in
`Docs/Aleatora_Design.md`; what was built is in `Docs/Aleatora_Tech.md` ("As built").

Open for Jon, none blocking (`Docs/Aleatora_Design.md`, *Open, from M0's report*): the window
size; the cell colliders part when `Gap` is above 0, to be put right when the emitter's bodies get
a plan (ruling 14).

## Next: M1, the tables in (branch `m1-tables`, from `main`)

Read: `Docs/Aleatora_Markov_Plan.md` (*The data*, *The tables on disk*, *M1*);
`Tools/markov.py`'s docstring; `Docs/Aleatora_Tech.md` (*Code layout*, *Checks*). All 17 sets
ship (ruling 6).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) now lists `Aleatora/`; that folder is not a repository.
