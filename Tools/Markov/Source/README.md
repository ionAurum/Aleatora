# Melodic scale-degree statistics: unigram + Markov (orders 1–4)

Computed from ~10,000 folk melodies bundled with [music21](https://web.mit.edu/music21/):
- the **Essen Folksong Collection** (German and Chinese Han subsets)
- **O'Neill's *Music of Ireland* (1850)**
- **Ryan's Mammoth Collection (1883)**

## Files

For each `<corpus>_<mode>`:

| File | Contents |
|---|---|
| `<set>.json` | Full data: tune/note counts, unigram counts and probabilities, first-note counts, and transition **counts** for orders 1–4 |
| `<set>_order1.csv` | First-order transition matrix as probabilities (rows = current degree, columns = next), with row totals in `n` |
| `summary.json` | Unigram probabilities for every set |
| `sample_melody.py` | Example generator with back-off from order k down to unigram, plus a temperature setting |

The transition tables store raw counts, not probabilities, so you can apply your own smoothing (add-k, Kneser-Ney, etc.). Order-k contexts are space-separated degree strings, oldest first. For example, `transitions.order_3["5 4 3"]` gives counts for the note that follows 5→4→3.

### Available sets

| Set | Tunes | Notes |
|---|---|---|
| `european_ionian` (major) | 7136 | 481k |
| `european_aeolian` (natural minor) | 878 | 57k |
| `european_dorian` | 478 | 40k |
| `european_mixolydian` | 371 | 29k |
| `european_harmonic_minor` (♭3 with raised 7) | 238 | 11k |
| `german_*`, `irish_*` | the European sets split by source | |
| `chinese_han_gong / zhi / yu` | 659 / 128 / 397 | pentatonic modes (≈ ionian / mixolydian / aeolian finals) |

The `european_*` sets pool German and Irish tunes. German melodies are mostly vocal and stepwise; Irish ones are mostly dance tunes with more leaps and arpeggios. Pick one or the other if that difference matters for your tool.

## Unigram probabilities (proportion of notes)

| Set | 1 | ♭2 | 2 | ♭3 | 3 | 4 | ♯4 | 5 | ♭6 | 6 | ♭7 | 7 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| european_ionian | .216 | .001 | .151 | .001 | .179 | .098 | .003 | .189 | .001 | .086 | .002 | .074 |
| european_aeolian | .196 | .001 | .131 | .166 | .016 | .140 | .001 | .178 | .043 | .006 | .109 | .014 |
| european_dorian | .196 | .000 | .163 | .102 | .009 | .135 | .002 | .145 | .007 | .074 | .152 | .016 |
| european_mixolydian | .208 | .001 | .131 | .004 | .095 | .165 | .001 | .159 | .001 | .138 | .088 | .009 |
| european_harmonic_minor | .221 | .001 | .184 | .139 | .037 | .116 | .002 | .190 | .021 | .006 | .015 | .067 |
| chinese_han_gong | .218 | .000 | .202 | .000 | .073 | .145 | .000 | .201 | .000 | .154 | .000 | .007 |

## Method and caveats

- **Degrees are pitch classes relative to the tonic.** Octave and contour are not encoded, so 5→1 does not say whether the melody leaps up a 4th or down a 5th. Add interval direction yourself if you need it; the extraction step keeps semitone-from-tonic data for this.
- **Counts are per note, not weighted by duration.** Tied notes are merged, rests are skipped, and written repeats are not expanded.
- **Tonic = the final note.** This is standard for folk repertoire. Mode is inferred from pitch content as the diatonic mode whose scale contains the most notes of the tune, measured from the final.
  - The Essen ABC files label `K:` with the tonic rather than the key signature, so minor tunes would otherwise be misread as major.
  - Checked against the Irish files' explicit modal key signatures (e.g. `K:Ador`), the inferred mode agrees for about 90% of tunes. The most common disagreement is A-minor-signature tunes that really use F♯, which are labeled Dorian here.
- **Excluded:** tunes whose inferred "mode" was Phrygian or Lydian in the European sets (≈1,100). Their pitch content shows they are major tunes ending on 3 or 4, not genuinely modal tunes. Groups with fewer than 50 tunes are also left out.
- **Higher orders are sparse.** Order 4 has ~4,500 observed contexts in major and far fewer in the smaller modes. Use back-off or smoothing, and treat rare transitions (often chromatic, count < 5) with some suspicion, since some are encoding errors.

## Published reference points

- Krumhansl & Kessler (1982) key profiles are **listener ratings** (probe-tone experiments), not usage counts. They are often used as a proxy but are a different kind of data.
- Temperley, *Music and Probability* (2007), and Huron, *Sweet Anticipation* (2006), report Essen scale-degree frequencies and transition probabilities that this data should broadly reproduce.
- Verosky (2023, *Empirical Musicology Review*) found Essen scale-degree profiles correlate almost perfectly with popular North American children's songs.
