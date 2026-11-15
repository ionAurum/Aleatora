namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The degrees each mode has, as a mask of twelve bits (bit d is set when degree d is in the
    /// mode). The view colours a mode's own degrees with it, and the generator weighs the others by
    /// its ChromaticWeight. The modes are the tables' (Tools/Markov/Source/README.md); a mode this
    /// doesn't know has all twelve.
    /// </summary>
    public static class ModeScale
    {
        /// <summary>All twelve degrees: the mask of a mode this doesn't know.</summary>
        public const int All = (1 << Degree.Count) - 1;

        // Degrees by semitone: 1 = 0, ♭2 = 1, 2 = 2, ♭3 = 3, 3 = 4, 4 = 5, ♯4 = 6, 5 = 7, ♭6 = 8, 6 = 9, ♭7 = 10, 7 = 11.
        private static readonly int _ionian = Of(0, 2, 4, 5, 7, 9, 11);
        private static readonly int _aeolian = Of(0, 2, 3, 5, 7, 8, 10);
        private static readonly int _dorian = Of(0, 2, 3, 5, 7, 9, 10);
        private static readonly int _mixolydian = Of(0, 2, 4, 5, 7, 9, 10);
        private static readonly int _harmonicMinor = Of(0, 2, 3, 5, 7, 8, 11);
        private static readonly int _gong = Of(0, 2, 4, 7, 9);
        private static readonly int _zhi = Of(0, 2, 5, 7, 9);
        private static readonly int _yu = Of(0, 3, 5, 7, 10);

        /// <summary>The mode's degrees as a mask, by the name the tables give it ("ionian", "harmonic_minor", "gong", …).</summary>
        public static int Mask(string mode)
        {
            switch (mode)
            {
                case "ionian": return _ionian;
                case "aeolian": return _aeolian;
                case "dorian": return _dorian;
                case "mixolydian": return _mixolydian;
                case "harmonic_minor": return _harmonicMinor;
                case "gong": return _gong;
                case "zhi": return _zhi;
                case "yu": return _yu;
                default: return All;
            }
        }

        /// <summary>Whether a mask has this degree. A number that is no degree is in no mask.</summary>
        public static bool Has(int mask, int degree) => Degree.IsValid(degree) && (mask & (1 << degree)) != 0;

        /// <summary>Whether the mode has this degree.</summary>
        public static bool Contains(string mode, int degree) => Has(Mask(mode), degree);

        private static int Of(params int[] degrees)
        {
            int mask = 0;
            foreach (int degree in degrees)
            {
                mask |= 1 << degree;
            }

            return mask;
        }
    }
}
