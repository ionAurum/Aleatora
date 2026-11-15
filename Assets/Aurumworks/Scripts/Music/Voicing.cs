using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Places degrees as MIDI note numbers, ready for MIDI output (the plan's decision 6: the tables
    /// hold pitch classes only). The first note of a melody is the degree's placement nearest the
    /// Tonic; each next the placement nearest the note before, a tie (a tritone either way) going
    /// toward the middle of the range; a placement outside Low to High moves an octave inward until
    /// it is inside. Low to High always spans at least eleven semitones, so every degree has a
    /// placement in it.
    /// </summary>
    public sealed class Voicing
    {
        /// <summary>The highest MIDI note number.</summary>
        public const int HighestNote = 127;

        /// <summary>The narrowest range: eleven semitones, so each of the twelve degrees has a placement in it.</summary>
        public const int NarrowestRange = Degree.Count - 1;

        private int _previous = -1;

        /// <summary>Placements around this tonic (MIDI), between Low and High (MIDI, both included); see Set for how they are brought into range.</summary>
        public Voicing(int tonic, int low, int high)
        {
            Set(tonic, low, high);
        }

        /// <summary>The tonic's MIDI note: degree 1's pitch, and where a melody starts from.</summary>
        public int Tonic { get; private set; }

        /// <summary>The lowest note placed (MIDI).</summary>
        public int Low { get; private set; }

        /// <summary>The highest note placed (MIDI).</summary>
        public int High { get; private set; }

        /// <summary>The last note placed, or −1 at the start of a melody.</summary>
        public int Previous => _previous;

        /// <summary>
        /// Sets the tonic and range, from the next note on. Each is kept within 0 to 127; Low and High
        /// are swapped if they are the wrong way round, and High is raised (or, at the top, Low lowered)
        /// until the range spans NarrowestRange. A tonic outside the range is fine: it only sets the
        /// key and where the first note is looked for.
        /// </summary>
        public void Set(int tonic, int low, int high)
        {
            Tonic = Math.Clamp(tonic, 0, HighestNote);
            low = Math.Clamp(low, 0, HighestNote);
            high = Math.Clamp(high, 0, HighestNote);
            if (low > high)
            {
                (low, high) = (high, low);
            }

            if (high - low < NarrowestRange)
            {
                high = Math.Min(low + NarrowestRange, HighestNote);
                low = high - NarrowestRange;
            }

            Low = low;
            High = high;
        }

        /// <summary>Starts a new melody: the next note is placed nearest the tonic.</summary>
        public void Reset()
        {
            _previous = -1;
        }

        /// <summary>The MIDI note for this degree, placed after the note before; it becomes the note before the next.</summary>
        public int Place(int degree)
        {
            int note = Placement(degree, _previous < 0 ? Tonic : _previous);
            _previous = note;
            return note;
        }

        /// <summary>
        /// The MIDI note for this degree nearest to the reference note, brought into range, without
        /// placing it. A tie goes toward the middle of the range.
        /// </summary>
        public int Placement(int degree, int reference)
        {
            if (!Degree.IsValid(degree))
            {
                throw new ArgumentOutOfRangeException(nameof(degree), $"A degree is 0 to 11: {degree}.");
            }

            // The degree's pitch class above the reference: 0 to 11 semitones up, or that less an octave down.
            int pitchClass = (Tonic + degree) % Degree.Count;
            int up = ((pitchClass - reference) % Degree.Count + Degree.Count) % Degree.Count;
            int above = reference + up;
            int below = above - Degree.Count;
            int note;
            if (up < Degree.Count - up)
            {
                note = above;
            }
            else if (up > Degree.Count - up)
            {
                note = below;
            }
            else
            {
                // A tritone either way: toward the middle of the range.
                int middle2 = Low + High; // twice the middle, to stay in whole numbers
                note = Math.Abs(2 * above - middle2) <= Math.Abs(2 * below - middle2) ? above : below;
            }

            while (note < Low)
            {
                note += Degree.Count;
            }

            while (note > High)
            {
                note -= Degree.Count;
            }

            return note;
        }
    }
}
