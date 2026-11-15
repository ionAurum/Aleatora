# Aleatora: hand-off

Status (2026-11-15): **M2 built on branch `m2-generator`, waiting for Jon's go-ahead to merge.**
M0, M1 and the gel pads are on `main` (M1 merged `--no-ff` with the gel pads and Jon's Bloom 15
and `Emitter` off). M2: `Music/Rng`, `GeneratorSettings`, `MelodyGenerator`, `MelodyStep`,
`Voicing`, and `Game/MelodyTuning` on the tuning asset (`Melodies`). 107 EditMode tests pass; the
menu check passes (55 checks, tree clean). What was built: `Docs/Aleatora_Tech.md` (*Melody
generator*, "As built"). The plan is `Docs/Aleatora_Markov_Plan.md`; Jon's rulings are in
`Docs/Aleatora_Design.md`.

Open for Jon:
- From M2 (`Docs/Aleatora_Design.md`, *Open, from M2's report*): the voicing's leaps at the
  range's edges (the plan's test couldn't hold; changed); the seed's default (1).
- Beyond the plan, for him to know: `MelodyStep.IsFirst`; `MelodyGenerator.SetHistory`; the
  generator's settings are their own Inspector fields on `MelodyTuning`; the allocation test uses
  Unity's recorder, since Mono's counter counts nothing.
- Dithering on the Main Camera, which the gel glow wants (Tech, *Cell look*).
- From M0 (`Docs/Aleatora_Design.md`, *Open, from M0's report*): the window size; the cell
  colliders part when `Gap` is above 0 (ruling 14).

## Next: merge M2, then M3, the view (branch `m3-view`, from `main`)

On Jon's go-ahead: merge `m2-generator` into `main` (`--no-ff`), then branch. M3 ends in a hard
stop: Jon looks at the *Melodies* page.

Read: `Docs/Aleatora_Markov_Plan.md` (*Decisions for Jon* 4, *M3*); `Docs/Aleatora_Tech.md`
(*Melody generator*, *Checks*); `Assets/Aurumworks/Scripts/Music/MelodyGenerator.cs`,
`MelodyStep.cs`; `Game/MelodyTuning.cs`; the menus (`MainMenu.cs`, `SessionMenu.cs`) and
`../Framework/README.md` for the UI kit.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) now lists `Aleatora/`; that folder is not a repository.
