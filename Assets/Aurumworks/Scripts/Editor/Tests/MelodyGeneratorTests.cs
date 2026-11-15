using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints; // AllocatingGCMemory, an extension on NUnit's constraints
using AllocIs = UnityEngine.TestTools.Constraints.Is;
using Is = NUnit.Framework.Is;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the generator, as the plan's M2 lists them, on the shipped tables: one seed
    /// is one melody; order 0 draws as the unigram and order 1 after 7 as the data; with no smoothing
    /// it plays only what the data heard; it backs off from a context missing or heard too seldom; a
    /// low temperature picks the likeliest note and ChromaticWeight 0 keeps to the mode; the
    /// distribution handed back is the one drawn from; and stepping allocates nothing.
    /// </summary>
    public class MelodyGeneratorTests
    {
        private const int Seven = 11; // the degree 7

        private static MarkovTable Table(string name = "european_ionian")
        {
            Assert.That(MarkovLibrary.TryGet(name, out MarkovTable table), Is.True, name);
            return table;
        }

        private static GeneratorSettings Settings(int order, float temperature = 1f, float smoothing = 0f, int minimumContext = 3,
            float chromaticWeight = 1f)
        {
            return new GeneratorSettings
            {
                Order = order,
                Temperature = temperature,
                Smoothing = smoothing,
                MinimumContext = minimumContext,
                ChromaticWeight = chromaticWeight,
            };
        }

        private static float Sum(float[] values)
        {
            float sum = 0f;
            foreach (float value in values)
            {
                sum += value;
            }

            return sum;
        }

        private static int ArgMax(ReadOnlySpan<int> counts, out int runnerUp)
        {
            int best = 0;
            for (int d = 1; d < counts.Length; d++)
            {
                if (counts[d] > counts[best])
                {
                    best = d;
                }
            }

            runnerUp = 0;
            for (int d = 0; d < counts.Length; d++)
            {
                if (d != best)
                {
                    runnerUp = Math.Max(runnerUp, counts[d]);
                }
            }

            return best;
        }

        [Test]
        public void TheSameSeedGivesTheSameSteps()
        {
            MarkovTable table = Table();
            var a = new MelodyGenerator(table, GeneratorSettings.Default, 1234);
            var b = new MelodyGenerator(table, GeneratorSettings.Default, 1234);
            var c = new MelodyGenerator(table, GeneratorSettings.Default, 1235);
            float[] da = new float[Degree.Count];
            float[] db = new float[Degree.Count];
            float[] dc = new float[Degree.Count];
            var first = new List<int>();
            int differ = 0;
            for (int i = 0; i < 1000; i++)
            {
                MelodyStep sa = a.Next(da);
                MelodyStep sb = b.Next(db);
                MelodyStep sc = c.Next(dc);
                Assert.That(sb.Degree, Is.EqualTo(sa.Degree), $"step {i}");
                Assert.That(sb.OrderUsed, Is.EqualTo(sa.OrderUsed), $"step {i}");
                Assert.That(db, Is.EqualTo(da), $"step {i}");
                differ += sc.Degree != sa.Degree ? 1 : 0;
                first.Add(sa.Degree);
            }

            Assert.That(differ, Is.GreaterThan(100), "another seed gives another melody");

            a.Reset(1234);
            for (int i = 0; i < 1000; i++)
            {
                Assert.That(a.Next(da).Degree, Is.EqualTo(first[i]), $"after Reset, step {i}");
            }
        }

        [Test]
        public void TheFirstStepIsFromTheFirstCounts()
        {
            MarkovTable table = Table();
            var generator = new MelodyGenerator(table, GeneratorSettings.Default, 7);
            float[] distribution = new float[Degree.Count];
            MelodyStep step = generator.Next(distribution);
            Assert.That(step.IsFirst, Is.True);
            Assert.That(step.OrderUsed, Is.EqualTo(0));
            Assert.That(step.ContextTotal, Is.EqualTo(table.Tunes));
            for (int d = 0; d < Degree.Count; d++)
            {
                Assert.That(distribution[d], Is.EqualTo(table.First(d) / (double)table.Tunes).Within(1e-6), Degree.Name(d));
            }

            Assert.That(generator.Next(distribution).IsFirst, Is.False);
            Assert.That(generator.History.Length, Is.EqualTo(2));
        }

        [Test]
        public void OrderZeroDrawsAsTheUnigram()
        {
            MarkovTable table = Table();
            var generator = new MelodyGenerator(table, Settings(0), 11);
            float[] distribution = new float[Degree.Count];
            int[] seen = new int[Degree.Count];
            generator.Next(distribution); // the first note, from First
            const int draws = 200000;
            for (int i = 0; i < draws; i++)
            {
                MelodyStep step = generator.Next(distribution);
                Assert.That(step.OrderUsed, Is.EqualTo(0));
                seen[step.Degree]++;
            }

            for (int d = 0; d < Degree.Count; d++)
            {
                Assert.That(seen[d] / (double)draws, Is.EqualTo(table.Unigram(d) / (double)table.Notes).Within(0.005), Degree.Name(d));
            }
        }

        [Test]
        public void OrderOneAfterSevenGoesToOneAsTheDataDoes()
        {
            MarkovTable table = Table();
            var generator = new MelodyGenerator(table, Settings(1), 21);
            float[] distribution = new float[Degree.Count];
            int toOne = 0;
            const int draws = 40000;
            for (int i = 0; i < draws; i++)
            {
                generator.SetHistory(new[] { Seven });
                MelodyStep step = generator.Next(distribution);
                Assert.That(step.OrderUsed, Is.EqualTo(1));
                Assert.That(step.ContextTotal, Is.EqualTo(35562));
                toOne += step.Degree == 0 ? 1 : 0;
            }

            Assert.That(distribution[0], Is.EqualTo(0.458).Within(0.01), "the probability handed back");
            Assert.That(toOne / (double)draws, Is.EqualTo(0.458).Within(0.01), "the share drawn");
        }

        [Test]
        public void WithNoSmoothingOrderOneOnlyPlaysPairsTheDataHeard()
        {
            MarkovTable table = Table();
            var generator = new MelodyGenerator(table, Settings(1), 31);
            float[] distribution = new float[Degree.Count];
            int previous = generator.Next(distribution).Degree;
            int atOrderOne = 0;
            for (int i = 0; i < 20000; i++)
            {
                MelodyStep step = generator.Next(distribution);
                if (step.OrderUsed == 1)
                {
                    atOrderOne++;
                    Assert.That(table.TryGetCounts(new[] { previous }, out ReadOnlySpan<int> counts), Is.True);
                    Assert.That(counts[step.Degree], Is.GreaterThan(0), $"{Degree.Name(previous)} → {Degree.Name(step.Degree)}");
                }

                previous = step.Degree;
            }

            Assert.That(atOrderOne, Is.GreaterThan(19000));
        }

        [Test]
        public void AtOrderFourEveryContextUsedWasHeardEnough()
        {
            MarkovTable table = Table();
            GeneratorSettings settings = Settings(4);
            var generator = new MelodyGenerator(table, settings, 41);
            float[] distribution = new float[Degree.Count];
            int[] usedOrders = new int[5];
            for (int i = 0; i < 20000; i++)
            {
                int[] before = generator.History.ToArray();
                MelodyStep step = generator.Next(distribution);
                usedOrders[step.OrderUsed]++;
                Assert.That(step.OrderAsked, Is.EqualTo(Math.Min(4, before.Length)));
                Assert.That(step.OrderUsed, Is.LessThanOrEqualTo(step.OrderAsked));
                if (step.OrderUsed > 0)
                {
                    int[] context = before.AsSpan(before.Length - step.OrderUsed).ToArray();
                    Assert.That(table.TryGetCounts(context, out ReadOnlySpan<int> counts), Is.True);
                    Assert.That(table.Total(context), Is.GreaterThanOrEqualTo(settings.MinimumContext).And.EqualTo(step.ContextTotal));
                    Assert.That(counts[step.Degree], Is.GreaterThan(0));
                }
            }

            Assert.That(usedOrders[4], Is.GreaterThan(15000), "mostly at order 4: " + string.Join(", ", usedOrders));
        }

        [Test]
        public void ItBacksOffFromAContextNeverHeard()
        {
            MarkovTable table = Table();
            int[] neverHeard = { 1, 1, 1, 1 }; // ♭2 four times
            Assert.That(table.TryGetCounts(neverHeard, out _), Is.False, "the context must be absent for the test to mean anything");
            var generator = new MelodyGenerator(table, Settings(4), 51);
            generator.SetHistory(neverHeard);
            MelodyStep step = generator.Next(new float[Degree.Count]);
            Assert.That(step.OrderAsked, Is.EqualTo(4));
            Assert.That(step.OrderUsed, Is.LessThan(4));
            Assert.That(step.IsFirst, Is.False);
        }

        [Test]
        public void ItBacksOffFromAContextHeardTooSeldom()
        {
            MarkovTable table = Table();
            int[] seldom = null;
            foreach (int[] context in table.Contexts(4))
            {
                if (table.Total(context) < 3)
                {
                    seldom = context;
                    break;
                }
            }

            Assert.That(seldom, Is.Not.Null, "european_ionian has an order-4 context heard fewer than 3 times");
            var generator = new MelodyGenerator(table, Settings(4, minimumContext: 3), 61);
            generator.SetHistory(seldom);
            Assert.That(generator.Next(new float[Degree.Count]).OrderUsed, Is.LessThan(4));

            generator.Settings = Settings(4, minimumContext: 1);
            generator.SetHistory(seldom);
            Assert.That(generator.Next(new float[Degree.Count]).OrderUsed, Is.EqualTo(4), "with MinimumContext 1 it is used");
        }

        [Test]
        public void ALowTemperaturePicksTheLikeliestNote()
        {
            MarkovTable table = Table();
            var generator = new MelodyGenerator(table, Settings(1, temperature: 0.01f), 71);
            float[] distribution = new float[Degree.Count];
            int tried = 0;
            foreach (int[] context in table.Contexts(1))
            {
                table.TryGetCounts(context, out ReadOnlySpan<int> counts);
                int best = ArgMax(counts, out int runnerUp);
                if (table.Total(context) < 3 || runnerUp >= 0.9 * counts[best])
                {
                    continue; // too close to call at this temperature
                }

                tried++;
                for (int i = 0; i < 20; i++)
                {
                    generator.SetHistory(context);
                    Assert.That(generator.Next(distribution).Degree, Is.EqualTo(best), $"after {Degree.Name(context[0])}");
                }

                Assert.That(distribution[best], Is.GreaterThan(0.999f), $"after {Degree.Name(context[0])}");
            }

            Assert.That(tried, Is.GreaterThanOrEqualTo(6));
        }

        [Test]
        public void ChromaticWeightZeroNeverLeavesTheMode()
        {
            float[] distribution = new float[Degree.Count];
            foreach (string name in MarkovLibrary.Names)
            {
                MarkovTable table = Table(name);
                int mask = ModeScale.Mask(table.Mode);
                var generator = new MelodyGenerator(table, Settings(2, chromaticWeight: 0f), 81);
                for (int i = 0; i < 3000; i++)
                {
                    MelodyStep step = generator.Next(distribution);
                    Assert.That(ModeScale.Has(mask, step.Degree), Is.True, $"{name}: {Degree.Name(step.Degree)}");
                    for (int d = 0; d < Degree.Count; d++)
                    {
                        if (!ModeScale.Has(mask, d))
                        {
                            Assert.That(distribution[d], Is.EqualTo(0f), $"{name}: {Degree.Name(d)}");
                        }
                    }
                }
            }
        }

        [Test]
        public void TheDistributionHandedBackIsTheOneDrawnFrom()
        {
            MarkovTable table = Table();
            var generator = new MelodyGenerator(table, Settings(3), 91);
            float[] distribution = new float[Degree.Count];
            for (int i = 0; i < 2000; i++)
            {
                int[] before = generator.History.ToArray();
                MelodyStep step = generator.Next(distribution);
                Assert.That(Sum(distribution), Is.EqualTo(1f).Within(1e-5f), $"step {i}");
                Assert.That(distribution[step.Degree], Is.GreaterThan(0f), $"step {i}");
                if (!step.IsFirst)
                {
                    // At temperature 1, no smoothing and ChromaticWeight 1: the context's counts over their total.
                    ReadOnlySpan<int> context = before.AsSpan(before.Length - step.OrderUsed);
                    table.TryGetCounts(context, out ReadOnlySpan<int> counts);
                    for (int d = 0; d < Degree.Count; d++)
                    {
                        Assert.That(distribution[d], Is.EqualTo(counts[d] / (double)step.ContextTotal).Within(1e-6), $"step {i}, {Degree.Name(d)}");
                    }
                }
            }
        }

        [Test]
        public void SmoothingLetsEveryNoteFollow()
        {
            MarkovTable table = Table();
            var generator = new MelodyGenerator(table, Settings(1, smoothing: 1f), 101);
            float[] distribution = new float[Degree.Count];
            generator.SetHistory(new[] { Seven });
            generator.Next(distribution);
            foreach (float p in distribution)
            {
                Assert.That(p, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void SteppingAllocatesNothing()
        {
            // Unity's own GC.Alloc recorder: Mono's GC.GetAllocatedBytesForCurrentThread counts nothing.
            var generator = new MelodyGenerator(Table(), Settings(4), 111);
            float[] distribution = new float[Degree.Count];
            for (int i = 0; i < 100; i++)
            {
                generator.Next(distribution);
            }

            Assert.That(() =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    generator.Next(distribution);
                }
            }, AllocIs.Not.AllocatingGCMemory());

            // The recorder must see an allocation, or the check above proves nothing.
            Assert.That(() => GC.KeepAlive(new byte[4096]), AllocIs.AllocatingGCMemory());
        }

        [Test]
        public void WhatIsWrongIsRefused()
        {
            var generator = new MelodyGenerator(Table(), GeneratorSettings.Default, 1);
            Assert.That(() => generator.Next(new float[Degree.Count - 1]), Throws.ArgumentException);
            Assert.That(() => generator.SetHistory(new[] { 0, 12 }), Throws.ArgumentException);
            Assert.That(() => new MelodyGenerator(null, GeneratorSettings.Default, 1), Throws.ArgumentNullException);
        }

        [Test]
        public void SettingsAreClamped()
        {
            GeneratorSettings clamped = new GeneratorSettings
            {
                Order = 9,
                Temperature = 0f,
                Smoothing = -1f,
                MinimumContext = -2,
                ChromaticWeight = float.NaN,
            }.Clamped();
            Assert.That(clamped.Order, Is.EqualTo(4));
            Assert.That(clamped.Temperature, Is.EqualTo(GeneratorSettings.LowestTemperature));
            Assert.That(clamped.Smoothing, Is.EqualTo(0f));
            Assert.That(clamped.MinimumContext, Is.EqualTo(0));
            Assert.That(clamped.ChromaticWeight, Is.EqualTo(1f));
            Assert.That(new GeneratorSettings { Order = -1 }.Clamped().Order, Is.EqualTo(0));
        }

        [Test]
        public void TheHistoryKeepsTheLastFour()
        {
            var generator = new MelodyGenerator(Table(), GeneratorSettings.Default, 1);
            Assert.That(generator.MaxOrder, Is.EqualTo(4));
            generator.SetHistory(new[] { 0, 2, 4, 5, 7, 9 });
            Assert.That(generator.History.ToArray(), Is.EqualTo(new[] { 4, 5, 7, 9 }));
            MelodyStep step = generator.Next(new float[Degree.Count]);
            Assert.That(generator.History.ToArray(), Is.EqualTo(new[] { 5, 7, 9, step.Degree }));
            generator.SetHistory(ReadOnlySpan<int>.Empty);
            Assert.That(generator.Next(new float[Degree.Count]).IsFirst, Is.True);
        }
    }
}
