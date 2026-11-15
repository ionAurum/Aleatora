using System;
using System.Collections.Generic;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// One set's melody statistics, as MarkovTableReader read them: how often each degree is played
    /// and begins a melody, and for each order k from 1 to MaxOrder, how often each degree follows
    /// each run of k degrees (a context). It never changes once read. A context is packed base 12
    /// into one whole number (its oldest degree the most significant), which finds its row of twelve
    /// counts in one flat array, so a lookup makes no string and allocates nothing.
    /// </summary>
    public sealed class MarkovTable
    {
        /// <summary>The highest order a table can hold: 12⁸ contexts still pack into an int.</summary>
        public const int HighestOrder = 8;

        private readonly int[] _unigram;
        private readonly int[] _first;
        private readonly Dictionary<int, int>[] _offsets; // per order (order 1 first): packed context to its row's offset in _counts
        private readonly int[][] _contexts; // per order: the packed contexts, in the file's order
        private readonly int[] _counts;

        /// <summary>The reader's: takes the arrays as its own, checked already.</summary>
        internal MarkovTable(string name, string corpus, string mode, int tunes, int notes, int[] unigram, int[] first,
            Dictionary<int, int>[] offsets, int[][] contexts, int[] counts)
        {
            Name = name;
            Corpus = corpus;
            Mode = mode;
            Tunes = tunes;
            Notes = notes;
            _unigram = unigram;
            _first = first;
            _offsets = offsets;
            _contexts = contexts;
            _counts = counts;
        }

        /// <summary>The set's name, which is its file's: "european_ionian".</summary>
        public string Name { get; }

        /// <summary>The collection the melodies came from: "european", "german", "irish", "chinese_han".</summary>
        public string Corpus { get; }

        /// <summary>The mode, as ModeScale knows it: "ionian", "gong", ….</summary>
        public string Mode { get; }

        /// <summary>How many melodies the set was counted from.</summary>
        public int Tunes { get; }

        /// <summary>How many notes are in those melodies.</summary>
        public int Notes { get; }

        /// <summary>The longest context the table holds.</summary>
        public int MaxOrder => _offsets.Length;

        /// <summary>How often this degree is played.</summary>
        public int Unigram(int degree) => _unigram[degree];

        /// <summary>How often this degree begins a melody.</summary>
        public int First(int degree) => _first[degree];

        /// <summary>How many contexts the table has of this order (1 for order 0, none above MaxOrder).</summary>
        public int ContextCount(int order)
        {
            if (order == 0)
            {
                return 1;
            }

            return order > 0 && order <= MaxOrder ? _contexts[order - 1].Length : 0;
        }

        /// <summary>
        /// The counts of each degree after this context: twelve, in the degrees' order. The context's
        /// length is its order and its last element is the most recent note. An empty context is
        /// order 0, whose counts are the unigram. False (and no counts) if the table never heard the
        /// context, if it is longer than MaxOrder, or if something in it is no degree.
        /// </summary>
        public bool TryGetCounts(ReadOnlySpan<int> context, out ReadOnlySpan<int> counts)
        {
            if (context.Length == 0)
            {
                counts = _unigram;
                return true;
            }

            if (context.Length <= MaxOrder && TryPack(context, out int packed)
                && _offsets[context.Length - 1].TryGetValue(packed, out int offset))
            {
                counts = new ReadOnlySpan<int>(_counts, offset, Degree.Count);
                return true;
            }

            counts = default;
            return false;
        }

        /// <summary>How many notes the table heard after this context: the sum of its counts, 0 if it never heard it.</summary>
        public int Total(ReadOnlySpan<int> context)
        {
            if (!TryGetCounts(context, out ReadOnlySpan<int> counts))
            {
                return 0;
            }

            int total = 0;
            foreach (int count in counts)
            {
                total += count;
            }

            return total;
        }

        /// <summary>
        /// Every context of this order, in the file's order (sorted by degree, oldest first), each a
        /// new array of the order's length. Order 0 has the one empty context; an order the table
        /// doesn't hold has none. For the view: it allocates.
        /// </summary>
        public IEnumerable<int[]> Contexts(int order)
        {
            if (order == 0)
            {
                yield return Array.Empty<int>();
                yield break;
            }

            if (order < 0 || order > MaxOrder)
            {
                yield break;
            }

            foreach (int packed in _contexts[order - 1])
            {
                yield return Unpack(packed, order);
            }
        }

        /// <summary>Packs a context base 12, its oldest degree the most significant. False if it is too long or holds no degree.</summary>
        internal static bool TryPack(ReadOnlySpan<int> context, out int packed)
        {
            packed = 0;
            if (context.Length > HighestOrder)
            {
                return false;
            }

            foreach (int degree in context)
            {
                if (!Degree.IsValid(degree))
                {
                    return false;
                }

                packed = (packed * Degree.Count) + degree;
            }

            return true;
        }

        private static int[] Unpack(int packed, int order)
        {
            int[] context = new int[order];
            for (int i = order - 1; i >= 0; i--)
            {
                context[i] = packed % Degree.Count;
                packed /= Degree.Count;
            }

            return context;
        }
    }
}
