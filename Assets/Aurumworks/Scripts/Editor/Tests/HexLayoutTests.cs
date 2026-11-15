using System;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the layout (HexLayout), in both orientations: picking a cell's own position
    /// gives the cell, and so does any point well inside it; neighbours sit √3 · Size + Gap apart;
    /// a centred board's middle is the origin; the k axis runs east in Pointy and north in Flat;
    /// and Flat with Size 1 and Gap 0 spaces cells as the old grid did (1.5 one way, √3 the other).
    /// </summary>
    public class HexLayoutTests
    {
        private const float Tolerance = 1e-4f;

        private static readonly HexOrientation[] _orientations = { HexOrientation.Flat, HexOrientation.Pointy };

        private static float Distance(HexPoint a, HexPoint b) =>
            (float)Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Z - b.Z) * (a.Z - b.Z)));

        [Test]
        public void PickingACellsPositionGivesTheCell()
        {
            var shape = new HexShape(7, 5, 3);
            int step = 0;
            foreach (HexOrientation orientation in _orientations)
            {
                foreach (float gap in new[] { 0f, 0.35f })
                {
                    HexLayout layout = HexLayout.Centred(shape, orientation, 1.3f, gap);
                    foreach (HexCoord cell in shape.Cells)
                    {
                        HexPoint point = layout.Position(cell);
                        Assert.That(layout.Pick(point.X, point.Z), Is.EqualTo(cell), $"{orientation}, gap {gap}: {cell}");

                        // Anywhere within the inscribed circle, less a little, is the cell's too.
                        // A different bearing and distance for each cell, by the golden angle.
                        step++;
                        double angle = step * 2.399963;
                        double radius = ((step % 5) + 1) / 5.0 * 0.49 * layout.Spacing;
                        float x = point.X + (float)(Math.Cos(angle) * radius);
                        float z = point.Z + (float)(Math.Sin(angle) * radius);
                        Assert.That(layout.Pick(x, z), Is.EqualTo(cell), $"{orientation}, gap {gap}: inside {cell}");
                    }
                }
            }
        }

        [Test]
        public void NeighboursSitRootThreeSizesAndAGapApart()
        {
            var shape = new HexShape(7, 5, 3);
            foreach (HexOrientation orientation in _orientations)
            {
                const float Size = 0.8f;
                const float Gap = 0.25f;
                HexLayout layout = HexLayout.Centred(shape, orientation, Size, Gap);
                float expected = ((float)Math.Sqrt(3.0) * Size) + Gap;
                Assert.That(layout.Spacing, Is.EqualTo(expected).Within(Tolerance));
                foreach (HexCoord cell in shape.Cells)
                {
                    for (int d = 0; d < HexDirections.Count; d++)
                    {
                        float distance = Distance(layout.Position(cell), layout.Position(cell.Neighbour((HexDirection)d)));
                        Assert.That(distance, Is.EqualTo(expected).Within(Tolerance), $"{orientation}: {cell} {(HexDirection)d}");
                    }
                }
            }
        }

        [Test]
        public void ACentredBoardsMiddleIsTheOrigin()
        {
            foreach (HexOrientation orientation in _orientations)
            {
                foreach (HexShape shape in new[] { new HexShape(7, 5, 3), new HexShape(5, 5, 5), new HexShape(1, 1, 1), new HexShape(7, 4, 1) })
                {
                    HexLayout layout = HexLayout.Centred(shape, orientation, 1f, 0.1f);
                    double sumX = 0.0, sumZ = 0.0;
                    foreach (HexCoord cell in shape.Cells)
                    {
                        HexPoint point = layout.Position(cell);
                        sumX += point.X;
                        sumZ += point.Z;
                    }

                    Assert.That(sumX / shape.Count, Is.EqualTo(0.0).Within(Tolerance), $"{orientation} {shape.K} × {shape.M} × {shape.N}: x");
                    Assert.That(sumZ / shape.Count, Is.EqualTo(0.0).Within(Tolerance), $"{orientation} {shape.K} × {shape.M} × {shape.N}: z");
                }
            }
        }

        [Test]
        public void TheKAxisRunsEastInPointyAndNorthInFlat()
        {
            var origin = new HexCoord(0, 0);
            var pointy = new HexLayout(HexOrientation.Pointy, 1f, 0f);
            HexPoint k = pointy.Position(origin.Neighbour(HexDirection.PlusK));
            HexPoint m = pointy.Position(origin.Neighbour(HexDirection.PlusM));
            HexPoint n = pointy.Position(origin.Neighbour(HexDirection.PlusN));
            Assert.That(k.X, Is.EqualTo(Math.Sqrt(3.0)).Within(Tolerance), "Pointy: +K is east");
            Assert.That(k.Z, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(m.X > 0f && m.Z > 0f, Is.True, "Pointy: +M is north-east");
            Assert.That(n.X < 0f && n.Z > 0f, Is.True, "Pointy: +N is north-west");

            var flat = new HexLayout(HexOrientation.Flat, 1f, 0f);
            k = flat.Position(origin.Neighbour(HexDirection.PlusK));
            Assert.That(k.X, Is.EqualTo(0.0).Within(Tolerance), "Flat: +K is north");
            Assert.That(k.Z, Is.EqualTo(Math.Sqrt(3.0)).Within(Tolerance));
        }

        [Test]
        public void FlatAtSizeOneSpacesCellsAsTheOldGridDid()
        {
            // The old grid: columns 1.5 apart in x, cells √3 apart in z, every other column half a cell up.
            var layout = new HexLayout(HexOrientation.Flat, 1f, 0f);
            var origin = new HexCoord(0, 0);
            HexPoint north = layout.Position(origin.Neighbour(HexDirection.PlusK));
            Assert.That(Math.Abs(north.X), Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(Math.Abs(north.Z), Is.EqualTo(Math.Sqrt(3.0)).Within(Tolerance));
            foreach (HexDirection direction in new[] { HexDirection.PlusM, HexDirection.PlusN, HexDirection.MinusM, HexDirection.MinusN })
            {
                HexPoint point = layout.Position(origin.Neighbour(direction));
                Assert.That(Math.Abs(point.X), Is.EqualTo(1.5).Within(Tolerance), $"{direction}: the next column");
                Assert.That(Math.Abs(point.Z), Is.EqualTo(Math.Sqrt(3.0) / 2.0).Within(Tolerance), $"{direction}: half a cell up or down");
            }
        }
    }
}
