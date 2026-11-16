using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The cone's tilt (the controls doc, A3; the emitter plan, EM-6): two angles in degrees, each
    /// from −90 to 90, so 0–180° from the horizon on one side through straight down (0, 0) to the
    /// horizon on the other. TiltX swings the axis toward +X, TiltZ toward +Z, in the world: the
    /// direction is (sin x cos z, −cos x cos z, sin z), unit length and never above the horizon.
    /// A movement on the board's plane (the mouse's, turned onto the camera's flattened axes) adds
    /// to the two angles, each held at its limits. Plain C#, so EditMode tests run it whole.
    /// </summary>
    public static class ConeTilt
    {
        /// <summary>The largest tilt either way on each axis, in degrees (the horizon).</summary>
        public const double Limit = 90.0;

        private const double Radians = Math.PI / 180.0;

        /// <summary>The axis's direction for a tilt: unit length, its y never above 0.</summary>
        public static void Direction(double tiltX, double tiltZ, out double x, out double y, out double z)
        {
            double a = Clamp(tiltX) * Radians;
            double b = Clamp(tiltZ) * Radians;
            x = Math.Sin(a) * Math.Cos(b);
            y = -Math.Cos(a) * Math.Cos(b);
            z = Math.Sin(b);
        }

        /// <summary>
        /// The tilt that points the axis along a direction (any length); a direction above the
        /// horizon gives the tilt at the horizon nearest it. A zero direction is straight down.
        /// </summary>
        public static void Toward(double x, double y, double z, out double tiltX, out double tiltZ)
        {
            double length = Math.Sqrt((x * x) + (y * y) + (z * z));
            if (length < 1e-12)
            {
                tiltX = 0.0;
                tiltZ = 0.0;
                return;
            }

            x /= length;
            y /= length;
            z /= length;
            tiltZ = Math.Asin(Math.Max(-1.0, Math.Min(1.0, z))) / Radians;
            tiltX = Clamp(Math.Atan2(x, Math.Max(0.0, -y)) / Radians);
        }

        /// <summary>Adds a movement on the board's plane, in degrees along X and along Z, each angle held within the limits.</summary>
        public static void Turn(ref double tiltX, ref double tiltZ, double alongX, double alongZ)
        {
            tiltX = Clamp(tiltX + alongX);
            tiltZ = Clamp(tiltZ + alongZ);
        }

        /// <summary>An angle held within −Limit and Limit.</summary>
        public static double Clamp(double angle) => Math.Max(-Limit, Math.Min(Limit, angle));
    }
}
