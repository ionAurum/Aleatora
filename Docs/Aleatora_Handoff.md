# Aleatora: hand-off

Status (2026-11-15): **TH-1 is finished and merged to `main`** (Design, rulings 51 and 52). The
four theory tables (`virtuoso_lydian`, `_phrygian`, `_double_harmonic`, `_hindu_pentatonic`) ship
with the 17 folk sets, 21 in all. 291 EditMode tests and the menu check's 186 checks all pass
(Tech, *Checks*, *Runs*). On `main` too: the Markov plan, MIDI out, MIDI in with clock and sync
(all finished).

Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Next: TH-2, Jon plays them

Jon picks each `Virtuoso · …` set on the *Melodies* page and plays it into Reaper at different
orders and temperatures, then says what to change in `Tools/Markov/Theory/virtuoso.json`. For
each change, on a branch (`th-2-tuning`) from `main`: re-run
`python -I Tools/Markov/Theory/virtuoso.py`, then
`python -I Tools/markov.py Tools/Markov/Source Tools/Markov/Theory/Output Assets/Aurumworks/Resources/Markov`,
then the EditMode tests (`TheoryTableTests` checks the pulls and counts). Record his rulings in
Design.

Read for it: Tech, *Melody tables*, *As built (TH-1)*; the Theory plan, *TH-2* and *Risks*;
the docstring of `Tools/Markov/Theory/virtuoso.py`.

Open for Jon: the Lydian pull is the weakest (after ♯4: 5 39%, 3 33%). To make it stronger,
raise `pull`, or have pulls apply inside figures too. The second changes the rules, so it is his
call.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
