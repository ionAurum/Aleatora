using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the octave buttons and keys (ruling 83). The board: a step moves its lowest
    /// octave by one and stops at its limits. The Melodies page: the tonic, Low and High move twelve
    /// semitones together, nothing moves if any would leave 0 to 127, and the voicing places every
    /// degree an octave away.
    /// </summary>
    public class OctaveStepTests
    {
        [Test]
        public void TheBoardStepsByAnOctave()
        {
            var tuning = new EmitterTuning { LowestOctave = 3 };
            Assert.That(tuning.StepOctave(1), Is.True);
            Assert.That(tuning.LowestOctave, Is.EqualTo(4));
            Assert.That(tuning.StepOctave(-1), Is.True);
            Assert.That(tuning.StepOctave(-1), Is.True);
            Assert.That(tuning.LowestOctave, Is.EqualTo(2));
        }

        [Test]
        public void TheBoardStopsAtItsLimits()
        {
            var tuning = new EmitterTuning { LowestOctave = EmitterTuning.MostLowestOctave };
            Assert.That(tuning.StepOctave(1), Is.False);
            Assert.That(tuning.LowestOctave, Is.EqualTo(EmitterTuning.MostLowestOctave));

            tuning.LowestOctave = EmitterTuning.LeastLowestOctave;
            Assert.That(tuning.StepOctave(-1), Is.False);
            Assert.That(tuning.LowestOctave, Is.EqualTo(EmitterTuning.LeastLowestOctave));
        }

        [Test]
        public void TheMelodyMovesTonicLowAndHighTogether()
        {
            var tuning = new MelodyTuning(); // tonic 60, Low 55, High 79
            Assert.That(tuning.ShiftOctave(1), Is.True);
            Assert.That(new[] { tuning.Tonic, tuning.Low, tuning.High }, Is.EqualTo(new[] { 72, 67, 91 }));
            Assert.That(tuning.ShiftOctave(-1), Is.True);
            Assert.That(tuning.ShiftOctave(-1), Is.True);
            Assert.That(new[] { tuning.Tonic, tuning.Low, tuning.High }, Is.EqualTo(new[] { 48, 43, 67 }));
        }

        [Test]
        public void TheMelodyStopsAtMidisEdges()
        {
            var tuning = new MelodyTuning();
            int ups = 0;
            while (tuning.ShiftOctave(1))
            {
                ups++;
            }

            Assert.That(ups, Is.EqualTo(4)); // High 79 → 127; one more would pass 127
            Assert.That(new[] { tuning.Tonic, tuning.Low, tuning.High }, Is.EqualTo(new[] { 108, 103, 127 }));

            int downs = 0;
            while (tuning.ShiftOctave(-1))
            {
                downs++;
            }

            Assert.That(downs, Is.EqualTo(8)); // Low 103 → 7; one more would pass 0
            Assert.That(new[] { tuning.Tonic, tuning.Low, tuning.High }, Is.EqualTo(new[] { 12, 7, 31 }));
        }

        [Test]
        public void TheVoicingPlacesEveryDegreeAnOctaveAway()
        {
            var tuning = new MelodyTuning();
            Voicing before = tuning.MakeVoicing();
            Assert.That(tuning.ShiftOctave(1), Is.True);
            Voicing after = tuning.MakeVoicing();
            int[] degrees = { 0, 4, 7, 11, 2, 9, 5, 0, 7, 1 };
            foreach (int degree in degrees)
            {
                Assert.That(after.Place(degree), Is.EqualTo(before.Place(degree) + 12), "degree " + degree);
            }
        }
    }
}
