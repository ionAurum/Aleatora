using System;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the project's random numbers: the first numbers of three seeds are those of
    /// the reference xoshiro128** seeded by SplitMix64 (worked out apart from this code), so every
    /// runtime gives the same; Reset starts again; NextDouble and Next stay in range and Next is even.
    /// </summary>
    public class RngTests
    {
        [TestCase(0UL, 3737715805u, 2584255861u, 2876756834u, 3286328325u)]
        [TestCase(1UL, 1695105466u, 1423115009u, 634581793u, 1068227753u)]
        [TestCase(0xDEADBEEFUL, 2848183187u, 2643428161u, 2173202904u, 2114983966u)]
        public void TheFirstNumbersAreTheReferences(ulong seed, uint a, uint b, uint c, uint d)
        {
            var rng = new Rng(seed);
            Assert.That(new[] { rng.NextUInt(), rng.NextUInt(), rng.NextUInt(), rng.NextUInt() }, Is.EqualTo(new[] { a, b, c, d }));
        }

        [Test]
        public void NextDoubleIsTheReference()
        {
            Assert.That(new Rng(42).NextDouble(), Is.EqualTo(0.4137016681565887));
        }

        [Test]
        public void ResetStartsAgain()
        {
            var rng = new Rng(9);
            uint[] first = { rng.NextUInt(), rng.NextUInt(), rng.NextUInt() };
            rng.NextDouble();
            rng.Reset(9);
            Assert.That(new[] { rng.NextUInt(), rng.NextUInt(), rng.NextUInt() }, Is.EqualTo(first));
            Assert.That(rng.Seed, Is.EqualTo(9UL));
        }

        [Test]
        public void NextDoubleIsFromZeroToOne()
        {
            var rng = new Rng(3);
            double sum = 0d;
            for (int i = 0; i < 100000; i++)
            {
                double x = rng.NextDouble();
                Assert.That(x, Is.GreaterThanOrEqualTo(0d).And.LessThan(1d));
                sum += x;
            }

            Assert.That(sum / 100000d, Is.EqualTo(0.5).Within(0.005));
        }

        [Test]
        public void NextIsEvenAndInRange()
        {
            var rng = new Rng(5);
            int[] seen = new int[12];
            const int draws = 120000;
            for (int i = 0; i < draws; i++)
            {
                int x = rng.Next(12);
                Assert.That(x, Is.InRange(0, 11));
                seen[x]++;
            }

            foreach (int count in seen)
            {
                Assert.That(count / (double)draws, Is.EqualTo(1d / 12d).Within(0.005));
            }

            Assert.That(rng.Next(1), Is.EqualTo(0));
            Assert.That(rng.Next(int.MaxValue), Is.InRange(0, int.MaxValue - 1));
        }

        [Test]
        public void NextRefusesNoCount()
        {
            var rng = new Rng(1);
            Assert.That(() => rng.Next(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => rng.Next(-3), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
