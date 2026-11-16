using System;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the emitter's draws (the emitter plan, EM-2): sizes between the smallest
    /// and the largest, speeds within the jitter, directions unit length and inside the spread, and
    /// the whole cap used; the same seed the same spheres, another seed others; four draws a sphere
    /// whatever the parameters; a spread of 0 a jet straight down.
    /// </summary>
    public class SprayDrawsTests
    {
        private const int Many = 5000;

        [TestCase(0.2, 0.5, 8.0, 0.1, 10.0)]
        [TestCase(0.1, 1.0, 30.0, 0.5, 60.0)]
        [TestCase(0.5, 0.2, 2.0, 0.0, 0.0)]
        public void EveryDrawIsInRange(double minSize, double maxSize, double speed, double jitter, double spread)
        {
            var rng = new Rng(2112);
            double low = Math.Min(minSize, maxSize), high = Math.Max(minSize, maxSize);
            for (int i = 0; i < Many; i++)
            {
                SprayDraw draw = SprayDraws.Draw(rng, minSize, maxSize, speed, jitter, spread);
                Assert.That(draw.Size, Is.InRange(low, high), "size");
                Assert.That(draw.Speed, Is.InRange((speed * (1 - jitter)) - 1e-9, (speed * (1 + jitter)) + 1e-9), "speed");
                Assert.That((draw.X * draw.X) + (draw.Y * draw.Y) + (draw.Z * draw.Z), Is.EqualTo(1.0).Within(1e-9), "unit length");
                Assert.That(draw.Angle, Is.LessThanOrEqualTo(spread + 1e-6), "inside the spread");
                Assert.That(draw.Y, Is.LessThan(0.0), "downward, along the axis");
            }
        }

        [Test]
        public void TheDrawsFillTheirRanges()
        {
            var rng = new Rng(7);
            double smallest = double.MaxValue, largest = 0, slowest = double.MaxValue, fastest = 0, widest = 0;
            int east = 0, north = 0;
            for (int i = 0; i < Many; i++)
            {
                SprayDraw draw = SprayDraws.Draw(rng, 0.2, 0.5, 8.0, 0.1, 10.0);
                smallest = Math.Min(smallest, draw.Size);
                largest = Math.Max(largest, draw.Size);
                slowest = Math.Min(slowest, draw.Speed);
                fastest = Math.Max(fastest, draw.Speed);
                widest = Math.Max(widest, draw.Angle);
                east += draw.X > 0 ? 1 : 0;
                north += draw.Z > 0 ? 1 : 0;
            }

            Assert.That(smallest, Is.LessThan(0.21));
            Assert.That(largest, Is.GreaterThan(0.49));
            Assert.That(slowest, Is.LessThan(7.25));
            Assert.That(fastest, Is.GreaterThan(8.75));
            Assert.That(widest, Is.GreaterThan(9.5));
            Assert.That(east, Is.InRange(Many * 45 / 100, Many * 55 / 100), "as many each side of the axis");
            Assert.That(north, Is.InRange(Many * 45 / 100, Many * 55 / 100), "as many each side of the axis");
        }

        // Even over the cap: half the directions lie within the angle whose cap is half the area.
        [Test]
        public void TheDirectionsAreEvenOverTheCap()
        {
            const double spread = 40.0;
            double cosSpread = Math.Cos(spread * Math.PI / 180.0);
            double halfAngle = Math.Acos(1.0 - ((1.0 - cosSpread) / 2.0)) * 180.0 / Math.PI;
            var rng = new Rng(11);
            int inner = 0;
            for (int i = 0; i < Many; i++)
            {
                inner += SprayDraws.Draw(rng, 0.2, 0.5, 8.0, 0.1, spread).Angle < halfAngle ? 1 : 0;
            }

            Assert.That(inner, Is.InRange(Many * 47 / 100, Many * 53 / 100));
        }

        [Test]
        public void TheSameSeedGivesTheSameSpheres()
        {
            var a = new Rng(2112);
            var b = new Rng(2112);
            var c = new Rng(2113);
            int differ = 0;
            for (int i = 0; i < 100; i++)
            {
                SprayDraw x = SprayDraws.Draw(a, 0.2, 0.5, 8.0, 0.1, 10.0);
                SprayDraw y = SprayDraws.Draw(b, 0.2, 0.5, 8.0, 0.1, 10.0);
                SprayDraw z = SprayDraws.Draw(c, 0.2, 0.5, 8.0, 0.1, 10.0);
                Assert.That(new[] { x.Size, x.Speed, x.X, x.Y, x.Z }, Is.EqualTo(new[] { y.Size, y.Speed, y.X, y.Y, y.Z }));
                differ += x.Size != z.Size ? 1 : 0;
            }

            Assert.That(differ, Is.GreaterThan(90), "another seed, other spheres");
        }

        // Four draws a sphere whatever the parameters, so changing one in play doesn't shift the stream.
        [Test]
        public void EverySphereTakesFourDraws()
        {
            var drawn = new Rng(5);
            var counted = new Rng(5);
            SprayDraws.Draw(drawn, 0.1, 0.1, 2.0, 0.0, 0.0);
            SprayDraws.Draw(drawn, 0.2, 1.0, 30.0, 0.5, 60.0);
            for (int i = 0; i < 8; i++)
            {
                counted.NextDouble();
            }

            Assert.That(drawn.NextUInt(), Is.EqualTo(counted.NextUInt()));
        }

        [Test]
        public void NoSpreadIsAJetStraightDown()
        {
            var rng = new Rng(3);
            for (int i = 0; i < 100; i++)
            {
                SprayDraw draw = SprayDraws.Draw(rng, 0.3, 0.3, 8.0, 0.0, 0.0);
                Assert.That(draw.Y, Is.EqualTo(-1.0));
                Assert.That(draw.Angle, Is.EqualTo(0.0).Within(1e-9));
                Assert.That(draw.Size, Is.EqualTo(0.3));
                Assert.That(draw.Speed, Is.EqualTo(8.0));
            }
        }

        [Test]
        public void ANullGeneratorThrows()
        {
            Assert.That(() => SprayDraws.Draw(null, 0.2, 0.5, 8.0, 0.1, 10.0), Throws.TypeOf<ArgumentNullException>());
        }
    }
}
