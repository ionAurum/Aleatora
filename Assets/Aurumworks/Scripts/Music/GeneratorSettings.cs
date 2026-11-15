using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// How MelodyGenerator chooses each note: the longest context it looks at, how sharply it
    /// favours the likeliest notes, how far it strays from what the data heard, and from the mode.
    /// The tuning asset's Melodies section (MelodyTuning) makes one from its Inspector fields. The
    /// generator reads Clamped(), so a value out of range never reaches it.
    /// </summary>
    [Serializable]
    public struct GeneratorSettings
    {
        /// <summary>The longest context the generator looks at, 0 (the unigram) to this.</summary>
        public const int HighestOrder = 4;

        /// <summary>The lowest temperature the generator uses: at it the likeliest note is already all but certain.</summary>
        public const float LowestTemperature = 0.01f;

        /// <summary>The longest context to look at: 0 (each note on its own) to 4. The table's own MaxOrder caps it too.</summary>
        public int Order;

        /// <summary>
        /// Above 0. 1 draws as the data has it; toward 0 the likeliest note nearly always; above 1
        /// flatter, toward all of a context's notes as likely.
        /// </summary>
        public float Temperature;

        /// <summary>Added to each of the twelve counts of the context used (add-k). 0 never plays a transition the data hasn't heard.</summary>
        public float Smoothing;

        /// <summary>A context with fewer notes than this behind it is passed over for the next shorter one.</summary>
        public int MinimumContext;

        /// <summary>Multiplies the weight of each degree outside the mode. 0 keeps a melody in the mode; 1 as the data has it.</summary>
        public float ChromaticWeight;

        /// <summary>The plan's defaults: order 2, temperature 1, no smoothing, a context of at least 3 notes, chromatic as heard.</summary>
        public static GeneratorSettings Default => new GeneratorSettings
        {
            Order = 2,
            Temperature = 1f,
            Smoothing = 0f,
            MinimumContext = 3,
            ChromaticWeight = 1f,
        };

        /// <summary>These settings with each value brought into its range (a NaN becomes the default).</summary>
        public GeneratorSettings Clamped()
        {
            GeneratorSettings d = Default;
            return new GeneratorSettings
            {
                Order = Math.Clamp(Order, 0, HighestOrder),
                Temperature = float.IsNaN(Temperature) ? d.Temperature : Math.Max(Temperature, LowestTemperature),
                Smoothing = float.IsNaN(Smoothing) ? d.Smoothing : Math.Max(Smoothing, 0f),
                MinimumContext = Math.Max(MinimumContext, 0),
                ChromaticWeight = float.IsNaN(ChromaticWeight) ? d.ChromaticWeight : Math.Max(ChromaticWeight, 0f),
            };
        }
    }
}
