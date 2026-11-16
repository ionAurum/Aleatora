# Aleatora: hand-off

Status (2026-11-16): **The theory plan is finished and merged to `main`** (Design, rulings 53
to 58). Five Virtuoso sets (Lydian, Phrygian, Byzantine (double harmonic), the Hindu pentatonic,
the phrygian dominant) ship with the 17 folk sets, 22 in all; every Virtuoso phrase starts and
ends on 1. EditMode tests 297 of 297; the menu check `th2pd` exit 0, 186 checks held (Tech,
*Checks*, *Runs*). Also finished on `main`: the Markov plan, MIDI out, MIDI in with clock and sync.

Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Next: Jon picks the next plan

No plan is under way. The open questions, each its own plan later, are in Design, *Open, each
its own plan later*: what the grid means musically, how the generator drives the grid, OSC out
(Reaper's OSC, ruling 39), what the keyboard's notes do (ruling 37), and rhythm (the MIDI plan's
*Later*). When Jon picks one, write its plan in `Docs/` the way the earlier plans are written,
and wait for his go-ahead.

Read for it: Design, *Open, each its own plan later*; the latest plan
(`Docs/Aleatora_Theory_Plan.md`) for the shape.

Left open, not pressing (Jon approved the sets as they are): the Lydian pull is the weakest
(after ♯4: 5 43%, 3 31%); raise `pull` in `Tools/Markov/Theory/virtuoso.json`, or have pulls
apply inside figures too (a rule change, his call).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
