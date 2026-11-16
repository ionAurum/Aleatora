using System;
using System.Collections.Generic;

namespace Aurumworks.Aleatora
{
    /// <summary>How the board's degrees are laid out on its cells (the emitter plan, ruling 61).</summary>
    public enum CellLayout
    {
        /// <summary>The cells shared out among the degrees by their probabilities, then shuffled.</summary>
        Scattered,

        /// <summary>The same counts, each degree one connected region.</summary>
        Clustered,

        /// <summary>An isomorphic layout: each step along an axis moves the degree by that axis's interval.</summary>
        Axes,
    }

    /// <summary>
    /// The board's notes (the emitter plan, EM-1): a degree and an octave for every cell of a
    /// HexShape, by its index. The degrees come from a set's probabilities (its unigram, rulings 60
    /// and 61) in one of three layouts; the octave from the cell's distance from the centre cell
    /// (ruling 62). The same seed gives the same board (ruling 63). Plain C#: the tests run it whole.
    /// </summary>
    public sealed class CellNotes
    {
        /// <summary>The most octaves the board spans.</summary>
        public const int MostOctaves = 8;

        private readonly int[] _degrees;
        private readonly int[] _octaves;
        private readonly double[] _probabilities;
        private readonly int[] _counts;

        /// <summary>
        /// Lays the degrees out on the shape. Scattered and Clustered share the cells out by the
        /// probabilities; Axes puts 1 on the centre cell and moves kSteps scale steps a step along +K
        /// and nSteps along +N (+M is their sum), over the mode's degrees (mask). The octaves share
        /// the board's rings among octaves, the centre lowest unless outwardLower.
        /// </summary>
        public CellNotes(HexShape shape, IReadOnlyList<double> probabilities, int mask, CellLayout layout, ulong seed,
            int kSteps, int nSteps, int octaves, bool outwardLower)
        {
            Shape = shape ?? throw new ArgumentNullException(nameof(shape));
            if (probabilities == null || probabilities.Count != Degree.Count)
            {
                throw new ArgumentException("The probabilities are twelve, one a degree.", nameof(probabilities));
            }

            Layout = layout;
            Octaves = Math.Clamp(octaves, 1, MostOctaves);
            OutwardLower = outwardLower;
            Centre = CentreCell(shape);
            _probabilities = new double[Degree.Count];
            for (int d = 0; d < Degree.Count; d++)
            {
                _probabilities[d] = probabilities[d];
            }

            _degrees = new int[shape.Count];
            var rng = new Rng(seed);
            if (layout == CellLayout.Axes)
            {
                LayAlongAxes(mask, kSteps, nSteps);
            }
            else
            {
                int[] counts = ShareOut(_probabilities, shape.Count);
                if (layout == CellLayout.Clustered)
                {
                    Carve(counts, rng);
                }
                else
                {
                    Scatter(counts, rng);
                }
            }

            _counts = new int[Degree.Count];
            foreach (int degree in _degrees)
            {
                _counts[degree]++;
            }

            int farthest = 0;
            foreach (HexCoord cell in shape.Cells)
            {
                farthest = Math.Max(farthest, HexCoord.Distance(cell, Centre));
            }

            Rings = farthest + 1;
            _octaves = new int[shape.Count];
            for (int i = 0; i < shape.Count; i++)
            {
                _octaves[i] = OctaveOfRing(HexCoord.Distance(shape.Cells[i], Centre), Rings, Octaves, outwardLower);
            }
        }

        /// <summary>The board the notes are for.</summary>
        public HexShape Shape { get; }

        /// <summary>The layout the degrees were laid out in.</summary>
        public CellLayout Layout { get; }

        /// <summary>The cell nearest the board's middle: ring 0, and 1 in the Axes layout.</summary>
        public HexCoord Centre { get; }

        /// <summary>The board's rings around the centre cell: its farthest cell's distance, plus 1.</summary>
        public int Rings { get; }

        /// <summary>How many octaves the rings are shared among, 1 to MostOctaves.</summary>
        public int Octaves { get; }

        /// <summary>Whether the octaves fall outward (the centre highest) rather than rise.</summary>
        public bool OutwardLower { get; }

        /// <summary>The degree of the cell at an index of the shape, 0 to 11.</summary>
        public int DegreeAt(int index) => _degrees[index];

        /// <summary>The octave of the cell at an index, 0 (the lowest) to Octaves − 1.</summary>
        public int OctaveAt(int index) => _octaves[index];

        /// <summary>How many cells have the degree.</summary>
        public int CellsOf(int degree) => _counts[degree];

        /// <summary>A degree's probability, as given.</summary>
        public double Probability(int degree) => _probabilities[degree];

        /// <summary>
        /// How blue a degree's cells are, 0 (white) to 1 (blue): its probability over the largest
        /// probability of any degree but the root's (ruling 71). 0 for the root, which is red.
        /// </summary>
        public float Blue(int degree)
        {
            if (degree == 0)
            {
                return 0f;
            }

            double most = 0.0;
            for (int d = 1; d < Degree.Count; d++)
            {
                most = Math.Max(most, _probabilities[d]);
            }

            return most > 0.0 ? (float)Math.Clamp(_probabilities[degree] / most, 0.0, 1.0) : 0f;
        }

        /// <summary>
        /// The MIDI note of the cell at an index: the tonic's pitch class (0 C to 11 B) in the lowest
        /// octave (MIDI's: C4 is 60, so octave 3 starts at 48), up the cell's octaves, plus its degree.
        /// Kept within 0 to 127 by whole octaves.
        /// </summary>
        public int Note(int index, int tonic, int lowestOctave) =>
            NoteOf(tonic, lowestOctave, _octaves[index], _degrees[index]);

        //----------

        /// <summary>
        /// A set's probabilities from its unigram counts: each degree outside the mode's mask
        /// multiplied by chromaticWeight, then all divided by their sum. If nothing weighs anything,
        /// the mode's degrees as likely.
        /// </summary>
        public static double[] Probabilities(IReadOnlyList<int> counts, int mask, double chromaticWeight)
        {
            if (counts == null || counts.Count != Degree.Count)
            {
                throw new ArgumentException("The counts are twelve, one a degree.", nameof(counts));
            }

            double weight = double.IsNaN(chromaticWeight) ? 0.0 : Math.Max(0.0, chromaticWeight);
            var probabilities = new double[Degree.Count];
            double sum = 0.0;
            for (int d = 0; d < probabilities.Length; d++)
            {
                probabilities[d] = Math.Max(0, counts[d]) * (ModeScale.Has(mask, d) ? 1.0 : weight);
                sum += probabilities[d];
            }

            if (sum <= 0.0)
            {
                for (int d = 0; d < probabilities.Length; d++)
                {
                    probabilities[d] = ModeScale.Has(mask, d) ? 1.0 : 0.0;
                    sum += probabilities[d];
                }
            }

            if (sum <= 0.0)
            {
                probabilities[0] = sum = 1.0; // an empty mask: the root alone
            }

            for (int d = 0; d < probabilities.Length; d++)
            {
                probabilities[d] /= sum;
            }

            return probabilities;
        }

        /// <summary>
        /// The cells shared out among the degrees in proportion to the probabilities, by largest
        /// remainder: each degree its whole share, then the cells left to the largest fractions
        /// (the larger probability, then the lower degree, on a tie). The counts sum to cells.
        /// </summary>
        public static int[] ShareOut(IReadOnlyList<double> probabilities, int cells)
        {
            if (probabilities == null || probabilities.Count != Degree.Count)
            {
                throw new ArgumentException("The probabilities are twelve, one a degree.", nameof(probabilities));
            }

            double sum = 0.0;
            for (int d = 0; d < probabilities.Count; d++)
            {
                sum += Math.Max(0.0, probabilities[d]);
            }

            if (sum <= 0.0 || cells < 0)
            {
                throw new ArgumentException("The probabilities weigh nothing, or the cells are fewer than 0.", nameof(probabilities));
            }

            var counts = new int[Degree.Count];
            var fractions = new double[Degree.Count];
            var order = new int[Degree.Count];
            int given = 0;
            for (int d = 0; d < counts.Length; d++)
            {
                double quota = cells * Math.Max(0.0, probabilities[d]) / sum;
                counts[d] = (int)Math.Floor(quota);
                fractions[d] = quota - counts[d];
                given += counts[d];
                order[d] = d;
            }

            Array.Sort(order, (a, b) =>
            {
                int byFraction = fractions[b].CompareTo(fractions[a]);
                if (byFraction != 0)
                {
                    return byFraction;
                }

                int byProbability = probabilities[b].CompareTo(probabilities[a]);
                return byProbability != 0 ? byProbability : a.CompareTo(b);
            });

            for (int i = 0; given < cells; i = (i + 1) % order.Length)
            {
                if (probabilities[order[i]] > 0.0)
                {
                    counts[order[i]]++;
                    given++;
                }
            }

            return counts;
        }

        /// <summary>The cell nearest the board's middle (the mean of its cells), rounded; on the board.</summary>
        public static HexCoord CentreCell(HexShape shape)
        {
            HexCoord rounded = HexCoord.Round(shape.CentreQ, shape.CentreR);
            if (shape.Contains(rounded))
            {
                return rounded;
            }

            HexCoord nearest = shape.Cells[0];
            foreach (HexCoord cell in shape.Cells)
            {
                if (HexCoord.Distance(cell, rounded) < HexCoord.Distance(nearest, rounded))
                {
                    nearest = cell;
                }
            }

            return nearest;
        }

        /// <summary>
        /// A ring's octave: the rings shared evenly among the octaves, ring r of R in octave
        /// ⌊r · octaves / R⌋, counted from the centre upward, or turned round.
        /// </summary>
        public static int OctaveOfRing(int ring, int rings, int octaves, bool outwardLower)
        {
            int octave = Math.Clamp(rings > 0 ? ring * octaves / rings : 0, 0, octaves - 1);
            return outwardLower ? octaves - 1 - octave : octave;
        }

        /// <summary>A note from its parts (see Note), within 0 to 127 by whole octaves.</summary>
        public static int NoteOf(int tonic, int lowestOctave, int octave, int degree)
        {
            int pitchClass = ((tonic % Degree.Count) + Degree.Count) % Degree.Count;
            int note = ((lowestOctave + 1 + octave) * Degree.Count) + pitchClass + degree;
            while (note > 127)
            {
                note -= Degree.Count;
            }

            while (note < 0)
            {
                note += Degree.Count;
            }

            return note;
        }

        //----------

        // The counts' degrees in order, shuffled (Fisher–Yates), one a cell.
        private void Scatter(int[] counts, Rng rng)
        {
            int index = 0;
            for (int d = 0; d < counts.Length; d++)
            {
                for (int c = 0; c < counts[d]; c++)
                {
                    _degrees[index++] = d;
                }
            }

            for (int i = _degrees.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (_degrees[i], _degrees[j]) = (_degrees[j], _degrees[i]);
            }
        }

        // 1 on the centre; kSteps scale steps a step along +K (+1, 0), nSteps along +N (0, −1).
        private void LayAlongAxes(int mask, int kSteps, int nSteps)
        {
            var scale = new List<int>(Degree.Count);
            for (int d = 0; d < Degree.Count; d++)
            {
                if (ModeScale.Has(mask, d))
                {
                    scale.Add(d);
                }
            }

            if (scale.Count == 0)
            {
                scale.Add(0);
            }

            for (int i = 0; i < _degrees.Length; i++)
            {
                HexCoord cell = Shape.Cells[i];
                long steps = ((long)(cell.Q - Centre.Q) * kSteps) - ((long)(cell.R - Centre.R) * nSteps);
                int place = (int)(((steps % scale.Count) + scale.Count) % scale.Count);
                _degrees[i] = scale[place];
            }
        }

        // Clustered: the regions carved one at a time, in an order drawn from the seed. Each starts
        // on a free cell with the fewest free neighbours (the edge of what is left) and grows into
        // the free neighbour with the most of its own, the ties drawn from the seed; a cell is taken
        // only if the cells still free stay connected, so the last region, all that is left, is
        // whole too. A region that can't grow that way (rare) takes a cell anyway and may come in
        // two pieces; the counts always hold.
        private void Carve(int[] counts, Rng rng)
        {
            int n = _degrees.Length;
            int[][] neighbours = Neighbours(Shape);
            var free = new bool[n];
            for (int i = 0; i < n; i++)
            {
                free[i] = true;
                _degrees[i] = -1;
            }

            var order = new List<int>();
            for (int d = 0; d < counts.Length; d++)
            {
                if (counts[d] > 0)
                {
                    order.Add(d);
                }
            }

            Shuffle(order, rng);
            var candidates = new List<int>(n);
            var ranks = new int[n];
            var marked = new bool[n];
            var region = new List<int>(n);
            var work = new Stack<int>(n);
            var seen = new bool[n];
            for (int r = 0; r < order.Count; r++)
            {
                int degree = order[r];
                if (r == order.Count - 1)
                {
                    for (int i = 0; i < n; i++)
                    {
                        if (free[i])
                        {
                            _degrees[i] = degree;
                        }
                    }

                    break;
                }

                // The start: the edge of what is left.
                candidates.Clear();
                for (int i = 0; i < n; i++)
                {
                    if (free[i])
                    {
                        candidates.Add(i);
                    }
                }

                SortByKey(candidates, rng, ranks, cell => CountAround(neighbours[cell], free, null, -1));
                int start = FirstKeepingConnected(candidates, neighbours, free, work, seen);
                region.Clear();
                Take(start, degree, free, region);

                while (region.Count < counts[degree])
                {
                    candidates.Clear();
                    foreach (int cell in region)
                    {
                        foreach (int next in neighbours[cell])
                        {
                            if (next >= 0 && free[next] && !marked[next])
                            {
                                marked[next] = true;
                                candidates.Add(next);
                            }
                        }
                    }

                    foreach (int cell in candidates)
                    {
                        marked[cell] = false;
                    }

                    if (candidates.Count == 0)
                    {
                        // Boxed in: the free cells nearest the region, as candidates.
                        int nearest = int.MaxValue;
                        for (int i = 0; i < n; i++)
                        {
                            if (!free[i])
                            {
                                continue;
                            }

                            int distance = int.MaxValue;
                            foreach (int cell in region)
                            {
                                distance = Math.Min(distance, HexCoord.Distance(Shape.Cells[i], Shape.Cells[cell]));
                            }

                            if (distance < nearest)
                            {
                                nearest = distance;
                                candidates.Clear();
                            }

                            if (distance == nearest)
                            {
                                candidates.Add(i);
                            }
                        }
                    }

                    SortByKey(candidates, rng, ranks, cell => -CountAround(neighbours[cell], null, _degrees, degree));
                    Take(FirstKeepingConnected(candidates, neighbours, free, work, seen), degree, free, region);
                }
            }
        }

        private void Take(int cell, int degree, bool[] free, List<int> region)
        {
            free[cell] = false;
            _degrees[cell] = degree;
            region.Add(cell);
        }

        // The first candidate whose taking leaves the free cells connected; the first if none does.
        private static int FirstKeepingConnected(List<int> candidates, int[][] neighbours, bool[] free, Stack<int> work, bool[] seen)
        {
            foreach (int cell in candidates)
            {
                if (KeepsConnected(cell, neighbours, free, work, seen))
                {
                    return cell;
                }
            }

            return candidates[0];
        }

        // Whether the free cells but this one are connected. Its free neighbours in one unbroken arc
        // around it settles it at once; otherwise a search from one of them.
        private static bool KeepsConnected(int cell, int[][] neighbours, bool[] free, Stack<int> work, bool[] seen)
        {
            int[] around = neighbours[cell];
            int arcs = 0;
            for (int i = 0; i < around.Length; i++)
            {
                bool here = around[i] >= 0 && free[around[i]];
                int before = around[(i + around.Length - 1) % around.Length];
                bool previous = before >= 0 && free[before];
                if (here && !previous)
                {
                    arcs++;
                }
            }

            if (arcs <= 1)
            {
                return true; // none free around it, all six, or one unbroken arc
            }

            int total = 0;
            int first = -1;
            for (int i = 0; i < free.Length; i++)
            {
                if (free[i] && i != cell)
                {
                    total++;
                    first = first < 0 ? i : first;
                }
            }

            if (total == 0)
            {
                return true;
            }

            Array.Clear(seen, 0, seen.Length);
            work.Clear();
            work.Push(first);
            seen[first] = true;
            int reached = 1;
            while (work.Count > 0)
            {
                foreach (int next in neighbours[work.Pop()])
                {
                    if (next >= 0 && next != cell && free[next] && !seen[next])
                    {
                        seen[next] = true;
                        reached++;
                        work.Push(next);
                    }
                }
            }

            return reached == total;
        }

        // How many of the cells around are free (free given), or have the degree (degrees given).
        private static int CountAround(int[] around, bool[] free, int[] degrees, int degree)
        {
            int count = 0;
            foreach (int next in around)
            {
                if (next >= 0 && (free != null ? free[next] : degrees[next] == degree))
                {
                    count++;
                }
            }

            return count;
        }

        // Shuffled from the seed, then ordered by the key, the shuffle breaking ties (a stable order).
        private static void SortByKey(List<int> cells, Rng rng, int[] ranks, Func<int, int> key)
        {
            Shuffle(cells, rng);
            for (int i = 0; i < cells.Count; i++)
            {
                ranks[cells[i]] = i;
            }

            cells.Sort((a, b) =>
            {
                int byKey = key(a).CompareTo(key(b));
                return byKey != 0 ? byKey : ranks[a].CompareTo(ranks[b]);
            });
        }

        private static void Shuffle(List<int> items, Rng rng)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        // Each cell's six neighbours' indices, in turning order (−1 off the board).
        private static int[][] Neighbours(HexShape shape)
        {
            var neighbours = new int[shape.Count][];
            for (int i = 0; i < shape.Count; i++)
            {
                neighbours[i] = new int[HexDirections.Count];
                for (int d = 0; d < HexDirections.Count; d++)
                {
                    neighbours[i][d] = shape.IndexOf(shape.Cells[i].Neighbour((HexDirection)d));
                }
            }

            return neighbours;
        }
    }
}
