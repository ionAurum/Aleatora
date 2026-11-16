# Aleatora: hand-off

Status (2026-11-15): **the theory tables plan is written and waits for Jon's decisions**
(`Docs/Aleatora_Theory_Plan.md`; Design, rulings 48 to 50). On `main`: the Markov plan, MIDI
out, MIDI in with clock and sync (all finished, rulings to 47). 268 EditMode tests; the menu
check at 185 checks at both sizes.

The brief: new Markov tables (orders 1 to 4) for Lydian, Phrygian, double harmonic and the Hindu
pentatonic (1 3 4 5 ♭7), written from music theory in the style of rock guitar virtuosos
(Satriani, Thorn, Loureiro, Timmons, Petrucci). Aleatora will be sold, so no non-commercial
data (CantusCorpus and SymbTr are out). No fetch and analyse pipeline: a script writes phrases
by rule and counts them into the tables' usual JSON.

Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Open for Jon

The plan's *Decisions for Jon*, 1 to 6: which modes beyond his four; the set names
(`virtuoso_*`); the figures' weights; chromatic passing notes; 20,000 phrases a mode; Python's
seeded generator in the offline script. Then the go-ahead for TH-1.

## Next: TH-1, once Jon rules

Record his rulings in `Docs/Aleatora_Design.md`, then build TH-1 on `th-1-tables` from `main`.

Read for it:
- `Docs/Aleatora_Theory_Plan.md`: all of it (it is short).
- `Docs/Aleatora_Markov_Plan.md`: *The tables on disk*.
- `Docs/Aleatora_Tech.md`: *Melody tables* and *Melody generator*.
- `Tools/markov.py` (its docstring) and `Tools/Markov/Source/README.md`.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
