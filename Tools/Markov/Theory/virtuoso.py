"""Writes melody statistics for modes the folk corpora don't have, from music theory rather than data.

    python -I Tools/Markov/Theory/virtuoso.py [weights.json] [output folder]

The weights default to virtuoso.json beside this script, the output to Output/ beside it. For each
mode in the weights it writes phrases by rule (Docs/Aleatora_Theory_Plan.md, How it works), in the
style of instrumental rock guitar: scale runs, sequences in threes and fours, arpeggios, pedal
licks, turns, leaps stepped back from, with the mode's colour notes pulled to where they lead. It
counts the phrases exactly as the folk analysis counted tunes and writes <corpus>_<mode>.json in
the same shape (Tools/Markov/Source/README.md), which Tools/markov.py turns into a table:

    python -I Tools/markov.py Tools/Markov/Source Tools/Markov/Theory/Output Assets/Aurumworks/Resources/Markov

A phrase is written in pitch, on scale steps over two octaves and a bit, so steps and leaps are
real; each note is then counted as its degree (semitones above the tonic, 0 to 11). It is:

1. A start on 1, in the middle octave (Design, ruling 53: every melody starts on the root).
2. Two to five figures, each starting a step or a third (one or two scale steps) from where the
   last one ended, up or down:
   run         four to eight scale steps one way; a whole step inside it is sometimes filled by
               its chromatic passing note (the only notes outside the mode)
   sequence    two to four groups of three or four steps, each group a step on from the last
   arpeggio    three to five notes on every other scale step
   pedal       a note alternating with a line of three or four steps moving away from it,
               within the octave
   turn        a note, its neighbour one way, the note, its neighbour the other way, the note
   leap        a leap of a fourth to a sixth (5 to 9 semitones), then a step back
3. A figure that ends on a colour note is followed by its pull (the weights' "pull" of the time):
   the nearest placement of the pull's target, the way it goes.
4. An ending on 1, by step: through a pull into 1 when the mode has one (the "pull" of the time),
   otherwise from the nearer side. A phrase under the minimum length is written again.

The random numbers are Python's random.Random, seeded from the weights: one seed is one set of
files (ruling 51). The script runs offline and its output is committed, so the game stays
deterministic.

Before writing, it checks its own phrases and counts, and exits 1 if any check fails: every phrase
starts and ends on 1 and has the minimum notes or more; no degree comes twice running; no note is
outside the mode but a passing note, and each passing note lies between two scale notes a whole step apart; each pulled degree's likeliest
next note (order 1) is one of its pulls' targets. It writes nothing over a file that is already
identical.
"""
import json
import os
import random
import sys

DEGREES = ["1", "b2", "2", "b3", "3", "4", "#4", "5", "b6", "6", "b7", "7"]
INDEX = {name: i for i, name in enumerate(DEGREES)}
FIGURES = ["run", "sequence", "arpeggio", "pedal", "turn", "leap"]
HERE = os.path.dirname(os.path.abspath(__file__))


class Mode:
    """A mode's scale, laid out on scale steps: step i is pitch(i) semitones above the tonic of
    octave 0, and steps 0, n, 2n, … are tonics."""

    def __init__(self, name, spec):
        self.name = name
        self.scale = [INDEX[d] for d in spec["degrees"].split()]
        if self.scale != sorted(set(self.scale)) or self.scale[0] != 0:
            raise ValueError(f"{name}: degrees must rise from 1 without repeats")
        self.n = len(self.scale)
        self.third = INDEX[spec["third"]]
        self.colour = INDEX[spec["colour"]]
        self.pulls = {}
        for pull in spec["pulls"]:
            source, target = INDEX[pull["from"]], INDEX[pull["to"]]
            if pull["way"] not in ("up", "down"):
                raise ValueError(f"{name}: a pull's way is up or down")
            self.pulls.setdefault(source, []).append((target, 1 if pull["way"] == "up" else -1))
        for degree in [self.third, self.colour, *self.pulls, *(t for ts in self.pulls.values() for t, _ in ts)]:
            if degree not in self.scale:
                raise ValueError(f"{name}: {DEGREES[degree]} is not in the mode")
        # Two octaves and a bit: from two steps under the low tonic to two over the high one.
        self.low = -2
        self.high = 2 * self.n + 2

    def pitch(self, step):
        octave, place = divmod(step, self.n)
        return 12 * octave + self.scale[place]

    def degree(self, step):
        return self.scale[step % self.n]

    def step_of(self, degree, octave):
        return octave * self.n + self.scale.index(degree)

    def inside(self, steps):
        return all(self.low <= s <= self.high for s in steps)


class Writer:
    """Writes one mode's phrases. A note is (pitch, passing): passing is True for a chromatic
    passing note, the only kind outside the mode."""

    def __init__(self, mode, weights, rng):
        self.mode = mode
        self.w = weights
        self.rng = rng
        self.figure_weights = [weights["figures"][f] for f in FIGURES]
        self.used = {f: 0 for f in FIGURES}
        self.passing = 0
        self.pulled = 0

    def chance(self, p):
        return self.rng.random() < p

    def figure(self, kind, start, way):
        """The figure's scale steps from start, going way (+1 up, -1 down), or None if it can't go."""
        r = self.rng
        if kind == "run":
            return [start + way * j for j in range(r.randint(4, 8))]
        if kind == "sequence":
            size, groups = r.choice([3, 4]), r.randint(2, 4)
            return [start + way * (g + j) for g in range(groups) for j in range(size)]
        if kind == "arpeggio":
            return [start + way * 2 * j for j in range(r.randint(3, 5))]
        if kind == "pedal":
            # The line stays inside the octave above or below the pedal, so it never meets the
            # pedal's own degree (in the pentatonic that limits how far away it can start).
            lines = r.randint(3, 4)
            away = r.randint(1, min(3, self.mode.n - lines))
            steps = []
            for j in range(lines):
                steps += [start, start + way * (away + j)]
            return steps
        if kind == "turn":
            return [start, start + way, start, start - way, start]
        if kind == "leap":
            here = self.mode.pitch(start)
            targets = [start + way * j for j in range(1, self.mode.n + 1)
                       if 5 <= abs(self.mode.pitch(start + way * j) - here) <= 9]
            if not targets:
                return None
            target = r.choice(targets)
            return [start, target, target - way]
        raise ValueError(kind)

    def notes_of(self, kind, steps):
        """The figure's notes, with passing notes filling a run's whole steps now and then."""
        notes = []
        for i, step in enumerate(steps):
            pitch = self.mode.pitch(step)
            if kind == "run" and i > 0:
                before = self.mode.pitch(steps[i - 1])
                if abs(pitch - before) == 2 and self.chance(self.w["chromatic"]):
                    notes.append(((pitch + before) // 2, True))
                    self.passing += 1
            notes.append((pitch, False))
        return notes

    def next_figure(self, current):
        """A figure starting a step or a third from current: its notes and its last scale step."""
        r = self.rng
        step_or_third = self.w["next_figure"]
        for _ in range(50):
            kind = r.choices(FIGURES, self.figure_weights)[0]
            offset = r.choices([1, 2], [step_or_third["step"], step_or_third["third"]])[0] * r.choice([-1, 1])
            way = r.choice([-1, 1])
            for o, w in ((offset, way), (-offset, -way)):
                steps = self.figure(kind, current + o, w)
                if steps and self.mode.inside(steps):
                    self.used[kind] += 1
                    return self.notes_of(kind, steps), steps[-1]
        raise RuntimeError(f"{self.mode.name}: no figure fits from step {current}")

    def pull(self, step):
        """The step a pull from this step's degree goes to, or None if the degree has no pull."""
        pulls = self.mode.pulls.get(self.mode.degree(step))
        if not pulls:
            return None
        target, way = self.rng.choice(pulls)
        to = step + way
        while self.mode.degree(to) != target:
            to += way
        return to if self.mode.inside([to]) else None

    def ending(self, current):
        """The scale steps from current (not included) down or up to a tonic, by step."""
        mode = self.mode
        into_one = [way for ts in mode.pulls.values() for t, way in ts if t == 0]
        if into_one and self.chance(self.w["pull"]):
            # A pull up into 1 comes from below: the ending rises.
            rising = self.rng.choice(into_one) == 1
        else:
            below = current - (current // mode.n) * mode.n
            above = (-current) % mode.n
            rising = above < below if above != below else self.chance(0.5)
        if rising:
            tonic = -(-current // mode.n) * mode.n
            if tonic > mode.high:
                tonic, rising = (current // mode.n) * mode.n, False
        else:
            tonic = (current // mode.n) * mode.n
            if tonic < mode.low:
                tonic, rising = -(-current // mode.n) * mode.n, True
        way = 1 if rising else -1
        if tonic == current:
            return [current - way, tonic]
        return list(range(current + way, tonic + way, way))

    def phrase(self):
        mode, r, w = self.mode, self.rng, self.w
        while True:
            current = mode.step_of(0, 1)
            notes = [(mode.pitch(current), False)]
            low, high = w["figures_per_phrase"]
            for _ in range(r.randint(low, high)):
                figure, current = self.next_figure(current)
                notes += figure
                if mode.degree(current) in mode.pulls and self.chance(w["pull"]):
                    to = self.pull(current)
                    if to is not None:
                        current = to
                        notes.append((mode.pitch(current), False))
                        self.pulled += 1
            notes += [(mode.pitch(s), False) for s in self.ending(current)]
            if len(notes) >= w["minimum_notes"]:
                return notes


def check_phrase(mode, notes, minimum):
    errors = []
    if notes[0][0] % 12 != 0:
        errors.append("starts on " + DEGREES[notes[0][0] % 12])
    if notes[-1][0] % 12 != 0:
        errors.append("ends on " + DEGREES[notes[-1][0] % 12])
    if len(notes) < minimum:
        errors.append(f"{len(notes)} notes")
    for i, (pitch, passing) in enumerate(notes):
        if not passing:
            if pitch % 12 not in mode.scale:
                errors.append(f"note {i} ({DEGREES[pitch % 12]}) is outside the mode")
        elif not (0 < i < len(notes) - 1 and abs(notes[i + 1][0] - notes[i - 1][0]) == 2
                  and min(notes[i - 1][0], notes[i + 1][0]) < pitch < max(notes[i - 1][0], notes[i + 1][0])):
            errors.append(f"passing note {i} is not between a whole step")
    for a, b in zip(notes, notes[1:]):
        if a[0] % 12 == b[0] % 12:
            # A degree twice running reads as a repeated note in play (Voicing places it nearest).
            errors.append(f"{DEGREES[a[0] % 12]} twice running")
            break
    return errors


def count(corpus, mode_name, phrases):
    unigram, first = {}, {}
    orders = {k: {} for k in range(1, 5)}
    notes = 0
    for phrase in phrases:
        degrees = [DEGREES[p % 12] for p, _ in phrase]
        notes += len(degrees)
        first[degrees[0]] = first.get(degrees[0], 0) + 1
        for d in degrees:
            unigram[d] = unigram.get(d, 0) + 1
        for k, table in orders.items():
            for i in range(k, len(degrees)):
                row = table.setdefault(" ".join(degrees[i - k:i]), {})
                row[degrees[i]] = row.get(degrees[i], 0) + 1
    ordered = lambda counts: {d: counts[d] for d in DEGREES if d in counts}
    return {
        "corpus": corpus,
        "mode": mode_name,
        "tunes": len(phrases),
        "notes": notes,
        "degree_labels": DEGREES,
        "unigram_counts": ordered(unigram),
        "unigram_prob": {d: round(c / notes, 5) for d, c in ordered(unigram).items()},
        "first_note_counts": ordered(first),
        "transitions": {
            f"order_{k}": {c: ordered(table[c]) for c in sorted(table, key=lambda c: [INDEX[d] for d in c.split()])}
            for k, table in orders.items()
        },
    }


def check_counts(mode, data):
    errors = []
    order_1 = data["transitions"]["order_1"]
    for source, targets in sorted(mode.pulls.items()):
        row = order_1.get(DEGREES[source], {})
        if not row:
            errors.append(f"{DEGREES[source]} is never followed")
            continue
        likeliest = max(row, key=row.get)
        wanted = {DEGREES[t] for t, _ in targets}
        if likeliest not in wanted:
            errors.append(f"after {DEGREES[source]} the likeliest note is {likeliest} ({row[likeliest]}), "
                          f"not {' or '.join(sorted(wanted))} ({', '.join(str(row.get(t, 0)) for t in sorted(wanted))})")
    if sum(data["unigram_counts"].values()) != data["notes"]:
        errors.append("unigram does not sum to notes")
    for k in range(1, 5):
        total = sum(sum(r.values()) for r in data["transitions"][f"order_{k}"].values())
        if total != data["notes"] - k * data["tunes"]:
            errors.append(f"order {k} sums to {total}")
    return errors


def write_if_changed(path, text):
    if os.path.exists(path):
        with open(path, encoding="utf-8", newline="") as f:
            if f.read() == text:
                return False
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    return True


def main(weights_path, output):
    with open(weights_path, encoding="utf-8") as f:
        weights = json.load(f)
    corpus = weights["corpus"]
    failed = False
    results = []
    for mode_name, spec in weights["modes"].items():
        mode = Mode(mode_name, spec)
        # One generator per mode, seeded from the seed and the mode's name, so a mode added or
        # removed leaves the others' phrases as they were.
        rng = random.Random(f"{weights['seed']}:{mode_name}")
        writer = Writer(mode, weights, rng)
        phrases = [writer.phrase() for _ in range(weights["phrases"])]
        errors = []
        for number, phrase in enumerate(phrases):
            for error in check_phrase(mode, phrase, weights["minimum_notes"]):
                errors.append(f"phrase {number}: {error}")
        data = count(corpus, mode_name, phrases)
        errors += check_counts(mode, data)
        for error in errors[:20]:
            print(f"{corpus}_{mode_name}: {error}")
        failed |= bool(errors)
        results.append((mode, data, writer, phrases))
        figures = ", ".join(f"{f} {writer.used[f]}" for f in FIGURES)
        print(f"{corpus}_{mode_name}: {data['tunes']} phrases, {data['notes']} notes "
              f"({data['notes'] / data['tunes']:.1f} a phrase), passing notes {writer.passing} "
              f"({100 * writer.passing / data['notes']:.1f}%), pulls after figures {writer.pulled}; {figures}")
        for source in sorted(mode.pulls):
            row = data["transitions"]["order_1"][DEGREES[source]]
            total = sum(row.values())
            top = sorted(row.items(), key=lambda kv: -kv[1])[:3]
            print(f"    after {DEGREES[source]}: " + ", ".join(f"{d} {100 * c / total:.0f}%" for d, c in top))
        for phrase in phrases[:3]:
            print("    " + " ".join(DEGREES[p % 12] + ("*" if passing else "") for p, passing in phrase))
    if failed:
        print("FAILED: nothing written")
        return 1
    os.makedirs(output, exist_ok=True)
    for mode, data, _, _ in results:
        name = f"{corpus}_{mode.name}"
        text = json.dumps(data, indent=1, ensure_ascii=False) + "\n"
        changed = write_if_changed(os.path.join(output, name + ".json"), text)
        print(f"{name}.json{' (written)' if changed else ' (unchanged)'}")
    print(f"{len(results)} modes, all checks held")
    return 0


if __name__ == "__main__":
    if len(sys.argv) > 3 or (len(sys.argv) > 1 and sys.argv[1] in ("-h", "--help")):
        print(__doc__)
        sys.exit(2)
    weights_path = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "virtuoso.json")
    output = sys.argv[2] if len(sys.argv) > 2 else os.path.join(HERE, "Output")
    sys.exit(main(weights_path, output))
