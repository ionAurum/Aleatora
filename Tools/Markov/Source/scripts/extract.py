"""Extract scale-degree sequences from music21's bundled folk corpora.

Tonic = final note's pitch class. Mode = final's position relative to the
key signature's major tonic (0=ionian, 2=dorian, 4=phrygian, 5=lydian,
7=mixolydian, 9=aeolian). Tunes with key-signature changes or with a final
outside the diatonic set are dropped.
"""
import json, os, sys, glob, warnings
import music21
from music21 import converter, key, note

warnings.filterwarnings("ignore")
music21.environment.UserSettings  # noqa
CORPUS = os.path.join(os.path.dirname(music21.__file__), "corpus")
MODES = {0: "ionian", 2: "dorian", 4: "phrygian", 5: "lydian", 7: "mixolydian", 9: "aeolian"}

def tune_seq(s):
    s = s.stripTies()
    parts = s.parts if s.parts else [s]
    flat = parts[0].flatten()
    ks = list(flat.getElementsByClass(key.KeySignature))
    if not ks or len({k.sharps for k in ks}) > 1:
        return None
    notes = [n for n in flat.notes if isinstance(n, note.Note)]
    if len(notes) < 8:
        return None
    major_tonic = ks[0].asKey("major").tonic.pitchClass
    tonic = notes[-1].pitch.pitchClass
    off = (tonic - major_tonic) % 12
    if off not in MODES:
        return None
    degs = [(n.pitch.pitchClass - tonic) % 12 for n in notes]
    # register relative to the final tonic, in semitones (for optional use)
    midi = [n.pitch.midi - notes[-1].pitch.midi for n in notes]
    return {"mode": MODES[off], "degs": degs, "rel": midi}

def main(corpora, out):
    res = []
    for c in corpora:
        files = sorted(glob.glob(os.path.join(CORPUS, c, "*.abc")))
        ok = bad = 0
        for f in files:
            try:
                op = converter.parse(f)
            except Exception as e:
                print("parse fail", f, e, file=sys.stderr); continue
            scores = op.scores if hasattr(op, "scores") else [op]
            for s in scores:
                try:
                    r = tune_seq(s)
                except Exception:
                    r = None
                if r:
                    r["corpus"] = c; r["file"] = os.path.basename(f)
                    res.append(r); ok += 1
                else:
                    bad += 1
        print(c, "kept", ok, "dropped", bad, file=sys.stderr)
    json.dump(res, open(out, "w"))

if __name__ == "__main__":
    main(sys.argv[2:], sys.argv[1])
