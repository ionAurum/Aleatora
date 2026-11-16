# Aleatora: hand-off

Status (2026-11-16): **TH-2's first round is merged to `main`** (Design, rulings 53 to 55):
every Virtuoso phrase starts on 1 (in `virtuoso.py`; the folk sets unchanged), and the double
harmonic set shows as "Virtuoso · Byzantine". EditMode tests 291 of 291; the menu check has not
been run since TH-1 (Tech, *As built (TH-2)*). On `main` too: TH-1, the Markov plan, MIDI out,
MIDI in with clock and sync (all finished).

Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Next: TH-2, more listening if Jon wants it; then the plan is finished

Jon plays the Virtuoso sets again and says what else to change, or that TH-2 is done. For a
change, on a new branch from `main` (`th-2-tuning-2`): edit `Tools/Markov/Theory/virtuoso.json`
(or the script, for a rule), re-run `python -I Tools/Markov/Theory/virtuoso.py`, then
`python -I Tools/markov.py Tools/Markov/Source Tools/Markov/Theory/Output Assets/Aurumworks/Resources/Markov`,
then the EditMode tests (`TheoryTableTests` checks the pulls, counts and the start on 1). Record
his rulings in Design, what was built in Tech, *As built (TH-2)*. The menu check
(`bash Tools/menucheck.sh th2`) is owed: run it with the next change, or on `main` when Jon
allows (it plays on his screen).

Read for it: Tech, *Melody tables*, *As built (TH-1)* and *(TH-2)*; the Theory plan, *TH-2* and
*Risks*; the docstring of `Tools/Markov/Theory/virtuoso.py`.

Open for Jon: the Lydian pull is still the weakest (after ♯4: 5 43%, 3 31%). To make it
stronger, raise `pull`, or have pulls apply inside figures too. The second changes the rules, so
it is his call.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
