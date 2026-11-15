"""Turns the melody statistics in Tools/Markov/Source/ into the text tables Aleatora reads.

    python -I Tools/markov.py <source folder> <output folder>

<source folder> holds one JSON file per set (european_ionian.json, ...), as the analysis in
Tools/Markov/Source/scripts/ wrote them, and summary.json, which is skipped. <output folder> is
Assets/Aurumworks/Resources/Markov/. For each set it writes <set>.txt, and it writes Index.txt
listing the sets. It then reads every file it wrote back and checks it (the checks below); it
exits 1 if any fails, and writes nothing over a file that is already identical.

The text format (Docs/Aleatora_Markov_Plan.md, The tables on disk), one record per line:

    aleatora-markov 1                 the format and its version
    set european_ionian               the set's name (its file name)
    corpus european                   the collection it came from
    mode ionian                       the mode
    tunes 7136                        how many melodies
    notes 481199                      how many notes in them
    unigram <12 counts>               how often each degree is played
    first <12 counts>                 how often each degree begins a melody
    order 1 <contexts>                the order's header and how many context lines follow
    1 | <12 counts>                   a context (oldest degree first), then the next note's counts
    ...
    order 2 <contexts>
    3 2 | <12 counts>
    ...

Degrees are written 1 b2 2 b3 3 4 #4 5 b6 6 b7 7 (semitones 0 to 11 above the tonic), and every
row of 12 counts is in that order. Lines are LF-terminated; a line starting with // is a comment (not #, which begins the degree #4).

The checks: the unigram sums to notes; first sums to tunes; each order k's counts sum to
notes - k * tunes (every melody has at least 8 notes, so each gives exactly len - k); no count is
negative; no context repeats; every context has k known degrees.
"""
import json
import os
import sys

DEGREES = ["1", "b2", "2", "b3", "3", "4", "#4", "5", "b6", "6", "b7", "7"]
INDEX = {name: i for i, name in enumerate(DEGREES)}
FORMAT = "aleatora-markov 1"


def row(counts):
    return " ".join(str(counts.get(d, 0)) for d in DEGREES)


def write_set(data):
    lines = [
        FORMAT,
        f"set {data['corpus']}_{data['mode']}",
        f"corpus {data['corpus']}",
        f"mode {data['mode']}",
        f"tunes {data['tunes']}",
        f"notes {data['notes']}",
        f"unigram {row(data['unigram_counts'])}",
        f"first {row(data['first_note_counts'])}",
    ]
    order = 1
    while f"order_{order}" in data["transitions"]:
        table = data["transitions"][f"order_{order}"]
        # Contexts sorted by their degrees, oldest first, so a diff between versions stays small.
        keys = sorted(table, key=lambda c: [INDEX[d] for d in c.split()])
        lines.append(f"order {order} {len(keys)}")
        for context in keys:
            lines.append(f"{context} | {row(table[context])}")
        order += 1
    return "\n".join(lines) + "\n"


def check(text):
    """Reads a table back and returns a list of what is wrong with it (empty if nothing)."""
    errors = []
    head = {}
    orders = {}
    current = None
    expected = 0
    for number, line in enumerate(text.split("\n"), 1):
        if not line or line.startswith("//"):
            continue
        if number == 1:
            if line != FORMAT:
                errors.append(f"line 1: expected '{FORMAT}'")
            continue
        word, _, rest = line.partition(" ")
        if word in ("set", "corpus", "mode"):
            head[word] = rest
        elif word in ("tunes", "notes"):
            head[word] = int(rest)
        elif word in ("unigram", "first"):
            counts = [int(x) for x in rest.split()]
            if len(counts) != 12:
                errors.append(f"line {number}: {word} needs 12 counts")
            head[word] = counts
        elif word == "order":
            if current is not None and len(orders[current]) != expected:
                errors.append(f"order {current}: {len(orders[current])} contexts, header said {expected}")
            k, n = (int(x) for x in rest.split())
            current, expected = k, n
            orders[k] = {}
        else:
            context, bar, counts = line.partition(" | ")
            degrees = context.split()
            values = [int(x) for x in counts.split()]
            if not bar or current is None:
                errors.append(f"line {number}: not a context line")
            elif len(degrees) != current or any(d not in INDEX for d in degrees):
                errors.append(f"line {number}: context '{context}' is not {current} known degrees")
            elif len(values) != 12 or any(v < 0 for v in values):
                errors.append(f"line {number}: needs 12 counts, none negative")
            elif context in orders[current]:
                errors.append(f"line {number}: context '{context}' repeats")
            else:
                orders[current][context] = values
    if current is not None and len(orders[current]) != expected:
        errors.append(f"order {current}: {len(orders[current])} contexts, header said {expected}")
    if sum(head.get("unigram", [])) != head.get("notes"):
        errors.append("unigram does not sum to notes")
    if sum(head.get("first", [])) != head.get("tunes"):
        errors.append("first does not sum to tunes")
    for k, table in orders.items():
        total = sum(sum(v) for v in table.values())
        want = head["notes"] - k * head["tunes"]
        if total != want:
            errors.append(f"order {k} sums to {total}, not notes - {k} * tunes = {want}")
    return errors


def write_if_changed(path, text):
    if os.path.exists(path):
        with open(path, encoding="utf-8", newline="") as f:
            if f.read() == text:
                return False
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    return True


def main(source, output):
    os.makedirs(output, exist_ok=True)
    names = []
    failed = False
    for file in sorted(os.listdir(source)):
        if not file.endswith(".json") or file == "summary.json":
            continue
        with open(os.path.join(source, file), encoding="utf-8") as f:
            data = json.load(f)
        text = write_set(data)
        name = f"{data['corpus']}_{data['mode']}"
        errors = check(text)
        for error in errors:
            print(f"{name}: {error}")
        failed |= bool(errors)
        changed = write_if_changed(os.path.join(output, name + ".txt"), text)
        names.append(name)
        print(f"{name}: {data['tunes']} tunes, {data['notes']} notes{' (written)' if changed else ''}")
    index = "// The sets in this folder, one per line (Tools/markov.py writes it).\n" + "\n".join(names) + "\n"
    write_if_changed(os.path.join(output, "Index.txt"), index)
    print(f"{len(names)} sets, {'FAILED' if failed else 'all checks held'}")
    return 1 if failed else 0


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(2)
    sys.exit(main(sys.argv[1], sys.argv[2]))
