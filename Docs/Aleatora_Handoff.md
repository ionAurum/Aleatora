# Aleatora: hand-off

Status (2026-11-15): **TH-1 is built on `th-1-tables` but not yet run in Unity.** Jon's Editor
had the project open, so the EditMode tests and the menu check haven't run. Jon took the theory
plan's defaults (Design, ruling 51). On `main`: the Markov plan, MIDI out, MIDI in with clock and
sync (all finished, rulings to 47). Expected: 291 EditMode tests (268 before), the menu check at
186 checks (185 before).

TH-1 (Tech, *Melody tables*, *As built (TH-1)*): `Tools/Markov/Theory/virtuoso.py` and
`virtuoso.json` write 20,000 phrases a mode by rule. `markov.py` takes several source folders.
The four `virtuoso_*` tables are in `Resources/Markov` (21 sets), and the 17 folk tables are
unchanged byte for byte. `ModeScale` and `ViewText.SetName` have the four modes, and
`TheoryTableTests` is new. The script's own checks all hold.

Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Next: finish TH-1, then TH-2

1. With no Editor open (`Temp/UnityLockfile`): run the EditMode tests (291 expected), then
   `bash Tools/menucheck.sh th1` on `th-1-tables`. Commit the `.meta` files Unity makes for the
   four `virtuoso_*.txt` and `TheoryTableTests.cs` (an open Editor may already have made them).
   Put the runs in Tech, *Checks*, *Runs*, and the menu check's logged order-4 melodies in the
   report to Jon.
2. TH-2 (`Docs/Aleatora_Theory_Plan.md`, *TH-2*): Jon plays each `Virtuoso · …` set into Reaper
   and says what to change in `Tools/Markov/Theory/virtuoso.json`. Re-run
   `python -I Tools/Markov/Theory/virtuoso.py`, then
   `python -I Tools/markov.py Tools/Markov/Source Tools/Markov/Theory/Output Assets/Aurumworks/Resources/Markov`.
   Merge once he approves.

Read for it: Tech, *As built (TH-1)* and *Checks* (TH-1 lines); the Theory plan, *TH-2* and
*Risks*.

Open for Jon: the Lydian pull is the weakest (after ♯4: 5 39%, 3 33%). If he wants it stronger,
raise `pull` or have pulls apply inside figures too. That second option changes the rules, so it
is his call.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
