using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the cube coordinates (HexCoord, HexDirection): the three sum to zero, each
    /// direction's neighbour is one step away and its opposite comes back, the six are distinct,
    /// distance is a metric, and rounding gives a cell its own centre and a corner to one of the
    /// three cells that meet there.
    /// </summary>
    public class HexCoordTests
    {
        private static IEnumerable<HexCoord> Sample()
        {
            for (int q = -4; q <= 4; q++)
            {
                for (int r = -4; r <= 4; r++)
                {
                    yield return new HexCoord(q, r);
                }
            }
        }

        private static IEnumerable<HexDirection> Directions()
        {
            for (int d = 0; d < HexDirections.Count; d++)
            {
                yield return (HexDirection)d;
            }
        }

        [Test]
        public void CoordinatesSumToZero()
        {
            foreach (HexCoord cell in Sample())
            {
                Assert.That(cell.Q + cell.R + cell.S, Is.EqualTo(0), $"{cell}");
            }
        }

        [Test]
        public void DirectionsAreAsNamed()
        {
            Assert.That(HexDirections.Offset(HexDirection.PlusK), Is.EqualTo(new HexCoord(1, 0)));
            Assert.That(HexDirections.Offset(HexDirection.PlusM), Is.EqualTo(new HexCoord(1, -1)));
            Assert.That(HexDirections.Offset(HexDirection.PlusN), Is.EqualTo(new HexCoord(0, -1)));
            Assert.That(HexDirections.Offset(HexDirection.MinusK), Is.EqualTo(new HexCoord(-1, 0)));
            Assert.That(HexDirections.Offset(HexDirection.MinusM), Is.EqualTo(new HexCoord(-1, 1)));
            Assert.That(HexDirections.Offset(HexDirection.MinusN), Is.EqualTo(new HexCoord(0, 1)));
        }

        [Test]
        public void ANeighbourIsOneStepAwayAndItsOppositeComesBack()
        {
            foreach (HexCoord cell in Sample())
            {
                foreach (HexDirection direction in Directions())
                {
                    HexCoord neighbour = cell.Neighbour(direction);
                    Assert.That(HexCoord.Distance(cell, neighbour), Is.EqualTo(1), $"{cell} {direction}");
                    Assert.That(neighbour.Neighbour(HexDirections.Opposite(direction)), Is.EqualTo(cell), $"{cell} {direction} and back");
                    Assert.That((int)HexDirections.Opposite(direction), Is.EqualTo(((int)direction + 3) % 6));
                }
            }
        }

        [Test]
        public void TheSixNeighboursAreDistinct()
        {
            var seen = new HashSet<HexCoord>();
            foreach (HexDirection direction in Directions())
            {
                seen.Add(new HexCoord(2, -3).Neighbour(direction));
            }

            Assert.That(seen.Count, Is.EqualTo(6));
        }

        [Test]
        public void DistanceIsSymmetricAndObeysTheTriangleInequality()
        {
            var cells = new List<HexCoord>(Sample());
            for (int i = 0; i < cells.Count; i += 3)
            {
                for (int j = 0; j < cells.Count; j += 5)
                {
                    int ab = HexCoord.Distance(cells[i], cells[j]);
                    Assert.That(HexCoord.Distance(cells[j], cells[i]), Is.EqualTo(ab), $"{cells[i]} {cells[j]}");
                    Assert.That(ab == 0, Is.EqualTo(cells[i] == cells[j]), "distance 0 is the same cell");
                    for (int k = 0; k < cells.Count; k += 7)
                    {
                        Assert.That(ab, Is.LessThanOrEqualTo(HexCoord.Distance(cells[i], cells[k]) + HexCoord.Distance(cells[k], cells[j])),
                            $"{cells[i]} {cells[j]} by way of {cells[k]}");
                    }
                }
            }
        }

        [Test]
        public void EqualityAndHashingAgree()
        {
            var a = new HexCoord(3, -2);
            var b = new HexCoord(3, -2);
            Assert.That(a == b && a.Equals(b) && a.Equals((object)b) && !(a != b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a != new HexCoord(-2, 3), Is.True);
            Assert.That((a + new HexCoord(1, 1)) - new HexCoord(1, 1), Is.EqualTo(a));
        }

        [Test]
        public void RoundingACellsCentreGivesTheCell()
        {
            foreach (HexCoord cell in Sample())
            {
                Assert.That(HexCoord.Round(cell.Q, cell.R), Is.EqualTo(cell));
                Assert.That(HexCoord.Round(cell.Q + 0.2, cell.R - 0.1), Is.EqualTo(cell), $"near {cell}");
            }
        }

        [Test]
        public void APointNearACornerRoundsToOneOfTheThreeCellsThatMeetThere()
        {
            // The corner where a cell, its +K neighbour and its +M neighbour meet is the mean of the three.
            foreach (HexCoord cell in Sample())
            {
                for (int d = 0; d < HexDirections.Count; d++)
                {
                    HexCoord second = cell.Neighbour((HexDirection)d);
                    HexCoord third = cell.Neighbour((HexDirection)((d + 1) % HexDirections.Count));
                    double cornerQ = (cell.Q + second.Q + third.Q) / 3.0;
                    double cornerR = (cell.R + second.R + third.R) / 3.0;
                    for (int i = 0; i < 8; i++)
                    {
                        // Eight points round the corner, a twentieth of a cell from it.
                        double q = cornerQ + (Math.Cos(i * Math.PI / 4.0) * 0.05);
                        double r = cornerR + (Math.Sin(i * Math.PI / 4.0) * 0.05);
                        HexCoord rounded = HexCoord.Round(q, r);
                        Assert.That(rounded == cell || rounded == second || rounded == third, Is.True,
                            $"({q:0.000}, {r:0.000}) near the corner of {cell}, {second}, {third} rounded to {rounded}");
                    }
                }
            }
        }
    }
}
