using System;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the degrees' names (Degree) and of the modes' degrees (ModeScale).
    /// </summary>
    public class DegreeTests
    {
        private static readonly string[] _onDisk = { "1", "b2", "2", "b3", "3", "4", "#4", "5", "b6", "6", "b7", "7" };

        [Test]
        public void NamesAreAsOnDiskAndParseBack()
        {
            Assert.That(Degree.Count, Is.EqualTo(12));
            for (int degree = 0; degree < Degree.Count; degree++)
            {
                Assert.That(Degree.Name(degree), Is.EqualTo(_onDisk[degree]));
                Assert.That(Degree.TryParse(_onDisk[degree], out int parsed), Is.True, _onDisk[degree]);
                Assert.That(parsed, Is.EqualTo(degree), _onDisk[degree]);
            }
        }

        [Test]
        public void WhatIsNoDegreeDoesNotParse()
        {
            foreach (string text in new[] { null, "", "0", "8", "b1", "#1", "b5", "B2", " 1", "1 ", "12", "♭2" })
            {
                Assert.That(Degree.TryParse(text, out int parsed), Is.False, $"'{text}'");
                Assert.That(parsed, Is.EqualTo(-1), $"'{text}'");
            }
        }

        [Test]
        public void DisplayUsesTheFlatAndSharpSigns()
        {
            Assert.That(Degree.Display(0), Is.EqualTo("1"));
            Assert.That(Degree.Display(1), Is.EqualTo("♭2"));
            Assert.That(Degree.Display(6), Is.EqualTo("♯4"));
            Assert.That(Degree.Display(10), Is.EqualTo("♭7"));
            Assert.That(Degree.Display(11), Is.EqualTo("7"));
        }

        [Test]
        public void ANumberThatIsNoDegreeIsRefused()
        {
            Assert.That(() => Degree.Name(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => Degree.Name(12), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => Degree.Display(12), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [TestCase("ionian", "1 2 3 4 5 6 7")]
        [TestCase("aeolian", "1 2 b3 4 5 b6 b7")]
        [TestCase("dorian", "1 2 b3 4 5 6 b7")]
        [TestCase("mixolydian", "1 2 3 4 5 6 b7")]
        [TestCase("harmonic_minor", "1 2 b3 4 5 b6 7")]
        [TestCase("gong", "1 2 3 5 6")]
        [TestCase("zhi", "1 2 4 5 6")]
        [TestCase("yu", "1 b3 4 5 b7")]
        [TestCase("lydian", "1 2 3 #4 5 6 7")]
        [TestCase("phrygian", "1 b2 b3 4 5 b6 b7")]
        [TestCase("double_harmonic", "1 b2 3 4 5 b6 7")]
        [TestCase("hindu_pentatonic", "1 3 4 5 b7")]
        [TestCase("locrian", "1 b2 2 b3 3 4 #4 5 b6 6 b7 7")]
        public void AModeHasItsDegrees(string mode, string degrees)
        {
            int mask = 0;
            foreach (string name in degrees.Split(' '))
            {
                Assert.That(Degree.TryParse(name, out int degree), Is.True, name);
                mask |= 1 << degree;
            }

            Assert.That(ModeScale.Mask(mode), Is.EqualTo(mask), mode);
            for (int degree = 0; degree < Degree.Count; degree++)
            {
                Assert.That(ModeScale.Contains(mode, degree), Is.EqualTo((mask & (1 << degree)) != 0), $"{mode}, {Degree.Name(degree)}");
            }

            Assert.That(ModeScale.Contains(mode, -1), Is.False);
            Assert.That(ModeScale.Contains(mode, 12), Is.False);
        }

        [Test]
        public void AnUnknownModeHasAllTwelve()
        {
            Assert.That(ModeScale.Mask("no such mode"), Is.EqualTo(ModeScale.All));
            Assert.That(ModeScale.Mask(null), Is.EqualTo(ModeScale.All));
            Assert.That(ModeScale.All, Is.EqualTo(4095));
        }
    }
}
