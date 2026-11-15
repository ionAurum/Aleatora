"""Generate a melody (scale degrees) from one of the tables, with back-off.

usage: python sample_melody.py european_ionian.json [order=3] [length=32] [temperature=1.0]
Back-off: if the order-k context was never observed, fall back to k-1, ... down to 0 (unigram).
"""
import json, random, sys

def sample(table, order=3, length=32, temp=1.0, start=None):
    trans = table["transitions"]
    uni = table["unigram_counts"]
    first = table["first_note_counts"]
    seq = [start or random.choices(list(first), weights=list(first.values()))[0]]
    while len(seq) < length:
        for k in range(min(order, len(seq)), -1, -1):
            dist = uni if k == 0 else trans[f"order_{k}"].get(" ".join(seq[-k:]))
            if dist:
                break
        items = [(d, c) for d, c in dist.items() if c > 0]
        w = [c ** (1.0 / temp) for _, c in items]
        seq.append(random.choices([d for d, _ in items], weights=w)[0])
    return seq

if __name__ == "__main__":
    t = json.load(open(sys.argv[1]))
    order = int(sys.argv[2]) if len(sys.argv) > 2 else 3
    n = int(sys.argv[3]) if len(sys.argv) > 3 else 32
    temp = float(sys.argv[4]) if len(sys.argv) > 4 else 1.0
    print(" ".join(sample(t, order, n, temp)))
