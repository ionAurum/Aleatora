using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the board's notes (CellNotes, the emitter plan's EM-1): the probabilities
    /// from a set's unigram; the share-out by largest remainder; the same seed the same board;
    /// Scattered and Clustered holding the counts; Clustered's regions connected; the Axes layout;
    /// the centre cell; the octaves by ring, both ways; the blue scaled to the likeliest; the notes.
    /// </summary>
    public class CellNotesTests
    {
        private static readonly int _ionian = ModeScale.Mask("ionian");

        // european_ionian's unigram, in the mode only (chromatic weight 0).
        private static double[] Ionian()
        {
            Assert.That(MarkovLibrary.TryGet("european_ionian", out MarkovTable table), Is.True);
            var counts = new int[Degree.Count];
            for (int d = 0; d < counts.Length; d++)
            {
                counts[d] = table.Unigram(d);
            }

            return CellNotes.Probabilities(counts, ModeScale.Mask(table.Mode), 0.0);
        }

        private static CellNotes Board(HexShape shape, CellLayout layout, ulong seed, int octaves = 3, bool outwardLower = false) =>
            new(shape, Ionian(), _ionian, layout, seed, 2, 2, octaves, outwardLower);

        private static string Name(HexShape shape) => $"{shape.K} × {shape.M} × {shape.N}";

        [Test]
        public void TheProbabilitiesKeepToTheModeAtChromaticWeightZero()
        {
            var counts = new[] { 10, 1, 8, 1, 6, 4, 1, 9, 1, 3, 1, 2 };
            double[] none = CellNotes.Probabilities(counts, _ionian, 0.0);
            double[] all = CellNotes.Probabilities(counts, _ionian, 1.0);
            double sumNone = 0.0;
            double sumAll = 0.0;
            for (int d = 0; d < Degree.Count; d++)
            {
                sumNone += none[d];
                sumAll += all[d];
                Assert.That(none[d] > 0.0, Is.EqualTo(ModeScale.Has(_ionian, d)), $"degree {Degree.Name(d)} at weight 0");
                Assert.That(all[d], Is.EqualTo(counts[d] / 47.0).Within(1e-12), $"degree {Degree.Name(d)} at weight 1");
            }

            Assert.That(sumNone, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(sumAll, Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void ProbabilitiesOfNothingAreTheModesDegreesAsLikely()
        {
            double[] equal = CellNotes.Probabilities(new int[Degree.Count], _ionian, 0.0);
            for (int d = 0; d < Degree.Count; d++)
            {
                Assert.That(equal[d], Is.EqualTo(ModeScale.Has(_ionian, d) ? 1.0 / 7.0 : 0.0).Within(1e-12));
            }
        }

        [Test]
        public void EuropeanIonianSharesOut61CellsAsThePlanSays()
        {
            int[] counts = CellNotes.ShareOut(Ionian(), 61);
            var expected = new[] { 13, 0, 9, 0, 11, 6, 0, 12, 0, 5, 0, 5 };
            Assert.That(counts, Is.EqualTo(expected));
        }

        [Test]
        public void TheShareOutIsLargestRemainder()
        {
            var rng = new Rng(7);
            var probabilities = new double[Degree.Count];
            for (int trial = 0; trial < 500; trial++)
            {
                double sum = 0.0;
                for (int d = 0; d < Degree.Count; d++)
                {
                    probabilities[d] = rng.Next(4) == 0 ? 0.0 : rng.NextDouble();
                    sum += probabilities[d];
                }

                if (sum <= 0.0)
                {
                    continue;
                }

                int cells = 1 + rng.Next(300);
                int[] counts = CellNotes.ShareOut(probabilities, cells);
                int total = 0;
                for (int d = 0; d < Degree.Count; d++)
                {
                    double quota = cells * probabilities[d] / sum;
                    total += counts[d];
                    Assert.That(counts[d], Is.EqualTo(Math.Floor(quota)).Or.EqualTo(Math.Floor(quota) + 1), $"trial {trial}, degree {d}");
                    if (probabilities[d] <= 0.0)
                    {
                        Assert.That(counts[d], Is.EqualTo(0), $"trial {trial}: a degree that weighs nothing has no cells");
                    }
                }

                Assert.That(total, Is.EqualTo(cells), $"trial {trial}");
            }
        }

        [Test]
        public void TheSameSeedGivesTheSameBoardAndAnotherAnother()
        {
            var shape = new HexShape(5, 5, 5);
            foreach (CellLayout layout in new[] { CellLayout.Scattered, CellLayout.Clustered })
            {
                CellNotes a = Board(shape, layout, 2112);
                CellNotes b = Board(shape, layout, 2112);
                CellNotes c = Board(shape, layout, 2113);
                bool differs = false;
                for (int i = 0; i < shape.Count; i++)
                {
                    Assert.That(b.DegreeAt(i), Is.EqualTo(a.DegreeAt(i)), $"{layout}, cell {i}");
                    differs |= c.DegreeAt(i) != a.DegreeAt(i);
                }

                Assert.That(differs, Is.True, $"{layout}: seed 2113 lays out another board");
            }
        }

        [Test]
        public void ScatteredAndClusteredHoldTheCounts()
        {
            foreach (HexShape shape in new[] { new HexShape(5, 5, 5), new HexShape(3, 5, 7), new HexShape(7, 4, 1), new HexShape(1, 1, 1), new HexShape(9, 9, 9) })
            {
                int[] expected = CellNotes.ShareOut(Ionian(), shape.Count);
                foreach (CellLayout layout in new[] { CellLayout.Scattered, CellLayout.Clustered })
                {
                    for (ulong seed = 0; seed < 20; seed++)
                    {
                        CellNotes notes = Board(shape, layout, seed);
                        var counts = new int[Degree.Count];
                        for (int i = 0; i < shape.Count; i++)
                        {
                            counts[notes.DegreeAt(i)]++;
                        }

                        Assert.That(counts, Is.EqualTo(expected), $"{Name(shape)}, {layout}, seed {seed}");
                        for (int d = 0; d < Degree.Count; d++)
                        {
                            Assert.That(notes.CellsOf(d), Is.EqualTo(expected[d]));
                        }
                    }
                }
            }
        }

        [Test]
        public void ClusteredRegionsAreWholeOnNearlyEveryBoard()
        {
            foreach (HexShape shape in new[] { new HexShape(5, 5, 5), new HexShape(3, 5, 7), new HexShape(9, 9, 9) })
            {
                const int Boards = 200;
                int whole = 0;
                for (ulong seed = 0; seed < Boards; seed++)
                {
                    CellNotes notes = Board(shape, CellLayout.Clustered, seed);
                    whole += Pieces(shape, notes) == 0 ? 1 : 0;
                }

                TestContext.WriteLine($"{Name(shape)}: {whole} of {Boards} boards with every region whole");
                Assert.That(whole, Is.GreaterThanOrEqualTo(Boards * 95 / 100), Name(shape));
            }
        }

        [Test]
        public void TheLargestBoardLaysOutQuickly()
        {
            var shape = new HexShape(GridTuning.LongestSide, GridTuning.LongestSide, GridTuning.LongestSide);
            var watch = Stopwatch.StartNew();
            CellNotes notes = Board(shape, CellLayout.Clustered, 2112);
            watch.Stop();
            TestContext.WriteLine($"40 × 40 × 40 ({shape.Count} cells) clustered in {watch.ElapsedMilliseconds} ms, {Pieces(shape, notes)} extra pieces");
            Assert.That(watch.ElapsedMilliseconds, Is.LessThan(3000));
        }

        [Test]
        public void TheAxesPutTheRootInTheCentreAndStepByTheirIntervals()
        {
            var shape = new HexShape(5, 5, 5);
            CellNotes notes = Board(shape, CellLayout.Axes, 1);
            HexCoord centre = notes.Centre;
            int At(HexCoord cell) => notes.DegreeAt(shape.IndexOf(cell));
            Assert.That(At(centre), Is.EqualTo(0), "1 on the centre");
            Assert.That(At(centre.Neighbour(HexDirection.PlusK)), Is.EqualTo(4), "+K a third: 3");
            Assert.That(At(centre.Neighbour(HexDirection.PlusN)), Is.EqualTo(4), "+N a third: 3");
            Assert.That(At(centre.Neighbour(HexDirection.PlusM)), Is.EqualTo(7), "+M a fifth: 5");
            Assert.That(At(centre.Neighbour(HexDirection.MinusK)), Is.EqualTo(9), "−K a third down: 6");
            Assert.That(At(centre.Neighbour(HexDirection.MinusM)), Is.EqualTo(5), "−M a fifth down: 4");

            // Every triad of the mode a triangle: a cell, its +K and its +M neighbours.
            foreach (HexCoord cell in shape.Cells)
            {
                HexCoord third = cell.Neighbour(HexDirection.PlusK);
                HexCoord fifth = cell.Neighbour(HexDirection.PlusM);
                if (shape.Contains(third) && shape.Contains(fifth))
                {
                    int root = Array.IndexOf(new[] { 0, 2, 4, 5, 7, 9, 11 }, At(cell));
                    Assert.That(At(third), Is.EqualTo(new[] { 0, 2, 4, 5, 7, 9, 11 }[(root + 2) % 7]), $"{cell}'s third");
                    Assert.That(At(fifth), Is.EqualTo(new[] { 0, 2, 4, 5, 7, 9, 11 }[(root + 4) % 7]), $"{cell}'s fifth");
                }
            }
        }

        [Test]
        public void TheCentreCellIsOnTheBoardAndInTheMiddle()
        {
            Assert.That(CellNotes.CentreCell(new HexShape(5, 5, 5)), Is.EqualTo(new HexCoord(0, 4)));
            Assert.That(CellNotes.CentreCell(new HexShape(1, 1, 1)), Is.EqualTo(new HexCoord(0, 0)));
            for (int k = 1; k <= 7; k++)
            {
                for (int m = 1; m <= 7; m++)
                {
                    for (int n = 1; n <= 7; n++)
                    {
                        var shape = new HexShape(k, m, n);
                        Assert.That(shape.Contains(CellNotes.CentreCell(shape)), Is.True, Name(shape));
                    }
                }
            }
        }

        [Test]
        public void TheOctavesShareTheRingsOutwardOrInward()
        {
            var shape = new HexShape(5, 5, 5);
            CellNotes up = Board(shape, CellLayout.Scattered, 1);
            CellNotes down = Board(shape, CellLayout.Scattered, 1, outwardLower: true);
            Assert.That(up.Rings, Is.EqualTo(5));
            var byRing = new[] { 0, 0, 1, 1, 2 };
            for (int i = 0; i < shape.Count; i++)
            {
                int ring = HexCoord.Distance(shape.Cells[i], up.Centre);
                Assert.That(up.OctaveAt(i), Is.EqualTo(byRing[ring]), $"ring {ring}");
                Assert.That(down.OctaveAt(i), Is.EqualTo(2 - byRing[ring]), $"ring {ring}, outward lower");
            }

            Assert.That(Board(new HexShape(1, 1, 1), CellLayout.Scattered, 1).OctaveAt(0), Is.EqualTo(0), "one cell: the lowest octave");
            var uneven = new HexShape(3, 5, 7);
            CellNotes notes = Board(uneven, CellLayout.Scattered, 1, octaves: 4);
            var seen = new HashSet<int>();
            for (int i = 0; i < uneven.Count; i++)
            {
                int ring = HexCoord.Distance(uneven.Cells[i], notes.Centre);
                Assert.That(notes.OctaveAt(i), Is.EqualTo(ring * 4 / notes.Rings), $"3 × 5 × 7, ring {ring}");
                seen.Add(notes.OctaveAt(i));
            }

            Assert.That(seen.Count, Is.EqualTo(Math.Min(4, notes.Rings)), "every octave has its rings");
        }

        [Test]
        public void TheBlueIsScaledToTheLikeliestButTheRoot()
        {
            CellNotes notes = Board(new HexShape(5, 5, 5), CellLayout.Scattered, 1);
            Assert.That(notes.Blue(0), Is.EqualTo(0f), "the root is red, not blue");
            Assert.That(notes.Blue(7), Is.EqualTo(1f), "5, the likeliest but the root, is full blue");
            Assert.That(notes.Blue(4), Is.EqualTo((float)(notes.Probability(4) / notes.Probability(7))).Within(1e-6f), "3 in proportion");
            Assert.That(notes.Blue(1), Is.EqualTo(0f), "♭2, never on the board, is white");
        }

        [Test]
        public void TheNotesAreTheTonicInItsOctavePlusTheDegree()
        {
            Assert.That(CellNotes.NoteOf(0, 3, 0, 0), Is.EqualTo(48), "C3");
            Assert.That(CellNotes.NoteOf(0, 4, 0, 0), Is.EqualTo(60), "C4");
            Assert.That(CellNotes.NoteOf(9, 3, 2, 11), Is.EqualTo(92), "A3 two octaves up, plus 7");
            Assert.That(CellNotes.NoteOf(0, 8, 7, 11), Is.LessThanOrEqualTo(127), "kept below 128");
            Assert.That(CellNotes.NoteOf(0, -1, 0, 0), Is.EqualTo(0), "C−1");
            var shape = new HexShape(5, 5, 5);
            CellNotes notes = Board(shape, CellLayout.Scattered, 1);
            int centre = shape.IndexOf(notes.Centre);
            Assert.That(notes.Note(centre, 2, 3), Is.EqualTo(50 + notes.DegreeAt(centre)), "the centre in the lowest octave, tonic D");
        }

        // How many more pieces the regions come in than one a degree.
        private static int Pieces(HexShape shape, CellNotes notes)
        {
            var seen = new bool[shape.Count];
            var regions = new int[Degree.Count];
            var work = new Stack<int>();
            for (int i = 0; i < shape.Count; i++)
            {
                if (seen[i])
                {
                    continue;
                }

                int degree = notes.DegreeAt(i);
                regions[degree]++;
                seen[i] = true;
                work.Push(i);
                while (work.Count > 0)
                {
                    HexCoord cell = shape.Cells[work.Pop()];
                    for (int d = 0; d < HexDirections.Count; d++)
                    {
                        int next = shape.IndexOf(cell.Neighbour((HexDirection)d));
                        if (next >= 0 && !seen[next] && notes.DegreeAt(next) == degree)
                        {
                            seen[next] = true;
                            work.Push(next);
                        }
                    }
                }
            }

            int extra = 0;
            foreach (int count in regions)
            {
                extra += Math.Max(0, count - 1);
            }

            return extra;
        }
    }
}
