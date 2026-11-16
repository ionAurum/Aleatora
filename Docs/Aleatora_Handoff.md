# Aleatora: hand-off

Status (2026-11-16): **TH-2 is under way on `th-2-tuning`** (from `main`, not merged). Jon's
first round of listening is in (Design, rulings 53 and 54): every Virtuoso phrase now starts on 1
(in `virtuoso.py`; the folk sets unchanged), and the double harmonic set shows as "Virtuoso ·
Byzantine". The four Virtuoso tables are written again; EditMode tests 291 of 291; the menu
check not run for it (Tech, *As built (TH-2)*). On `main`: TH-1, the Markov plan, MIDI out, MIDI
in with clock and sync (all finished).

Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Next: TH-2, Jon plays them again

Jon plays the Virtuoso sets again, with the start on 1, into Reaper at different orders and
temperatures, and says what else to change, or approves the merge of `th-2-tuning`. For each
change, on `th-2-tuning`: edit `Tools/Markov/Theory/virtuoso.json` (or the script, for a rule),
re-run `python -I Tools/Markov/Theory/virtuoso.py`, then
`python -I Tools/markov.py Tools/Markov/Source Tools/Markov/Theory/Output Assets/Aurumworks/Resources/Markov`,
then the EditMode tests (`TheoryTableTests` checks the pulls, counts and the start on 1). Record
his rulings in Design, what was built in Tech, *As built (TH-2)*. Before the merge: the menu check
(`bash Tools/menucheck.sh th2`).

Read for it: Tech, *Melody tables*, *As built (TH-1)* and *(TH-2)*; the Theory plan, *TH-2* and
*Risks*; the docstring of `Tools/Markov/Theory/virtuoso.py`.

Open for Jon: the Lydian pull is still the weakest (after ♯4: 5 43%, 3 31%). To make it
stronger, raise `pull`, or have pulls apply inside figures too. The second changes the rules, so
it is his call.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
