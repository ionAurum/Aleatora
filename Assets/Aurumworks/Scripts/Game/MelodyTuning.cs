using System;
using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The melodies' tuning, a section of AleatoraTuning: which set they come from, how the
    /// generator chooses each note (GeneratorSettings), how the degrees are placed as MIDI notes
    /// (Voicing) and the seed. The defaults are the plan's (M2).
    /// </summary>
    [Serializable]
    public class MelodyTuning
    {
        [Header("Set")]
        [Tooltip("The melody table the melodies come from, by its name in Resources/Markov/Index.txt: european_ionian, irish_dorian, chinese_han_gong, ….")]
        [SerializeField] private string _set = "european_ionian";

        [Header("Generator")]
        [Tooltip("The longest run of notes before the next that the generator looks at: 0 takes each note on its own, 4 the four before it. It backs off to shorter runs the data heard too seldom.")]
        [SerializeField, Range(0, GeneratorSettings.HighestOrder)] private int _order = 2;
        [Tooltip("1 draws as the data has it. Toward 0 the likeliest note nearly always; above 1 flatter, all of a context's notes toward as likely.")]
        [SerializeField, Min(GeneratorSettings.LowestTemperature)] private float _temperature = 1f;
        [Tooltip("Added to each of the twelve counts of the run used. 0 never plays a step the data hasn't heard; above 0 any note can follow any run.")]
        [SerializeField, Min(0f)] private float _smoothing;
        [Tooltip("A run of notes the data heard followed fewer times than this is passed over for the next shorter one.")]
        [SerializeField, Min(0)] private int _minimumContext = 3;
        [Tooltip("Multiplies the weight of each degree outside the set's mode. 0 keeps a melody in the mode; 1 as the data has it.")]
        [SerializeField, Min(0f)] private float _chromaticWeight = 1f;

        [Header("Voicing")]
        [Tooltip("The tonic, as a MIDI note number (60 is middle C, C4): the key, and where a melody's first note is placed nearest to.")]
        [SerializeField, Range(0, Voicing.HighestNote)] private int _tonic = 60;
        [Tooltip("The lowest note a melody plays, as a MIDI note number. Low to High spans at least eleven semitones; a narrower range is widened.")]
        [SerializeField, Range(0, Voicing.HighestNote)] private int _low = 55;
        [Tooltip("The highest note a melody plays, as a MIDI note number.")]
        [SerializeField, Range(0, Voicing.HighestNote)] private int _high = 79;

        [Header("Seed")]
        [Tooltip("Where the random draws start: one seed is one melody, in the Editor and in a player alike. 2112, for Rush.")]
        [SerializeField] private long _seed = 2112;

        /// <summary>The set's name, as the index lists it.</summary>
        public string Set
        {
            get => _set;
            set => _set = value;
        }

        /// <summary>The generator's settings, clamped into range.</summary>
        public GeneratorSettings Generator
        {
            get => new GeneratorSettings
            {
                Order = _order,
                Temperature = _temperature,
                Smoothing = _smoothing,
                MinimumContext = _minimumContext,
                ChromaticWeight = _chromaticWeight,
            }.Clamped();
            set
            {
                _order = value.Order;
                _temperature = value.Temperature;
                _smoothing = value.Smoothing;
                _minimumContext = value.MinimumContext;
                _chromaticWeight = value.ChromaticWeight;
            }
        }

        /// <summary>The tonic, as a MIDI note number.</summary>
        public int Tonic
        {
            get => _tonic;
            set => _tonic = value;
        }

        /// <summary>The lowest note, as a MIDI note number (Voicing widens a range narrower than eleven semitones).</summary>
        public int Low => _low;

        /// <summary>The highest note, as a MIDI note number.</summary>
        public int High => _high;

        /// <summary>The seed, as the generator takes it.</summary>
        public ulong Seed
        {
            get => unchecked((ulong)_seed);
            set => _seed = unchecked((long)value);
        }

        /// <summary>
        /// Moves the tonic, Low and High by octaves together (−1 down, 1 up; the octave buttons and
        /// keys), so a melody is placed the same an octave away. It stops at MIDI's edges: if any of
        /// the three would leave 0 to 127, nothing moves. True if it moved.
        /// </summary>
        public bool ShiftOctave(int octaves)
        {
            int by = octaves * Degree.Count;
            if (octaves == 0 || !InNotes(_tonic + by) || !InNotes(_low + by) || !InNotes(_high + by))
            {
                return false;
            }

            _tonic += by;
            _low += by;
            _high += by;
            return true;
        }

        private static bool InNotes(int note) => note >= 0 && note <= Voicing.HighestNote;

        /// <summary>A copy to change freely: the Melodies page works on one, never on the asset.</summary>
        public MelodyTuning Copy() => (MelodyTuning)MemberwiseClone();

        /// <summary>A voicing with this tuning's tonic and range.</summary>
        public Voicing MakeVoicing() => new Voicing(_tonic, _low, _high);
    }
}
