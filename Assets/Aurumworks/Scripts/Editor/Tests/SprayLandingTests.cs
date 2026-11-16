using System;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of where the spray lands (the emitter plan, EM-6; the controls doc, A4):
    /// straight down lands below; a throw lands where small steps under gravity put it; aimed
    /// level or above, it still lands under gravity, and without gravity never; the edge's
    /// directions are unit length, at the half-angle from the axis and evenly round it, whichever
    /// way the axis points.
    /// </summary>
    public class SprayLandingTests
    {
        private const double Gravity = 9.81;

        [Test]
        public void StraightDownLandsBelow()
        {
            Assert.That(SprayLanding.Land(1, 10, 2, 0, -8, 0, Gravity, 0, out double x, out double z, out double time), Is.True);
            Assert.That(x, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(z, Is.EqualTo(2.0).Within(1e-12));
            Assert.That(10 - (8 * time) - (0.5 * Gravity * time * time), Is.EqualTo(0.0).Within(1e-9), "on the plane at that time");
        }

        [TestCase(0.0, 10.0, 0.0, 5.0, -4.0, 2.0)]
        [TestCase(-3.0, 8.0, 1.0, 8.0, 0.0, 0.0)]
        [TestCase(0.0, 3.0, 0.0, -2.0, 6.0, 3.0)]
        public void AThrowLandsWhereSmallStepsPutIt(double px, double py, double pz, double vx, double vy, double vz)
        {
            Assert.That(SprayLanding.Land(px, py, pz, vx, vy, vz, Gravity, 0, out double x, out double z, out double time), Is.True);

            // Small steps under gravity, as a physics engine would, to the crossing.
            const double step = 1e-5;
            double sx = px, sy = py, sz = pz, svy = vy, t = 0;
            while (sy > 0 || svy > 0)
            {
                sx += vx * step;
                sz += vz * step;
                sy += (svy * step) - (0.5 * Gravity * step * step);
                svy -= Gravity * step;
                t += step;
            }

            Assert.That(time, Is.EqualTo(t).Within(1e-3), "time");
            Assert.That(x, Is.EqualTo(sx).Within(1e-2), "x");
            Assert.That(z, Is.EqualTo(sz).Within(1e-2), "z");
        }

        [Test]
        public void LevelOrUpLandsUnderGravityAndNeverWithout()
        {
            Assert.That(SprayLanding.Land(0, 5, 0, 4, 0, 0, Gravity, 0, out double x, out _, out _), Is.True, "level");
            Assert.That(x, Is.EqualTo(4 * Math.Sqrt(2 * 5 / Gravity)).Within(1e-9));
            Assert.That(SprayLanding.Land(0, 5, 0, 4, 3, 0, Gravity, 0, out _, out _, out _), Is.True, "upward");
            Assert.That(SprayLanding.Land(0, 5, 0, 4, 0, 0, 0, 0, out _, out _, out _), Is.False, "level, no gravity");
            Assert.That(SprayLanding.Land(0, 5, 0, 0, -5, 0, 0, 0, out _, out _, out double time), Is.True, "down, no gravity");
            Assert.That(time, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(SprayLanding.Land(0, -5, 0, 0, 1, 0, Gravity, 0, out _, out _, out _), Is.False, "below, never rising to it");
        }

        [TestCase(0.0, -1.0, 0.0, 10.0)]
        [TestCase(0.0, -1.0, 0.0, 90.0)]
        [TestCase(0.70710678118654757, -0.70710678118654757, 0.0, 30.0)]
        [TestCase(0.0, 0.0, 1.0, 45.0)]
        [TestCase(0.6, -0.48, 0.64, 0.0)]
        public void TheEdgeIsEvenlyRoundTheAxisAtTheHalfAngle(double ax, double ay, double az, double halfAngle)
        {
            const int count = 32;
            double cos = Math.Cos(halfAngle * Math.PI / 180.0);
            double sumX = 0, sumY = 0, sumZ = 0;
            for (int i = 0; i < count; i++)
            {
                SprayLanding.Edge(ax, ay, az, halfAngle, i, count, out double x, out double y, out double z);
                Assert.That((x * x) + (y * y) + (z * z), Is.EqualTo(1.0).Within(1e-9), $"unit, {i}");
                Assert.That((x * ax) + (y * ay) + (z * az), Is.EqualTo(cos).Within(1e-9), $"at the half-angle, {i}");
                sumX += x;
                sumY += y;
                sumZ += z;
            }

            // Evenly round: the directions average to the axis's share.
            Assert.That(sumX / count, Is.EqualTo(ax * cos).Within(1e-9));
            Assert.That(sumY / count, Is.EqualTo(ay * cos).Within(1e-9));
            Assert.That(sumZ / count, Is.EqualTo(az * cos).Within(1e-9));
        }
    }
}
