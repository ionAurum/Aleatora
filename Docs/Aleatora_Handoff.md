# Aleatora: hand-off

Status (2026-11-15): **M3 built on branch `m3-view`; hard stop, Jon looks at the *Melodies*
page.** M0, M1, M2 and the gel pads are on `main` (M2 merged `--no-ff`). Ruling 18 (Design): a
transition graph beside the grid, committed to `main` before branching. M3: `View/MelodiesPage`,
`DegreeBars`, `TransitionGrid`, `TransitionGraph`, `MelodyStrip`, `ViewText`, `ViewMesh`;
`Game/MelodyViewTuning` on the tuning asset (`Melody View`). 128 EditMode tests pass; the menu
check passes at 1920 × 1080 and 1280 × 720 (94 checks, 12 screenshots, tree clean). What was
built: `Docs/Aleatora_Tech.md` (*Melodies page*). The plan is `Docs/Aleatora_Markov_Plan.md`.

Open for Jon:
- From M3 (`Docs/Aleatora_Design.md`, *Open, from M3's report*): the layout (graph full height
  at the right), arcs bowing inward, bars scaled to the tallest, a melody made on opening, the
  opaque backdrop, no ♭/♯ in the default font.
- From M2 (Design, *Open, from M2's report*): the voicing's leaps at the range's edges; the
  seed's default (1).
- Dithering on the Main Camera, which the gel glow wants (Tech, *Cell look*).
- From M0 (Design, *Open, from M0's report*): the window size; the cell colliders part when `Gap`
  is above 0 (ruling 14).

## Next: Jon's rulings on the page, then M4

Jon opens *Melodies* from the main menu (or Escape in play, then *Melodies*) and tries it. The
graph's look is on `AleatoraTuning.asset` > *Melody View*, live. His rulings go into
`Docs/Aleatora_Design.md` (*Decided by Jon*, dated); changes to the page are made on `m3-view`,
then the menu check is run again (`bash Tools/menucheck.sh <name>`, with `WIDTH=1280
HEIGHT=720` for the small size) and the screenshots looked at. On his go-ahead, M4: `m3-view`
merges to `main` (`--no-ff`).

Read: `Docs/Aleatora_Design.md` (*Open, from M3's report*); `Docs/Aleatora_Tech.md` (*Melodies
page*); `Docs/Aleatora_Markov_Plan.md` (*M3*, *M4*); the file under `Assets/Aurumworks/Scripts/View/`
the ruling touches.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) now lists `Aleatora/`; that folder is not a repository.
