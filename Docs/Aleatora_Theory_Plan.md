# Aleatora — melody tables from theory: plan

New Markov tables, orders 1 to 4, for modes the folk corpora don't have, written from music
theory rather than learned from recordings or scores. The style aimed at is the instrumental
rock guitar of Joe Satriani, Pete Thorn, Kiko Loureiro's solo work, Andy Timmons and John
Petrucci: scale runs, sequences in threes and fours, pedal-point licks, arpeggios, and the mode's
colour note pushed to the front.

Jon's brief (2026-11-15; Design, rulings 48 to 50): Aleatora will be sold, so no data under a
non-commercial licence; the modes are Lydian, Phrygian, Byzantine (double harmonic) and the
Hindu pentatonic (1 3 4 5 ♭7); no fetch, analyse and learn pipeline: "generate the 1st through
4th order Markov chains based on standard Western music theory".

**Not in this plan:** rhythm (every note is still a quarter note; the MIDI plan's *Later*), bends,
slides, vibrato, legato and anything else that lives in a player's hands; contour (the tables
hold degrees, and `Voicing` places each nearest the note before); a change to the generator or
the page beyond the set list.

Written 2026-11-15 from `main` after the MIDI in plan (rulings to 47). **Finished 2026-11-16**
(Design, ruling 58), with a fifth mode, the phrygian dominant (ruling 57), and every phrase
starting on 1 (ruling 53).

---

## Read first

- `Tools/Markov/Source/README.md`: what a set's JSON holds.
- `Docs/Aleatora_Markov_Plan.md`, *The tables on disk*: the text format and its checks.
- `Docs/Aleatora_Tech.md`, *Melody tables* and *Melody generator*: `ModeScale`, the reader,
  `Voicing`.

## How it works

Rather than write 2,401 order-4 rows by hand per mode, a script writes **phrases** by the rules
below, tens of thousands per mode, and counts them exactly as the analysis counted folk tunes.
The counts go into the same JSON shape (`tunes`, `notes`, unigram, first notes, orders 1 to 4),
and `Tools/markov.py` turns them into tables like any other set. Orders 3 and 4 then carry the
figures whole (a run in threes is `1 2 3 → 2`, `2 3 2 → 3`, …), which is what higher orders are
for. A rule's weight changes how often its figure appears, so the style is tuned by the weights
file, and the script is run again.

A phrase is written in pitch (semitones, two octaves and a bit), so steps and leaps are real,
then each note is counted as its degree. It is:

1. **A start** on a strong degree: 1, 5 or 3 (the mode's own 3rd), or the colour note.
2. **Two to five figures**, each starting a step or a third from where the last one ended:
   - *Run*: four to eight scale steps up or down.
   - *Sequence*: groups of three or four scale steps, each group a step higher or lower than the
     last (`1 2 3, 2 3 4, 3 4 5`), up or down.
   - *Arpeggio*: every other scale degree (`1 3 5 7` in a seven-note mode; in the pentatonic
     that gives fourths), up or down, three to five notes.
   - *Pedal*: a held note alternating with a line moving away from it (`5 1 5 2 5 3`).
   - *Turn*: a note, a step above, the note, a step below, the note.
   - *Leap and step back*: a leap of a fourth to a sixth, then a step the other way.
3. **The colour note's pull**, applied over everything: in Lydian ♯4 rises to 5; in Phrygian ♭2
   falls to 1; in double harmonic ♭2 falls to 1, 7 rises to 1 and ♭6 falls to 5, with the two
   augmented seconds (♭2–3, ♭6–7) kept as steps; in the Hindu pentatonic ♭7 falls to 5 or rises
   to 1, and 4 falls to 3. A figure that lands on the colour note is followed by its pull more
   often than not.
4. **An ending** on 1, approached by step or through the colour note's pull (so every phrase
   ends on its tonic, as the format's checks expect, and has at least 8 notes).

Chromatic passing notes (decision 4) fill a whole step in a run now and then (`5 ♯5 6`).

## Decisions for Jon

Each has the default this plan builds if Jon doesn't say otherwise.

1. **The modes.** **Default:** Jon's four: `lydian`, `phrygian`, `double_harmonic`,
   `hindu_pentatonic`. Others the same players use a great deal, each one line in the weights
   file: Phrygian dominant (Petrucci, Loureiro), Mixolydian and Lydian dominant (Satriani,
   Timmons), Dorian, harmonic minor, the minor pentatonic and the blues scale. Say which, if any.
2. **The set names.** **Default:** the corpus is `virtuoso`, so the sets are
   `virtuoso_lydian`, … , and the page's *Set* list grows from 17 to 21.
3. **The figures' weights.** **Default:** runs 30%, sequences 25%, arpeggios 15%, pedals 15%,
   turns 10%, leaps 5%; the colour note's pull followed 70% of the time. In
   `Tools/Markov/Theory/virtuoso.json`, which Jon edits and C2 runs again. (The tables are files
   in the build, not values read in play, so these don't belong on the tuning asset.)
4. **Chromatic passing notes.** **Default:** on, in about 3% of notes, only inside runs between
   two scale steps a whole step apart. The generator's *Chromatic weight* still governs how
   often they're played.
5. **How many phrases.** **Default:** 20,000 a mode: enough that every order-4 context a figure
   can produce is heard many times, and the files stay near the size of `european_ionian`'s.
6. **The script's random numbers.** **Default:** Python's `random.Random`, seeded, in the
   script. The rule *every random draw from `Music/Rng`* is the game's; this runs offline, and its
   output is committed, so the game stays deterministic.

## Code layout

- `Tools/Markov/Theory/virtuoso.py` (new): the phrase writer and counter; Python 3, standard
  library, `python -I`. Writes `Tools/Markov/Theory/Output/virtuoso_<mode>.json`.
- `Tools/Markov/Theory/virtuoso.json` (new): the modes (their degrees and colour notes), the
  weights, the seed and the phrase count.
- `Tools/markov.py`: takes more than one source folder, and lists every set in `Index.txt`.
- `Music/ModeScale`: the new modes' masks.
- The tests' and menu check's set count: 17 to 21.

## TH-1 — The tables (branch `th-1-tables`, from `main`)

1. The script and its weights file as above. It checks its own output before writing: each
   phrase ends on 1 and has 8 notes or more; no degree outside the mode but the chromatic
   passing notes; and each mode's colour pull shows in the counts (in Phrygian, 1 is ♭2's most
   likely next note; in Lydian, 5 is ♯4's).
2. `markov.py` with several source folders; run over `Tools/Markov/Source` and
   `Tools/Markov/Theory/Output` into `Assets/Aurumworks/Resources/Markov/`. The 17 folk tables
   come out byte for byte as they are.
3. `ModeScale`: `lydian`, `phrygian`, `double_harmonic`, `hindu_pentatonic`.
4. EditMode tests: the four sets read and pass the reader's checks; their masks; a melody from
   each stays in its mode at *Chromatic weight* 0. The menu check counts 21 sets and makes a
   melody from each.
5. Sample melodies (order 4, a few seeds) printed per mode in the report, as degree names, for a
   first read before Jon plays them.

## TH-2 — Jon plays them

Jon picks each new set on the *Melodies* page and plays it into Reaper, at different orders and
temperatures. He rules on the decisions and says what to change in the weights. C2 re-runs the
script for each change. Merge once he approves.

## Milestone summary

| # | Branch | Lands | Checked by |
|---|---|---|---|
| TH-1 | `th-1-tables` | The script, the four tables, `ModeScale`, the set count | Script's own checks, EditMode tests, menu check |
| TH-2 | merge | Jon's listening, the weights tuned, rulings | **Jon** |

## Risks

- **It will sound like its rules.** A table from rules can only give back what the rules put in:
  correct, idiomatic, and narrower than a player. The order (1 to 4) and temperature on the page
  are the levers between "a lick" and "wandering"; order 1 or 2 mostly forgets the figures.
- **Without rhythm, a lot of the style is missing.** Satriani or Petrucci, played as even quarter
  notes, is a scale exercise. The figures here are the part of their style that survives that.
  Rhythm is its own plan later.
- **Contour.** The tables hold degrees; `Voicing` places each nearest the note before, so an
  arpeggio that climbs two octaves in the script comes out folded within an octave or so in play.
- **Copyright.** The rules are textbook theory and general idiom; no player's solo is
  transcribed or copied. Naming players in the docs as inspiration is fine; naming them in the
  product isn't planned.
