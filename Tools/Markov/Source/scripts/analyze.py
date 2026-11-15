"""Build scale-degree distributions and n-gram (Markov) tables.

Input: JSON lists from extract.py. Tonic = final note. Mode is inferred from
pitch content relative to the final (best-fitting diatonic mode, plus a
'harmonic_minor' class when b3 and natural 7 both dominate their rivals).
"""
import json, sys, os, collections, csv

DEG = ["1", "b2", "2", "b3", "3", "4", "#4", "5", "b6", "6", "b7", "7"]
MODES = {
    "ionian":     {0, 2, 4, 5, 7, 9, 11},
    "dorian":     {0, 2, 3, 5, 7, 9, 10},
    "phrygian":   {0, 1, 3, 5, 7, 8, 10},
    "lydian":     {0, 2, 4, 6, 7, 9, 11},
    "mixolydian": {0, 2, 4, 5, 7, 9, 10},
    "aeolian":    {0, 2, 3, 5, 7, 8, 10},
    "harmonic_minor": {0, 2, 3, 5, 7, 8, 11},
}
PREF = ["ionian", "aeolian", "mixolydian", "dorian", "harmonic_minor", "phrygian", "lydian"]
MAX_ORDER = 4
MIN_TUNES = 50
CN = {"ionian": "gong", "dorian": "shang", "phrygian": "jue", "mixolydian": "zhi", "aeolian": "yu"}

def classify(degs):
    h = collections.Counter(degs)
    best = max(PREF, key=lambda m: (sum(h[p] for p in MODES[m]), -PREF.index(m)))
    # A tune that never uses the 6th (or 7th, ...) is ambiguous; flag pentatonic/hexatonic
    used = {p for p, c in h.items() if c / len(degs) >= 0.01}
    return best, len(used)

def main(out_dir, specs):
    os.makedirs(out_dir, exist_ok=True)
    groups = collections.defaultdict(list)
    xcheck = collections.Counter()
    for spec in specs:
        path, label, filt = spec.split(":")
        for x in json.load(open(path)):
            if filt == "han" and not x["file"].startswith("han"): continue
            if filt == "nohan" and x["file"].startswith("han"): continue
            mode, nused = classify(x["degs"])
            if label == "irish":
                xcheck[(x["mode"], mode)] += 1
            if label != "chinese_han" and mode in ("phrygian", "lydian"):
                continue  # final is almost certainly not the tonic (e.g. major tune ending on 3)
            if label == "chinese_han":
                mode = CN.get(mode, mode)
            groups[(label, mode)].append(x["degs"])
    summary = {}
    for (label, mode), seqs in sorted(groups.items()):
        if len(seqs) < MIN_TUNES: continue
        n_notes = sum(map(len, seqs))
        uni = collections.Counter(d for s in seqs for d in s)
        first = collections.Counter(s[0] for s in seqs)
        ngrams = {}
        for k in range(1, MAX_ORDER + 1):
            c = collections.defaultdict(collections.Counter)
            for s in seqs:
                for i in range(len(s) - k):
                    ctx = " ".join(DEG[d] for d in s[i:i + k])
                    c[ctx][DEG[s[i + k]]] += 1
            ngrams[k] = c
        obj = {
            "corpus": label, "mode": mode, "tunes": len(seqs), "notes": n_notes,
            "degree_labels": DEG,
            "unigram_counts": {DEG[i]: uni[i] for i in range(12)},
            "unigram_prob": {DEG[i]: round(uni[i] / n_notes, 5) for i in range(12)},
            "first_note_counts": {DEG[i]: first[i] for i in range(12) if first[i]},
            "transitions": {
                f"order_{k}": {ctx: dict(nxt.most_common()) for ctx, nxt in
                               sorted(ngrams[k].items(), key=lambda kv: -sum(kv[1].values()))}
                for k in ngrams},
        }
        fn = f"{label}_{mode}"
        json.dump(obj, open(os.path.join(out_dir, fn + ".json"), "w"), indent=1)
        # order-1 matrix CSV (probabilities)
        with open(os.path.join(out_dir, fn + "_order1.csv"), "w", newline="") as f:
            w = csv.writer(f); w.writerow(["from\\to"] + DEG + ["n"])
            for i, d in enumerate(DEG):
                row = ngrams[1].get(d, {}); t = sum(row.values())
                if t == 0: continue
                w.writerow([d] + [f"{row.get(e, 0) / t:.4f}" for e in DEG] + [t])
        summary[fn] = {"tunes": len(seqs), "notes": n_notes,
                       "unigram_prob": obj["unigram_prob"]}
    json.dump(summary, open(os.path.join(out_dir, "summary.json"), "w"), indent=1)
    print("irish signature-mode vs content-mode:")
    for k, v in sorted(xcheck.items(), key=lambda kv: -kv[1])[:20]: print(" ", k, v)
    for k, v in summary.items(): print(k, v["tunes"], v["notes"])

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2:])
