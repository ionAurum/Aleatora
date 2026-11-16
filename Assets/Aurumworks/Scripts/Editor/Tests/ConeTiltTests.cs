using System;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the cone's tilt (the emitter plan, EM-6; the controls doc, A3): no tilt is
    /// straight down; a full tilt on one axis is the horizon that way; every tilt is unit length and
    /// never above the horizon; Toward undoes Direction; a direction above the horizon is held at it;
    /// turning stops at the limits and comes back at once.
    /// </summary>
    public class ConeTiltTests
    {
        private const double Close = 1e-9;

        [Test]
        public void NoTiltIsStraightDown()
        {
            ConeTilt.Direction(0, 0, out double x, out double y, out double z);
            Assert.That(x, Is.EqualTo(0.0).Within(Close));
            Assert.That(y, Is.EqualTo(-1.0).Within(Close));
            Assert.That(z, Is.EqualTo(0.0).Within(Close));
        }

        [TestCase(90.0, 0.0, 1.0, 0.0, 0.0)]
        [TestCase(-90.0, 0.0, -1.0, 0.0, 0.0)]
        [TestCase(0.0, 90.0, 0.0, 0.0, 1.0)]
        [TestCase(0.0, -90.0, 0.0, 0.0, -1.0)]
        [TestCase(45.0, 0.0, 0.70710678118654757, -0.70710678118654757, 0.0)]
        public void AFullTiltIsTheHorizon(double tiltX, double tiltZ, double ex, double ey, double ez)
        {
            ConeTilt.Direction(tiltX, tiltZ, out double x, out double y, out double z);
            Assert.That(x, Is.EqualTo(ex).Within(Close), "x");
            Assert.That(y, Is.EqualTo(ey).Within(Close), "y");
            Assert.That(z, Is.EqualTo(ez).Within(Close), "z");
        }

        [Test]
        public void EveryTiltIsUnitLengthAndNotAboveTheHorizon()
        {
            for (double a = -120; a <= 120; a += 7.5)
            {
                for (double b = -120; b <= 120; b += 7.5)
                {
                    ConeTilt.Direction(a, b, out double x, out double y, out double z);
                    Assert.That((x * x) + (y * y) + (z * z), Is.EqualTo(1.0).Within(Close), $"unit at {a}, {b}");
                    Assert.That(y, Is.LessThanOrEqualTo(Close), $"not above the horizon at {a}, {b}");
                }
            }
        }

        [Test]
        public void TowardUndoesDirection()
        {
            for (double a = -85; a <= 85; a += 17)
            {
                for (double b = -85; b <= 85; b += 17)
                {
                    ConeTilt.Direction(a, b, out double x, out double y, out double z);
                    ConeTilt.Toward(x * 3, y * 3, z * 3, out double backX, out double backZ);
                    Assert.That(backX, Is.EqualTo(a).Within(1e-6), $"x at {a}, {b}");
                    Assert.That(backZ, Is.EqualTo(b).Within(1e-6), $"z at {a}, {b}");
                }
            }
        }

        [Test]
        public void TowardAboveTheHorizonIsHeldAtIt()
        {
            ConeTilt.Toward(1, 1, 0, out double tiltX, out double tiltZ);
            Assert.That(tiltX, Is.EqualTo(90.0).Within(Close));
            Assert.That(tiltZ, Is.EqualTo(0.0).Within(Close));
            ConeTilt.Toward(0, 0, 0, out tiltX, out tiltZ);
            Assert.That(tiltX, Is.EqualTo(0.0));
            Assert.That(tiltZ, Is.EqualTo(0.0));
        }

        [Test]
        public void TurningStopsAtTheLimitsAndComesBackAtOnce()
        {
            double tiltX = 0, tiltZ = 0;
            ConeTilt.Turn(ref tiltX, ref tiltZ, 500, -500);
            Assert.That(tiltX, Is.EqualTo(ConeTilt.Limit));
            Assert.That(tiltZ, Is.EqualTo(-ConeTilt.Limit));
            ConeTilt.Turn(ref tiltX, ref tiltZ, -10, 10);
            Assert.That(tiltX, Is.EqualTo(80.0).Within(Close), "back from the limit at once");
            Assert.That(tiltZ, Is.EqualTo(-80.0).Within(Close));
            Assert.That(Math.Abs(ConeTilt.Clamp(-1000)), Is.EqualTo(ConeTilt.Limit));
        }
    }
}
