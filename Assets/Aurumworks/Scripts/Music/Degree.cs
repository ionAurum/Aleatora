using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The twelve scale degrees a melody is made of: a degree is a whole number from 0 to 11, the
    /// semitones above the tonic, with no octave. On disk they are written 1 b2 2 b3 3 4 #4 5 b6 6
    /// b7 7 (Tools/markov.py), and every row of twelve counts is in that order.
    /// </summary>
    public static class Degree
    {
        /// <summary>How many degrees there are.</summary>
        public const int Count = 12;

        private static readonly string[] _names = { "1", "b2", "2", "b3", "3", "4", "#4", "5", "b6", "6", "b7", "7" };
        private static readonly string[] _display = { "1", "♭2", "2", "♭3", "3", "4", "♯4", "5", "♭6", "6", "♭7", "7" };

        /// <summary>Whether this number is a degree (0 to 11).</summary>
        public static bool IsValid(int degree) => degree >= 0 && degree < Count;

        /// <summary>The degree as the tables write it: "1", "b2", … "7".</summary>
        public static string Name(int degree)
        {
            if (!IsValid(degree))
            {
                throw new ArgumentOutOfRangeException(nameof(degree), $"A degree is 0 to 11: {degree}.");
            }

            return _names[degree];
        }

        /// <summary>The degree as the view shows it, with the flat and sharp signs: "♭2", "♯4".</summary>
        public static string Display(int degree)
        {
            if (!IsValid(degree))
            {
                throw new ArgumentOutOfRangeException(nameof(degree), $"A degree is 0 to 11: {degree}.");
            }

            return _display[degree];
        }

        /// <summary>Reads a degree as the tables write it. False, and −1, if the text is none of the twelve.</summary>
        public static bool TryParse(string text, out int degree)
        {
            if (text == null)
            {
                degree = -1;
                return false;
            }

            return TryParse(text.AsSpan(), out degree);
        }

        /// <summary>Reads a degree as the tables write it, without making a string of it.</summary>
        public static bool TryParse(ReadOnlySpan<char> text, out int degree)
        {
            for (int i = 0; i < Count; i++)
            {
                if (text.SequenceEqual(_names[i].AsSpan()))
                {
                    degree = i;
                    return true;
                }
            }

            degree = -1;
            return false;
        }
    }
}
