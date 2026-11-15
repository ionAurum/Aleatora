using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the board's shape (HexShape), for every k, m and n from 1 to 7: the count
    /// formula against the cells, the six sides' lengths, Contains and Cells agreeing, IndexOf dense
    /// and unique, every cell connected to every other, and neighbourliness symmetric.
    /// </summary>
    public class HexShapeTests
    {
        private const int Longest = 7;

        private static IEnumerable<HexShape> Shapes()
        {
            for (int k = 1; k <= Longest; k++)
            {
                for (int m = 1; m <= Longest; m++)
                {
                    for (int n = 1; n <= Longest; n++)
                    {
                        yield return new HexShape(k, m, n);
                    }
                }
            }
        }

        private static string Name(HexShape shape) => $"{shape.K} × {shape.M} × {shape.N}";

        [Test]
        public void TheCountFormulaMatchesTheCells()
        {
            foreach (HexShape shape in Shapes())
            {
                int formula = (shape.K * shape.M) + (shape.M * shape.N) + (shape.N * shape.K) - shape.K - shape.M - shape.N + 1;
                Assert.That(shape.Count, Is.EqualTo(formula), Name(shape));
                Assert.That(shape.Cells.Count, Is.EqualTo(formula), Name(shape));
            }
        }

        [Test]
        public void KnownShapes()
        {
            Assert.That(new HexShape(1, 1, 1).Count, Is.EqualTo(1));
            Assert.That(new HexShape(1, 1, 1).Cells[0], Is.EqualTo(new HexCoord(0, 0)));
            Assert.That(new HexShape(5, 5, 5).Count, Is.EqualTo(61), "the default board");
            for (int k = 1; k <= Longest; k++)
            {
                for (int m = 1; m <= Longest; m++)
                {
                    Assert.That(new HexShape(k, m, 1).Count, Is.EqualTo(k * m), $"{k} × {m} × 1 is a parallelogram");
                }
            }
        }

        [Test]
        public void ASideShorterThanOneIsRefused()
        {
            Assert.That(() => new HexShape(0, 3, 3), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new HexShape(3, 0, 3), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new HexShape(3, 3, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheSixSidesHaveKMNKMNCells()
        {
            foreach (HexShape shape in Shapes())
            {
                int firstR = 0, lastR = 0, firstS = 0, lastS = 0, firstQ = 0, lastQ = 0;
                foreach (HexCoord cell in shape.Cells)
                {
                    firstR += cell.R == 0 ? 1 : 0;
                    lastR += cell.R == shape.M + shape.N - 2 ? 1 : 0;
                    firstS += cell.S == 0 ? 1 : 0;
                    lastS += cell.S == -(shape.K + shape.N - 2) ? 1 : 0;
                    firstQ += cell.Q == -(shape.M - 1) ? 1 : 0;
                    lastQ += cell.Q == shape.K - 1 ? 1 : 0;
                }

                Assert.That(new[] { firstR, lastR }, Is.All.EqualTo(shape.K), Name(shape) + ": the sides of constant R");
                Assert.That(new[] { firstS, lastS }, Is.All.EqualTo(shape.M), Name(shape) + ": the sides of constant S");
                Assert.That(new[] { firstQ, lastQ }, Is.All.EqualTo(shape.N), Name(shape) + ": the sides of constant Q");
            }
        }

        [Test]
        public void ContainsAndCellsAgree()
        {
            foreach (HexShape shape in Shapes())
            {
                var cells = new HashSet<HexCoord>(shape.Cells);
                Assert.That(cells.Count, Is.EqualTo(shape.Count), Name(shape) + ": no cell twice");
                for (int q = -Longest - 1; q <= Longest + 1; q++)
                {
                    for (int r = -2; r <= (2 * Longest) + 1; r++)
                    {
                        var cell = new HexCoord(q, r);
                        Assert.That(shape.Contains(cell), Is.EqualTo(cells.Contains(cell)), $"{Name(shape)}: {cell}");
                    }
                }
            }
        }

        [Test]
        public void IndexOfIsDenseAndInTheOrderOfCells()
        {
            foreach (HexShape shape in Shapes())
            {
                for (int i = 0; i < shape.Count; i++)
                {
                    Assert.That(shape.IndexOf(shape.Cells[i]), Is.EqualTo(i), $"{Name(shape)}: {shape.Cells[i]}");
                    if (i > 0)
                    {
                        HexCoord before = shape.Cells[i - 1];
                        HexCoord cell = shape.Cells[i];
                        Assert.That(cell.R > before.R || (cell.R == before.R && cell.Q > before.Q), Is.True,
                            $"{Name(shape)}: cells run by R, then Q");
                    }
                }

                Assert.That(shape.IndexOf(new HexCoord(0, -1)), Is.EqualTo(-1), Name(shape) + ": off the board");
                Assert.That(shape.IndexOf(new HexCoord(shape.K, 0)), Is.EqualTo(-1), Name(shape) + ": off the board");
            }
        }

        [Test]
        public void EveryCellIsConnectedToEveryOther()
        {
            foreach (HexShape shape in Shapes())
            {
                var reached = new bool[shape.Count];
                var queue = new Queue<HexCoord>();
                queue.Enqueue(shape.Cells[0]);
                reached[0] = true;
                int count = 1;
                while (queue.Count > 0)
                {
                    HexCoord cell = queue.Dequeue();
                    for (int d = 0; d < HexDirections.Count; d++)
                    {
                        int index = shape.IndexOf(cell.Neighbour((HexDirection)d));
                        if (index >= 0 && !reached[index])
                        {
                            reached[index] = true;
                            count++;
                            queue.Enqueue(shape.Cells[index]);
                        }
                    }
                }

                Assert.That(count, Is.EqualTo(shape.Count), Name(shape));
            }
        }

        [Test]
        public void NeighbourlinessIsSymmetric()
        {
            foreach (HexShape shape in Shapes())
            {
                foreach (HexCoord cell in shape.Cells)
                {
                    for (int d = 0; d < HexDirections.Count; d++)
                    {
                        var direction = (HexDirection)d;
                        HexCoord neighbour = cell.Neighbour(direction);
                        if (shape.Contains(neighbour))
                        {
                            Assert.That(neighbour.Neighbour(HexDirections.Opposite(direction)), Is.EqualTo(cell), $"{Name(shape)}: {cell} {direction}");
                        }
                    }
                }
            }
        }
    }
}
