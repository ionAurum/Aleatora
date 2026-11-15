using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Makes a melody one degree at a time from a MarkovTable. The first note is drawn from how the
    /// set's melodies begin; each next from the longest context of the melody so far, up to the
    /// settings' Order, that the table heard at least MinimumContext times, backing off one order at
    /// a time down to the unigram (order 0). A count's weight is (count + Smoothing)^(1 / Temperature),
    /// times ChromaticWeight for a degree outside the mode; a context whose weights are all 0 is
    /// backed off from too. Every draw comes from its own Rng, so one seed is one melody, and a step
    /// allocates nothing.
    /// </summary>
    public sealed class MelodyGenerator
    {
        private readonly Rng _rng;
        private readonly int _modeMask;
        private readonly int[] _first = new int[Degree.Count];
        private readonly int[] _history; // oldest first; _count of them in use
        private readonly double[] _weights = new double[Degree.Count];
        private int _count;
        private GeneratorSettings _settings;

        /// <summary>A generator on this table, with these settings, its draws from this seed.</summary>
        public MelodyGenerator(MarkovTable table, GeneratorSettings settings, ulong seed)
        {
            Table = table ?? throw new ArgumentNullException(nameof(table));
            _settings = settings.Clamped();
            _rng = new Rng(seed);
            _modeMask = ModeScale.Mask(table.Mode);
            _history = new int[Math.Max(table.MaxOrder, 1)];
            for (int degree = 0; degree < Degree.Count; degree++)
            {
                _first[degree] = table.First(degree);
            }
        }

        /// <summary>The table it draws from.</summary>
        public MarkovTable Table { get; }

        /// <summary>The settings, clamped into range. A change applies from the next step: the melody so far stays.</summary>
        public GeneratorSettings Settings
        {
            get => _settings;
            set => _settings = value.Clamped();
        }

        /// <summary>The seed it was last started from.</summary>
        public ulong Seed => _rng.Seed;

        /// <summary>The longest history it keeps: the table's MaxOrder (at least 1).</summary>
        public int MaxOrder => _history.Length;

        /// <summary>The last MaxOrder degrees chosen, oldest first; empty at the start of a melody.</summary>
        public ReadOnlySpan<int> History => new ReadOnlySpan<int>(_history, 0, _count);

        /// <summary>Starts a new melody from this seed: the same steps follow as from a new generator with it.</summary>
        public void Reset(ulong seed)
        {
            _rng.Reset(seed);
            _count = 0;
        }

        /// <summary>
        /// Sets the melody so far, as if these degrees had been chosen (oldest first; only the last
        /// MaxOrder are kept). Empty starts a new melody without touching the draws. For the view and
        /// the tests, to ask what follows a given run of notes.
        /// </summary>
        public void SetHistory(ReadOnlySpan<int> degrees)
        {
            foreach (int degree in degrees)
            {
                if (!Degree.IsValid(degree))
                {
                    throw new ArgumentException($"A degree is 0 to 11: {degree}.", nameof(degrees));
                }
            }

            _count = 0;
            foreach (int degree in degrees)
            {
                Push(degree);
            }
        }

        /// <summary>
        /// Chooses the next degree. The twelve probabilities it drew from (summing to 1) are written
        /// into distribution, which must hold at least twelve.
        /// </summary>
        public MelodyStep Next(Span<float> distribution)
        {
            if (distribution.Length < Degree.Count)
            {
                throw new ArgumentException($"The distribution needs room for {Degree.Count} probabilities: {distribution.Length}.",
                    nameof(distribution));
            }

            GeneratorSettings settings = _settings;
            double sum;
            int degree;

            if (_count == 0)
            {
                sum = Weigh(_first, settings);
                if (sum > 0d)
                {
                    degree = Draw(sum, distribution);
                    return new MelodyStep(degree, true, 0, 0, Table.Tunes);
                }
            }

            int asked = Math.Min(Math.Min(settings.Order, Table.MaxOrder), _count);
            for (int order = asked; order >= 0; order--)
            {
                ReadOnlySpan<int> context = new ReadOnlySpan<int>(_history, _count - order, order);
                if (!Table.TryGetCounts(context, out ReadOnlySpan<int> counts))
                {
                    continue;
                }

                int total = 0;
                foreach (int count in counts)
                {
                    total += count;
                }

                if (order > 0 && total < settings.MinimumContext)
                {
                    continue;
                }

                sum = Weigh(counts, settings);
                if (sum > 0d)
                {
                    degree = Draw(sum, distribution);
                    return new MelodyStep(degree, false, order, asked, total);
                }
            }

            // Nothing weighed anything: only with ChromaticWeight 0 on a table whose unigram has no
            // note in its mode, which no shipped set is. Every degree of the mode as likely, else all twelve.
            sum = 0d;
            for (int d = 0; d < Degree.Count; d++)
            {
                _weights[d] = ModeScale.Has(_modeMask, d) ? 1d : 0d;
                sum += _weights[d];
            }

            if (sum <= 0d)
            {
                for (int d = 0; d < Degree.Count; d++)
                {
                    _weights[d] = 1d;
                }

                sum = Degree.Count;
            }

            degree = Draw(sum, distribution);
            return new MelodyStep(degree, _count == 0, 0, asked, 0);
        }

        // Fills _weights from twelve counts and returns their sum (0 when none weighs anything). The
        // counts are divided by the largest first, so a low temperature's high power can't overflow.
        private double Weigh(ReadOnlySpan<int> counts, GeneratorSettings settings)
        {
            double smoothing = settings.Smoothing;
            double largest = 0d;
            for (int d = 0; d < Degree.Count; d++)
            {
                largest = Math.Max(largest, counts[d] + smoothing);
            }

            if (largest <= 0d)
            {
                return 0d;
            }

            double power = 1d / settings.Temperature;
            double sum = 0d;
            for (int d = 0; d < Degree.Count; d++)
            {
                double share = (counts[d] + smoothing) / largest;
                double weight = share > 0d ? (power == 1d ? share : Math.Pow(share, power)) : 0d;
                if (!ModeScale.Has(_modeMask, d))
                {
                    weight *= settings.ChromaticWeight;
                }

                _weights[d] = weight;
                sum += weight;
            }

            return double.IsFinite(sum) ? sum : 0d;
        }

        // Draws a degree from _weights (one Rng.NextDouble, whatever is drawn), writes the
        // probabilities into distribution and adds the degree to the history.
        private int Draw(double sum, Span<float> distribution)
        {
            double target = _rng.NextDouble() * sum;
            int chosen = -1;
            double running = 0d;
            for (int d = 0; d < Degree.Count; d++)
            {
                double weight = _weights[d];
                distribution[d] = (float)(weight / sum);
                if (weight <= 0d)
                {
                    continue;
                }

                running += weight;
                if (chosen < 0 && target < running)
                {
                    chosen = d;
                }
            }

            if (chosen < 0)
            {
                // Rounding left the target past the last weight: the last degree that weighs anything.
                for (int d = Degree.Count - 1; d >= 0 && chosen < 0; d--)
                {
                    if (_weights[d] > 0d)
                    {
                        chosen = d;
                    }
                }
            }

            Push(chosen);
            return chosen;
        }

        private void Push(int degree)
        {
            if (_count == _history.Length)
            {
                Array.Copy(_history, 1, _history, 0, _count - 1);
                _count--;
            }

            _history[_count++] = degree;
        }
    }
}
