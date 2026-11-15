using System;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of placing degrees as MIDI notes: always in the range and in 0 to 127, each in
    /// its pitch class; a melody's first note nearest the tonic; a tritone tie toward the middle; a
    /// range widened or turned round when it must be; and the leaps. The plan asked for no leap
    /// larger than a tritone with a range of an octave and a half or more; that can't hold at the
    /// range's edges, where the nearest placement lies outside and moves an octave inward. So: no
    /// leap above a tritone from a note at least a tritone inside both edges, and none of an octave
    /// or more anywhere.
    /// </summary>
    public class VoicingTests
    {
        private static int PitchClass(int note) => note % Degree.Count;

        [TestCase(60, 55, 79)]
        [TestCase(60, 60, 71)]
        [TestCase(0, 0, 11)]
        [TestCase(127, 116, 127)]
        [TestCase(66, 30, 100)]
        [TestCase(48, 0, 127)]
        public void EveryNoteIsInRangeAndInItsPitchClass(int tonic, int low, int high)
        {
            var voicing = new Voicing(tonic, low, high);
            var rng = new Rng(17);
            for (int i = 0; i < 20000; i++)
            {
                if (i % 500 == 0)
                {
                    voicing.Reset();
                }

                int degree = rng.Next(Degree.Count);
                int note = voicing.Place(degree);
                Assert.That(note, Is.InRange(voicing.Low, voicing.High).And.InRange(0, 127), $"note {i}");
                Assert.That(PitchClass(note), Is.EqualTo(PitchClass(tonic + degree)), $"note {i}");
            }
        }

        [Test]
        public void TheFirstNoteIsNearestTheTonic()
        {
            var voicing = new Voicing(60, 55, 79);
            int[] expected = { 60, 61, 62, 63, 64, 65, 66, 55, 56, 57, 58, 59 }; // ♯4 a tie, toward the middle (67): up
            for (int degree = 0; degree < Degree.Count; degree++)
            {
                voicing.Reset();
                Assert.That(voicing.Place(degree), Is.EqualTo(expected[degree]), Degree.Name(degree));
            }
        }

        [Test]
        public void ATritoneTieGoesTowardTheMiddle()
        {
            Assert.That(new Voicing(60, 55, 79).Placement(6, 60), Is.EqualTo(66), "middle 67: up");
            Assert.That(new Voicing(60, 40, 64).Placement(6, 60), Is.EqualTo(54), "middle 52: down");
        }

        [Test]
        public void EachNextNoteIsNearestTheOneBefore()
        {
            var voicing = new Voicing(60, 55, 79);
            Assert.That(voicing.Place(0), Is.EqualTo(60));
            Assert.That(voicing.Place(4), Is.EqualTo(64)); // 3
            Assert.That(voicing.Place(9), Is.EqualTo(69)); // 6
            Assert.That(voicing.Place(2), Is.EqualTo(74)); // 2, a fourth up rather than a fifth down
            Assert.That(voicing.Place(7), Is.EqualTo(79)); // 5, at the top
            Assert.That(voicing.Place(9), Is.EqualTo(69)); // 6: 81 is out of range, an octave inward
            Assert.That(voicing.Previous, Is.EqualTo(69));
        }

        [Test]
        public void ANarrowOrBackwardRangeIsMadeWhole()
        {
            var narrow = new Voicing(60, 60, 62);
            Assert.That((narrow.Low, narrow.High), Is.EqualTo((60, 71)));
            var backward = new Voicing(60, 79, 55);
            Assert.That((backward.Low, backward.High), Is.EqualTo((55, 79)));
            var top = new Voicing(130, 125, 127);
            Assert.That((top.Tonic, top.Low, top.High), Is.EqualTo((127, 116, 127)));
            var bottom = new Voicing(-5, -10, 3);
            Assert.That((bottom.Tonic, bottom.Low, bottom.High), Is.EqualTo((0, 0, 11)));
        }

        [TestCase(55, 79)]
        [TestCase(48, 66)]
        [TestCase(36, 84)]
        public void LeapsStayWithinATritoneAwayFromTheEdges(int low, int high)
        {
            var voicing = new Voicing(60, low, high);
            var rng = new Rng(23);
            int previous = -1;
            int inside = 0;
            for (int i = 0; i < 20000; i++)
            {
                int note = voicing.Place(rng.Next(Degree.Count));
                if (previous >= 0)
                {
                    int leap = Math.Abs(note - previous);
                    Assert.That(leap, Is.LessThan(Degree.Count), $"note {i}: {previous} to {note}");
                    if (previous - low >= 6 && high - previous >= 6)
                    {
                        inside++;
                        Assert.That(leap, Is.LessThanOrEqualTo(6), $"note {i}: {previous} to {note}");
                    }
                }

                previous = note;
            }

            Assert.That(inside, Is.GreaterThan(1000));
        }

        [Test]
        public void ADegreeThatIsNoneIsRefused()
        {
            var voicing = new Voicing(60, 55, 79);
            Assert.That(() => voicing.Place(12), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => voicing.Place(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
