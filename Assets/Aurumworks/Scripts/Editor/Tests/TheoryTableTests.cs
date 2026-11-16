using System;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the four tables written from theory (Docs/Aleatora_Theory_Plan.md, TH-1):
    /// each reads with four orders and the phrase count; every phrase begins in its mode and ends on
    /// 1; only passing notes are outside the mode; each colour note's likeliest next note is its
    /// pull; and a melody from each at order 4 keeps to the mode at ChromaticWeight 0 and mostly
    /// finds its order-4 contexts.
    /// </summary>
    public class TheoryTableTests
    {
        private const int Phrases = 20000; // Tools/Markov/Theory/virtuoso.json, ruling 51

        private static MarkovTable Table(string mode)
        {
            string name = "virtuoso_" + mode;
            Assert.That(MarkovLibrary.TryGet(name, out MarkovTable table), Is.True, name);
            return table;
        }

        private static int D(string name)
        {
            Assert.That(Degree.TryParse(name, out int degree), Is.True, name);
            return degree;
        }

        private static int Sum(ReadOnlySpan<int> counts)
        {
            int sum = 0;
            foreach (int count in counts)
            {
                sum += count;
            }

            return sum;
        }

        [TestCase("lydian")]
        [TestCase("phrygian")]
        [TestCase("double_harmonic")]
        [TestCase("hindu_pentatonic")]
        public void TheSetReadsAndKeepsToItsMode(string mode)
        {
            MarkovTable table = Table(mode);
            Assert.That(table.Corpus, Is.EqualTo("virtuoso"));
            Assert.That(table.Mode, Is.EqualTo(mode));
            Assert.That(table.Tunes, Is.EqualTo(Phrases));
            Assert.That(table.MaxOrder, Is.EqualTo(4));
            Assert.That(table.Notes, Is.GreaterThanOrEqualTo(8 * Phrases));

            int mask = ModeScale.Mask(mode);
            int outside = 0;
            for (int d = 0; d < Degree.Count; d++)
            {
                if (!ModeScale.Has(mask, d))
                {
                    Assert.That(table.First(d), Is.Zero, $"no phrase begins on {Degree.Name(d)}");
                    outside += table.Unigram(d);
                }

                // Every phrase ends on 1: each degree's order-1 row holds every time it is played
                // but the last notes, which are all 1.
                if (table.TryGetCounts(new[] { d }, out ReadOnlySpan<int> row))
                {
                    Assert.That(Sum(row), Is.EqualTo(table.Unigram(d) - (d == 0 ? table.Tunes : 0)), Degree.Name(d));
                }
                else
                {
                    Assert.That(table.Unigram(d), Is.Zero, Degree.Name(d));
                }
            }

            Assert.That(outside / (double)table.Notes, Is.LessThan(0.05), "only passing notes are outside the mode, about 3% at most");
        }

        [TestCase("lydian", "#4", "5")]
        [TestCase("phrygian", "b2", "1")]
        [TestCase("double_harmonic", "b2", "1")]
        [TestCase("double_harmonic", "7", "1")]
        [TestCase("double_harmonic", "b6", "5")]
        [TestCase("hindu_pentatonic", "b7", "1 5")]
        [TestCase("hindu_pentatonic", "4", "3")]
        public void TheColourNoteLeadsToItsPull(string mode, string colour, string pulls)
        {
            MarkovTable table = Table(mode);
            Assert.That(table.TryGetCounts(new[] { D(colour) }, out ReadOnlySpan<int> row), Is.True, colour);
            int likeliest = 0;
            for (int d = 1; d < Degree.Count; d++)
            {
                if (row[d] > row[likeliest])
                {
                    likeliest = d;
                }
            }

            Assert.That(pulls.Split(' '), Does.Contain(Degree.Name(likeliest)), $"{mode}: after {colour}");
        }

        [TestCase("lydian")]
        [TestCase("phrygian")]
        [TestCase("double_harmonic")]
        [TestCase("hindu_pentatonic")]
        public void AnOrderFourMelodyKeepsToTheMode(string mode)
        {
            MarkovTable table = Table(mode);
            int mask = ModeScale.Mask(mode);
            GeneratorSettings settings = GeneratorSettings.Default;
            settings.Order = 4;
            settings.ChromaticWeight = 0f;
            var generator = new MelodyGenerator(table, settings, 2112);
            float[] distribution = new float[Degree.Count];
            int steps = 4000;
            int atFour = 0;
            for (int i = 0; i < steps; i++)
            {
                MelodyStep step = generator.Next(distribution);
                Assert.That(ModeScale.Has(mask, step.Degree), Is.True, $"step {i}: {Degree.Name(step.Degree)}");
                if (step.OrderUsed == 4)
                {
                    atFour++;
                }
            }

            // The figures are carried by the order-4 contexts; backing off should be rare.
            Assert.That(atFour, Is.GreaterThan(steps * 9 / 10), $"{atFour} of {steps} steps found their order-4 context");
        }
    }
}
