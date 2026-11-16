using System;

namespace Aurumworks.Aleatora
{
    /// <summary>The note values a hit's note can be quantized to (the emitter plan, A hit; ruling 64).</summary>
    public enum QuantizeGrid
    {
        /// <summary>No quantizing: the note goes on as the hit lands.</summary>
        Off,

        /// <summary>A quarter note: 1 beat.</summary>
        Quarter,

        /// <summary>An eighth note: ½ beat.</summary>
        Eighth,

        /// <summary>A sixteenth note: ¼ beat.</summary>
        Sixteenth,

        /// <summary>A thirty-second note: ⅛ beat.</summary>
        ThirtySecond,

        /// <summary>An eighth-note triplet: ⅓ beat.</summary>
        EighthTriplet,

        /// <summary>A sixteenth-note triplet: ⅙ beat.</summary>
        SixteenthTriplet,
    }

    /// <summary>
    /// Quantizing a hit's note to the clock (the emitter plan, A hit; rulings 64 and 66): its start
    /// moves forward toward the next grid point by the strength, as a note can't go back in time; its
    /// length toward the nearest whole number of grid steps, at least one. Beats are quarter notes.
    /// Plain C#, for the EditMode tests.
    /// </summary>
    public static class Quantize
    {
        /// <summary>How close to a grid point, in grid steps, counts as on it.</summary>
        public const double Epsilon = 1e-9;

        /// <summary>A grid's step in beats: a quarter note 1, an eighth-note triplet ⅓; Off 0.</summary>
        public static double Beats(QuantizeGrid grid)
        {
            switch (grid)
            {
                case QuantizeGrid.Quarter:
                    return 1d;
                case QuantizeGrid.Eighth:
                    return 0.5d;
                case QuantizeGrid.Sixteenth:
                    return 0.25d;
                case QuantizeGrid.ThirtySecond:
                    return 0.125d;
                case QuantizeGrid.EighthTriplet:
                    return 1d / 3d;
                case QuantizeGrid.SixteenthTriplet:
                    return 1d / 6d;
                default:
                    return 0d;
            }
        }

        /// <summary>The first grid point at or after a beat; the beat itself with no grid (a step of 0 or less).</summary>
        public static double NextPoint(double beat, double grid)
        {
            if (grid <= 0d)
            {
                return beat;
            }

            double next = Math.Ceiling(beat / grid - Epsilon) * grid;
            return Math.Max(next, beat); // within Epsilon of a point, it is on it
        }

        /// <summary>
        /// When a note struck at this beat goes on: b + strength·(the next grid point − b). Never
        /// before b, never after the next grid point; on a grid point it stays.
        /// </summary>
        public static double Start(double beat, double grid, double strength)
        {
            return beat + Clamp01(strength) * (NextPoint(beat, grid) - beat);
        }

        /// <summary>
        /// A note's length, in beats: L + strength·(Lq − L), Lq the nearest whole number of grid
        /// steps to L, at least one. With no grid, L.
        /// </summary>
        public static double Length(double length, double grid, double strength)
        {
            if (grid <= 0d)
            {
                return length;
            }

            double steps = Math.Max(1d, Math.Round(length / grid, MidpointRounding.AwayFromZero));
            return length + Clamp01(strength) * (steps * grid - length);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }
}
