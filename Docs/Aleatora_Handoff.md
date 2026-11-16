# Aleatora: hand-off

Status (2026-11-16): **A fifth theory set, the phrygian dominant, is on
`th-2-phrygian-dominant`** (from `main`, not merged; Design, ruling 57): `virtuoso_phrygian_dominant`,
1 ♭2 3 4 5 ♭6 ♭7, pulls ♭2 → 1 and ♭6 → 5, 22 sets in all. EditMode tests 297 of 297; the menu
check `th2pd` exit 0, 186 checks held (Tech, *As built (TH-2)*). On `main`: the other four
Virtuoso sets, which Jon approved (ruling 56), starting on 1, the double harmonic shown as
"Byzantine"; the Markov plan, MIDI out, MIDI in with clock and sync (all finished).

Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Next: Jon plays the phrygian dominant; then merge, ending the theory plan

Jon picks "Virtuoso · phrygian dominant" on the *Melodies* page and plays it into Reaper. If it
sounds right, merge `th-2-phrygian-dominant` to `main` (a ruling in Design), which finishes the
theory plan: mark it finished in `CLAUDE.md` and here. For a change, on the same branch: edit
`Tools/Markov/Theory/virtuoso.json` (or the script, for a rule), re-run
`python -I Tools/Markov/Theory/virtuoso.py`, then
`python -I Tools/markov.py Tools/Markov/Source Tools/Markov/Theory/Output Assets/Aurumworks/Resources/Markov`,
then the EditMode tests.

Read for it: Tech, *Melody tables*, *As built (TH-1)* and *(TH-2)*; the Theory plan, *TH-2* and
*Risks*; the docstring of `Tools/Markov/Theory/virtuoso.py`.

Open for Jon (not pressing; he approved the sets as they are): the Lydian pull is still the weakest (after ♯4: 5 43%, 3 31%). To make it
stronger, raise `pull`, or have pulls apply inside figures too. The second changes the rules, so
it is his call.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
