# Aleatora: hand-off

Status (2026-11-15): **M1 built on branch `m1-tables`, waiting for Jon's go-ahead to merge.** M0
is on `main`. M1: the 17 tables are in `Resources/Markov`; `Music/Degree`, `ModeScale`,
`MarkovTable`, `MarkovTableReader` and `Game/MarkovLibrary` read them. 69 EditMode tests pass and
the menu check passes (51 checks, tree clean). What was built is in `Docs/Aleatora_Tech.md`
(*Melody tables*, "As built"). The plan is `Docs/Aleatora_Markov_Plan.md`; Jon's rulings are in
`Docs/Aleatora_Design.md`.

Open for Jon, none blocking:
- From M1's report: nothing to rule on. Beyond the plan, for him to know: an empty context is
  order 0 (the unigram); the menu check now reads the tables; a set whose `set` line is not its
  file's name is refused.
- From M0 (`Docs/Aleatora_Design.md`, *Open, from M0's report*): the window size; the cell
  colliders part when `Gap` is above 0 (ruling 14).

## Next: merge M1, then M2, the generator (branch `m2-generator`, from `main`)

On Jon's go-ahead: merge `m1-tables` into `main` (`--no-ff`, as M0 was), then branch.

Read: `Docs/Aleatora_Markov_Plan.md` (*Decisions for Jon* 6, *M2*); `Docs/Aleatora_Tech.md`
(*Melody tables*, *Checks*); `Assets/Aurumworks/Scripts/Music/MarkovTable.cs`;
`Assets/Aurumworks/Scripts/Game/AleatoraTuning.cs` and `GridTuning.cs` (for `MelodyTuning`).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) now lists `Aleatora/`; that folder is not a repository.
