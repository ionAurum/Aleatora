using System;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the right trigger's rate (EM-6, after Jon's try; ruling 80): nothing inside
    /// the dead zone; the least just past it; Rate at a full pull; geometric between, so equal travel
    /// multiplies the rate by the same factor; never more than Rate; a Rate at or below the least is
    /// reached linearly.
    /// </summary>
    public class TriggerRateTests
    {
        private const double Close = 1e-9;

        [TestCase(0.0)]
        [TestCase(-0.5)]
        [TestCase(TriggerRate.DeadZone)]
        [TestCase(double.NaN)]
        public void NothingInsideTheDeadZone(double pull)
        {
            Assert.That(TriggerRate.Rate(pull, 0.5, 4.0), Is.EqualTo(0.0));
        }

        [Test]
        public void ALightPullIsTheLeast()
        {
            Assert.That(TriggerRate.Rate(TriggerRate.DeadZone + 1e-12, 0.5, 4.0), Is.EqualTo(0.5).Within(1e-6));
        }

        [TestCase(1.0)]
        [TestCase(1.5)]
        public void AFullPullIsRate(double pull)
        {
            Assert.That(TriggerRate.Rate(pull, 0.5, 4.0), Is.EqualTo(4.0).Within(Close));
        }

        [Test]
        public void EqualTravelIsAnEqualFactor()
        {
            double Pull(double travel) => TriggerRate.DeadZone + (travel * (1.0 - TriggerRate.DeadZone));
            double quarter = TriggerRate.Rate(Pull(0.25), 0.5, 8.0);
            double half = TriggerRate.Rate(Pull(0.5), 0.5, 8.0);
            double threeQuarters = TriggerRate.Rate(Pull(0.75), 0.5, 8.0);
            Assert.That(half, Is.EqualTo(2.0).Within(Close)); // the geometric middle of 0.5 and 8
            Assert.That(half / quarter, Is.EqualTo(threeQuarters / half).Within(Close));
        }

        [Test]
        public void RisesWithThePull()
        {
            double last = 0.0;
            for (int i = 0; i <= 100; i++)
            {
                double rate = TriggerRate.Rate(i / 100.0, 0.5, 20.0);
                Assert.That(rate, Is.GreaterThanOrEqualTo(last));
                Assert.That(rate, Is.LessThanOrEqualTo(20.0 + Close));
                last = rate;
            }
        }

        [Test]
        public void ARateAtOrBelowTheLeastIsLinear()
        {
            double pull = TriggerRate.DeadZone + (0.5 * (1.0 - TriggerRate.DeadZone));
            Assert.That(TriggerRate.Rate(pull, 0.5, 0.4), Is.EqualTo(0.2).Within(Close));
            Assert.That(TriggerRate.Rate(1.0, 0.5, 0.5), Is.EqualTo(0.5).Within(Close));
            Assert.That(TriggerRate.Rate(1.0, 0.5, 0.0), Is.EqualTo(0.0));
        }
    }
}
