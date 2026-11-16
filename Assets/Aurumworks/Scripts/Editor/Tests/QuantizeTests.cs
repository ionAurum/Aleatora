using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of quantizing a hit's note and what it carries from the hit (the emitter plan,
    /// EM-3): each grid value, triplets too; the start at strength 0, ½ and 1; never before the hit
    /// nor past the next grid point; on a grid point it stays; the length at least one step; velocity
    /// from speed with its curve; length from size.
    /// </summary>
    public class QuantizeTests
    {
        private const double Tolerance = 1e-9;

        [TestCase(QuantizeGrid.Off, 0d)]
        [TestCase(QuantizeGrid.Quarter, 1d)]
        [TestCase(QuantizeGrid.Eighth, 0.5d)]
        [TestCase(QuantizeGrid.Sixteenth, 0.25d)]
        [TestCase(QuantizeGrid.ThirtySecond, 0.125d)]
        [TestCase(QuantizeGrid.EighthTriplet, 1d / 3d)]
        [TestCase(QuantizeGrid.SixteenthTriplet, 1d / 6d)]
        public void EachGridIsItsBeats(QuantizeGrid grid, double beats)
        {
            Assert.That(Quantize.Beats(grid), Is.EqualTo(beats).Within(Tolerance));
        }

        [TestCase(QuantizeGrid.Quarter, 2.1d, 3d)]
        [TestCase(QuantizeGrid.Eighth, 2.1d, 2.5d)]
        [TestCase(QuantizeGrid.Sixteenth, 2.1d, 2.25d)]
        [TestCase(QuantizeGrid.ThirtySecond, 2.1d, 2.125d)]
        [TestCase(QuantizeGrid.EighthTriplet, 2.1d, 7d / 3d)]
        [TestCase(QuantizeGrid.SixteenthTriplet, 2.1d, 13d / 6d)]
        [TestCase(QuantizeGrid.Off, 2.1d, 2.1d)]
        public void AtStrengthOneTheStartIsTheNextGridPoint(QuantizeGrid grid, double beat, double expected)
        {
            Assert.That(Quantize.Start(beat, Quantize.Beats(grid), 1d), Is.EqualTo(expected).Within(Tolerance));
        }

        [TestCase(0d, 0.1d)]
        [TestCase(0.5d, 0.175d)]
        [TestCase(1d, 0.25d)]
        public void StrengthMovesTheStartPartWay(double strength, double expected)
        {
            Assert.That(Quantize.Start(0.1d, 0.25d, strength), Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void TheStartIsNeverBeforeTheHitNorPastTheNextGridPoint()
        {
            QuantizeGrid[] grids =
            {
                QuantizeGrid.Quarter, QuantizeGrid.Eighth, QuantizeGrid.Sixteenth, QuantizeGrid.ThirtySecond,
                QuantizeGrid.EighthTriplet, QuantizeGrid.SixteenthTriplet,
            };
            foreach (QuantizeGrid grid in grids)
            {
                double g = Quantize.Beats(grid);
                for (int i = 0; i < 2000; i++)
                {
                    double beat = i * 0.0137d - 3d; // negative beats too: before a followed clock's first tick
                    foreach (double strength in new[] { 0d, 0.25d, 0.5d, 0.75d, 1d })
                    {
                        double start = Quantize.Start(beat, g, strength);
                        Assert.That(start, Is.GreaterThanOrEqualTo(beat), $"{grid} beat {beat} strength {strength}");
                        Assert.That(start, Is.LessThan(beat + g + Tolerance), $"{grid} beat {beat} strength {strength}");
                    }
                }
            }
        }

        [Test]
        public void OnAGridPointItStays()
        {
            Assert.That(Quantize.Start(0.5d, 0.25d, 1d), Is.EqualTo(0.5d));
            Assert.That(Quantize.Start(3d, 1d, 1d), Is.EqualTo(3d));

            // Triplets on the clock's ticks (24 a beat): 16 ticks is two eighth-note triplets, 4 one sixteenth-note triplet.
            double triplet = Quantize.Beats(QuantizeGrid.EighthTriplet);
            double sixteenthTriplet = Quantize.Beats(QuantizeGrid.SixteenthTriplet);
            for (int tick = 0; tick < 24 * 64; tick += 4)
            {
                double beat = tick / (double)ClockFollower.TicksPerBeat;
                Assert.That(Quantize.Start(beat, sixteenthTriplet, 1d), Is.EqualTo(beat).Within(Tolerance), $"tick {tick}");
                if (tick % 8 == 0)
                {
                    Assert.That(Quantize.Start(beat, triplet, 1d), Is.EqualTo(beat).Within(Tolerance), $"tick {tick}");
                }
            }
        }

        [TestCase(0.05d, 1d, 0.25d)] // at least one step
        [TestCase(0.6d, 1d, 0.5d)]   // to the nearest
        [TestCase(0.7d, 1d, 0.75d)]
        [TestCase(0.6d, 0.5d, 0.55d)]
        [TestCase(0.6d, 0d, 0.6d)]
        public void TheLengthGoesToTheNearestWholeStepsAtLeastOne(double length, double strength, double expected)
        {
            Assert.That(Quantize.Length(length, 0.25d, strength), Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void WithNoGridTheLengthStays()
        {
            Assert.That(Quantize.Length(0.37d, 0d, 1d), Is.EqualTo(0.37d));
        }

        [Test]
        public void ATripletLengthIsWholeTriplets()
        {
            double g = Quantize.Beats(QuantizeGrid.EighthTriplet);
            Assert.That(Quantize.Length(0.1d, g, 1d), Is.EqualTo(g).Within(Tolerance));
            Assert.That(Quantize.Length(0.9d, g, 1d), Is.EqualTo(1d).Within(Tolerance));
        }

        [Test]
        public void VelocityRunsFromMinToMaxWithSpeed()
        {
            Assert.That(HitNote.Velocity(1d, 1d, 15d, 30, 127, 1d), Is.EqualTo(30));
            Assert.That(HitNote.Velocity(15d, 1d, 15d, 30, 127, 1d), Is.EqualTo(127));
            Assert.That(HitNote.Velocity(8d, 1d, 15d, 30, 127, 1d), Is.EqualTo(79)); // 30 + ½·97 = 78.5, rounded up
            Assert.That(HitNote.Velocity(0.5d, 1d, 15d, 30, 127, 1d), Is.EqualTo(30), "below Min Speed");
            Assert.That(HitNote.Velocity(40d, 1d, 15d, 30, 127, 1d), Is.EqualTo(127), "above Max Speed");
        }

        [Test]
        public void TheCurveShapesVelocity()
        {
            int straight = HitNote.Velocity(8d, 1d, 15d, 30, 127, 1d);
            int softer = HitNote.Velocity(8d, 1d, 15d, 30, 127, 2d);
            int louder = HitNote.Velocity(8d, 1d, 15d, 30, 127, 0.5d);
            Assert.That(softer, Is.EqualTo(54)); // 30 + ¼·97 = 54.25
            Assert.That(softer, Is.LessThan(straight));
            Assert.That(louder, Is.GreaterThan(straight));
        }

        [Test]
        public void VelocityIsAlwaysAMidiVelocity()
        {
            Assert.That(HitNote.Velocity(0d, 1d, 15d, 0, 200, 1d), Is.EqualTo(1));
            Assert.That(HitNote.Velocity(99d, 1d, 15d, 0, 200, 1d), Is.EqualTo(127));
        }

        [Test]
        public void LengthRunsFromShortestToLongestWithSize()
        {
            Assert.That(HitNote.Length(0.2d, 0.2d, 0.5d, 0.25d, 2d), Is.EqualTo(0.25d).Within(Tolerance));
            Assert.That(HitNote.Length(0.5d, 0.2d, 0.5d, 0.25d, 2d), Is.EqualTo(2d).Within(Tolerance));
            Assert.That(HitNote.Length(0.35d, 0.2d, 0.5d, 0.25d, 2d), Is.EqualTo(1.125d).Within(Tolerance));
            Assert.That(HitNote.Length(0.3d, 0.3d, 0.3d, 0.25d, 2d), Is.EqualTo(0.25d).Within(Tolerance), "sizes equal");
        }
    }
}
