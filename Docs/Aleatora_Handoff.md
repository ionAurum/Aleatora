# Aleatora: hand-off

Status (2026-11-15): **the MIDI in plan is finished and merged to `main`** (IN-1 to IN-4;
Design, rulings 37 to 47). Jon ran IN-4: following Reaper, leading it with *Send clock*, and a
keyboard into the *Monitor* all worked as expected; he accepted decisions 1, 6, 7 and 10 and the
IN-2 and IN-3 readings as built. On `main`: the Markov plan, MIDI out, MIDI in with clock and
sync. 268 EditMode tests; the menu check at 185 checks at both sizes.

Jon's loopMIDI ports are now "Aleatora to Reaper" (was Greedo), "Reaper to Aleatora" (was Han)
and "Aleatora Test" (ruling 45). "Greedo" and "Han" in the docs, comments and tests are examples
only.

The keyboard's port shared by Reaper and Aleatora at once works too (Jon, after the merge;
ruling 45).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## In discussion: more modes for the Markov tables

Jon wants Lydian, Phrygian, Byzantine (double harmonic) and the Hindu scale (his words:
"pentatonic phrygian dominant"; which notes that is, he hasn't said yet). The folk corpora we
use have none of them. Sources C2 found (2026-11-15), each labelled by mode, so no inference
from pitch content:
- **Phrygian, and Lydian (weakly):** the CantusCorpus (github.com/bacor/CantusCorpus), plainchant
  from the Cantus database in Volpiano, with the church mode per chant: modes 3/4 Phrygian, 5/6
  Lydian (often sung with B♭, which makes it Ionian on F). CC BY-NC-SA 4.0. Chant is unmetered and
  stepwise. For a folk Lydian: maybe the Polish Kolberg corpus (Podhale tunes), in Humdrum;
  unchecked.
- **Phrygian dominant:** Beregovski's klezmer tunes, mode-tagged *freygish*, in Humdrum
  (shanahdt.github.io/mode_in_klezmer; licence unchecked); SymbTr's makam Hicaz.
- **Double harmonic:** SymbTr (github.com/MTG/SymbTr; 2,200 Turkish makam pieces, makam per piece,
  MusicXML and text; CC BY-NC-SA 4.0): makam Hicazkar (and Zirgüleli Hicaz). Its tuning is in
  commas of 53; rounding to 12 semitones is needed.
- **Hindu scale:** no corpus found; depends on which notes Jon means.
Open for Jon: which Hindu scale; whether the NC licences suit Aleatora; then a plan
(`Tools/Markov/Source/scripts` would read mode labels instead of inferring the mode).

## Next: Jon picks the next plan

Nothing is planned. The candidates: the modes above, or (Design, *Open, each its own plan later*): OSC (Jon means to
teach Aleatora Reaper's OSC, ruling 39), what the keyboard's notes do, what the grid means
musically, how the generator drives the grid. When Jon picks one, write its plan as
`Docs/Aleatora_<Name>_Plan.md`, in the shape of `Docs/Aleatora_MidiIn_Plan.md` (*Read first*,
*Decisions for Jon* with defaults, milestones, *Risks*), and stop for his rulings.

Read for it:
- `Docs/Aleatora_Design.md`: *Open, each its own plan later*, and rulings 37 to 47.
- `Docs/Aleatora_MidiIn_Plan.md`: *Later*, as a model of a plan.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
