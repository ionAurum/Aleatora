namespace Aurumworks.Aleatora
{
    /// <summary>
    /// One note MelodyGenerator chose, and what it chose it from: the degree, the context's order it
    /// asked for and the one it used after backing off, and how many notes the table heard after
    /// that context. The twelve probabilities it drew from go into the caller's span, not here, so a
    /// step allocates nothing.
    /// </summary>
    public readonly struct MelodyStep
    {
        /// <summary>The step's values, as the generator found them.</summary>
        public MelodyStep(int degree, bool isFirst, int orderUsed, int orderAsked, int contextTotal)
        {
            Degree = degree;
            IsFirst = isFirst;
            OrderUsed = orderUsed;
            OrderAsked = orderAsked;
            ContextTotal = contextTotal;
        }

        /// <summary>The degree chosen, 0 to 11.</summary>
        public int Degree { get; }

        /// <summary>
        /// Whether this is a melody's first note, drawn from how melodies begin (the table's First)
        /// rather than from a context. Its OrderUsed is 0 and its ContextTotal the set's melodies.
        /// </summary>
        public bool IsFirst { get; }

        /// <summary>The order of the context the note was drawn from, after backing off: 0 is the unigram.</summary>
        public int OrderUsed { get; }

        /// <summary>
        /// The order the generator looked for: the settings' Order, capped by the table's MaxOrder
        /// and by how many notes the melody has so far.
        /// </summary>
        public int OrderAsked { get; }

        /// <summary>How many notes the table heard after the context used (the sum of its counts): the evidence behind the choice.</summary>
        public int ContextTotal { get; }
    }
}
