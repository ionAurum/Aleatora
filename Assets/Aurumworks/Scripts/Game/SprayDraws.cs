using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// One sphere's draws as the emitter launches it (the emitter plan, EM-2): its size, its speed
    /// and its direction. The direction is a unit vector in the emitter's own frame, whose axis is
    /// straight down (−Y), as the cone points.
    /// </summary>
    public readonly struct SprayDraw
    {
        /// <summary>A sphere's draws: its diameter, its speed, and its direction's x, y and z.</summary>
        public SprayDraw(double size, double speed, double x, double y, double z)
        {
            Size = size;
            Speed = speed;
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>The sphere's diameter, in units.</summary>
        public double Size { get; }

        /// <summary>The sphere's launch speed, in units a second.</summary>
        public double Speed { get; }

        /// <summary>The direction's x, in the emitter's frame.</summary>
        public double X { get; }

        /// <summary>The direction's y, in the emitter's frame: −1 straight along the axis.</summary>
        public double Y { get; }

        /// <summary>The direction's z, in the emitter's frame.</summary>
        public double Z { get; }

        /// <summary>The angle between the direction and the emitter's axis, in degrees.</summary>
        public double Angle => Math.Acos(Math.Clamp(-Y, -1.0, 1.0)) * (180.0 / Math.PI);
    }

    /// <summary>
    /// Static: the emitter's draws (the emitter plan, EM-2; ruling 63), plain C# so the tests run it
    /// whole. Every sphere takes four draws from the emitter's own Rng, in the same order whatever
    /// the parameters, so one seed gives the same sequence of spheres: a size between the smallest
    /// and the largest; a speed within the jitter either side of the speed; and a direction spread
    /// evenly over the cap of the cone whose half-angle is the spread (the angle from the axis to
    /// the cone's edge, as the ParticleSystem's cone measures it).
    /// </summary>
    public static class SprayDraws
    {
        /// <summary>The largest spread, in degrees from the axis.</summary>
        public const double MostSpread = 90.0;

        /// <summary>
        /// Draws one sphere: the size between minSize and maxSize (either way round), the speed
        /// within ±jitter (a fraction, 0 to 1) of speed, the direction within spread degrees of the axis.
        /// </summary>
        public static SprayDraw Draw(Rng rng, double minSize, double maxSize, double speed, double jitter, double spread)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            double low = Math.Min(minSize, maxSize);
            double high = Math.Max(minSize, maxSize);
            double size = low + ((high - low) * rng.NextDouble());

            double drawnSpeed = speed * (1.0 + (Math.Clamp(jitter, 0.0, 1.0) * ((2.0 * rng.NextDouble()) - 1.0)));

            // Even over the cap: cos θ even between cos(spread) and 1, the turn about the axis even.
            double cosSpread = Math.Cos(Math.Clamp(spread, 0.0, MostSpread) * (Math.PI / 180.0));
            double cos = 1.0 - (rng.NextDouble() * (1.0 - cosSpread));
            double sin = Math.Sqrt(Math.Max(0.0, 1.0 - (cos * cos)));
            double turn = 2.0 * Math.PI * rng.NextDouble();
            return new SprayDraw(size, drawnSpeed, sin * Math.Cos(turn), -cos, sin * Math.Sin(turn));
        }
    }
}
