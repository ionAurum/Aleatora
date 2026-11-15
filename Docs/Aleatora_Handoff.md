# Aleatora: hand-off

Status (2026-11-15): **M3 accepted by Jon (Design, ruling 19); M4 under way on branch
`m3-view`, not yet merged.** M0, M1, M2 and the gel pads are on `main` (M2 merged `--no-ff`).
M3 built the *Melodies* page (`Docs/Aleatora_Tech.md`, *Melodies page*). Ruling 20: the voicing
may leap up to and including an octave; the code already did (at most 11), so only
`VoicingTests.LeapsAreAnOctaveAtMost` and the docs changed. 131 EditMode tests pass. The menu
check last passed before the CanvasRenderer fix (a player was never affected) and has not been
run since; nothing it checks has changed.

Open for Jon:
- Whether `m3-view` merges to `main` now (the plan's M4 merges it after his rulings and tuning).
- From M2 (Design, *Open, from M2's report*): the seed's default (1).
- Dithering on the Main Camera, which the gel glow wants (Tech, *Cell look*).
- From M0 (Design, *Open, from M0's report*): the window size; the cell colliders part when `Gap`
  is above 0 (ruling 14).

## Next: M4, then decision 7's plans

Jon tries the sets, orders and temperatures on the *Melodies* page; tuning he likes goes on
`AleatoraTuning.asset`, rulings into `Docs/Aleatora_Design.md` (*Decided by Jon*, dated). On his
go-ahead, `m3-view` merges to `main` (`--no-ff`). Then the plans for decision 7 (Design, *Open,
each its own plan later*): what the grid means musically, how the generator drives it, MIDI and
OSC out. Each needs Jon's direction before planning.

Read: `Docs/Aleatora_Markov_Plan.md` (*M4*); `Docs/Aleatora_Design.md` (*Open* sections).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) now lists `Aleatora/`; that folder is not a repository.
