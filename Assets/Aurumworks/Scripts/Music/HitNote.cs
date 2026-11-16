using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// What a hit's note carries from the hit (the emitter plan, A hit; ruling 62): its velocity from
    /// the impact speed, its length from the sphere's size. Plain C#, for the EditMode tests.
    /// </summary>
    public static class HitNote
    {
        /// <summary>MIDI's highest velocity.</summary>
        public const int HighestVelocity = 127;

        /// <summary>
        /// The velocity, 1–127: the speed from minSpeed to maxSpeed mapped to minVelocity to
        /// maxVelocity, the fraction raised to the curve first (1 straight; above 1 softer hits stay
        /// softer). Slower than minSpeed is minVelocity, faster than maxSpeed maxVelocity.
        /// </summary>
        public static int Velocity(double speed, double minSpeed, double maxSpeed, int minVelocity, int maxVelocity, double curve)
        {
            double t = maxSpeed > minSpeed ? Fraction(speed, minSpeed, maxSpeed) : speed >= maxSpeed ? 1d : 0d;
            t = Math.Pow(t, curve > 0d ? curve : 1d);
            int velocity = (int)Math.Round(minVelocity + t * (maxVelocity - minVelocity), MidpointRounding.AwayFromZero);
            return Math.Clamp(velocity, 1, HighestVelocity);
        }

        /// <summary>
        /// The length, in beats: the size from minSize to maxSize mapped to shortest to longest
        /// (outside it, the nearer end; with the two sizes equal, shortest). Never below a hundredth of a beat.
        /// </summary>
        public static double Length(double size, double minSize, double maxSize, double shortest, double longest)
        {
            double t = maxSize > minSize ? Fraction(size, minSize, maxSize) : 0d;
            return Math.Max(0.01d, shortest + t * (longest - shortest));
        }

        private static double Fraction(double value, double from, double to)
        {
            double t = (value - from) / (to - from);
            return t < 0d ? 0d : t > 1d ? 1d : t;
        }
    }
}
