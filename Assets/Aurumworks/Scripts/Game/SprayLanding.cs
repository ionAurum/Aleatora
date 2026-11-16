using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Where the spray lands (the controls doc, A4; the emitter plan, EM-6): a sphere launched from
    /// a point at a velocity falls under gravity (straight down, no drag, as the spheres' Rigidbodies
    /// have it) and crosses the board's plane on its way down. Land works that out in closed form;
    /// Edge gives the directions on the edge of the spray's cone, a ring round its axis, so the
    /// emitter can draw the outline of where the spray's first fall goes. Bounces and the speed's
    /// jitter aren't in it. Plain C#, so EditMode tests run it whole.
    /// </summary>
    public static class SprayLanding
    {
        private const double Radians = Math.PI / 180.0;

        /// <summary>
        /// Where a body from (px, py, pz) at velocity (vx, vy, vz), falling at gravity units a
        /// second squared (0 or more, downward), crosses the plane y = planeY going down: the
        /// point's x and z and the time. False if it never does (no gravity and not moving down,
        /// or below the plane and never rising to it).
        /// </summary>
        public static bool Land(double px, double py, double pz, double vx, double vy, double vz, double gravity, double planeY,
            out double x, out double z, out double time)
        {
            x = px;
            z = pz;
            time = 0.0;
            double height = py - planeY;
            if (gravity <= 1e-9)
            {
                if (vy >= -1e-12 || height < 0.0)
                {
                    return false;
                }

                time = height / -vy;
            }
            else
            {
                // height + vy t − g t² / 2 = 0, the later root: the crossing on the way down.
                double discriminant = (vy * vy) + (2.0 * gravity * height);
                if (discriminant < 0.0)
                {
                    return false;
                }

                time = (vy + Math.Sqrt(discriminant)) / gravity;
                if (time < 0.0)
                {
                    return false;
                }
            }

            x = px + (vx * time);
            z = pz + (vz * time);
            return true;
        }

        /// <summary>
        /// The index-th of count directions on the edge of the cone round the axis (ax, ay, az, unit
        /// length) whose half-angle is halfAngle degrees: unit length, evenly round the axis.
        /// </summary>
        public static void Edge(double ax, double ay, double az, double halfAngle, int index, int count,
            out double x, out double y, out double z)
        {
            // Two directions square to the axis and to each other.
            double hx = 0.0, hy = 1.0, hz = 0.0;
            if (Math.Abs(ay) > 0.9)
            {
                hx = 1.0;
                hy = 0.0;
            }

            double ux = (ay * hz) - (az * hy), uy = (az * hx) - (ax * hz), uz = (ax * hy) - (ay * hx);
            double length = Math.Sqrt((ux * ux) + (uy * uy) + (uz * uz));
            ux /= length;
            uy /= length;
            uz /= length;
            double wx = (ay * uz) - (az * uy), wy = (az * ux) - (ax * uz), wz = (ax * uy) - (ay * ux);

            double theta = halfAngle * Radians;
            double phi = 2.0 * Math.PI * index / Math.Max(1, count);
            double along = Math.Cos(theta), across = Math.Sin(theta);
            double cos = Math.Cos(phi), sin = Math.Sin(phi);
            x = (ax * along) + (((ux * cos) + (wx * sin)) * across);
            y = (ay * along) + (((uy * cos) + (wy * sin)) * across);
            z = (az * along) + (((uz * cos) + (wz * sin)) * across);
        }
    }
}
