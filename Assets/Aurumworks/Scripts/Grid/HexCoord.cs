using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// A hexagon's place on the board, in cube coordinates: Q + R + S = 0, stored as Q and R (Amit
    /// Patel's Hexagonal Grids, redblobgames.com/grids/hexagons). No offset coordinates anywhere, so
    /// a neighbour is the same step from every cell.
    /// </summary>
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        /// <summary>Makes the coordinate (q, r, −q − r).</summary>
        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        /// <summary>The first cube coordinate: it grows along the k axis.</summary>
        public int Q { get; }

        /// <summary>The second cube coordinate.</summary>
        public int R { get; }

        /// <summary>The third cube coordinate, −Q − R.</summary>
        public int S => -Q - R;

        public static HexCoord operator +(HexCoord a, HexCoord b) => new(a.Q + b.Q, a.R + b.R);

        public static HexCoord operator -(HexCoord a, HexCoord b) => new(a.Q - b.Q, a.R - b.R);

        public static bool operator ==(HexCoord a, HexCoord b) => a.Q == b.Q && a.R == b.R;

        public static bool operator !=(HexCoord a, HexCoord b) => !(a == b);

        /// <summary>The cell one step that way.</summary>
        public HexCoord Neighbour(HexDirection direction) => this + HexDirections.Offset(direction);

        /// <summary>How many steps from one cell to the other.</summary>
        public static int Distance(HexCoord a, HexCoord b)
        {
            HexCoord d = a - b;
            return (Math.Abs(d.Q) + Math.Abs(d.R) + Math.Abs(d.S)) / 2;
        }

        /// <summary>
        /// The cell a fractional coordinate falls in (cube rounding): each of the three is rounded,
        /// and the one that moved furthest is set from the other two, so they still sum to zero.
        /// </summary>
        public static HexCoord Round(double q, double r)
        {
            double s = -q - r;
            double roundQ = Math.Round(q);
            double roundR = Math.Round(r);
            double roundS = Math.Round(s);
            double movedQ = Math.Abs(roundQ - q);
            double movedR = Math.Abs(roundR - r);
            double movedS = Math.Abs(roundS - s);
            if (movedQ > movedR && movedQ > movedS)
            {
                roundQ = -roundR - roundS;
            }
            else if (movedR > movedS)
            {
                roundR = -roundQ - roundS;
            }

            return new HexCoord((int)roundQ, (int)roundR);
        }

        public bool Equals(HexCoord other) => this == other;

        public override bool Equals(object obj) => obj is HexCoord other && this == other;

        public override int GetHashCode() => unchecked((Q * 397) ^ R);

        public override string ToString() => $"({Q}, {R}, {S})";
    }
}
