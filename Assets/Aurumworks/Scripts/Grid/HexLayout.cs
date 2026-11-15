using System;

namespace Aurumworks.Aleatora
{
    /// <summary>Which way the hexagons face.</summary>
    public enum HexOrientation
    {
        /// <summary>A flat side up the board, and the k axis running north (+z).</summary>
        Flat,

        /// <summary>A corner up the board, and the k axis running east (+x), as in MathWorld's figure.</summary>
        Pointy,
    }

    /// <summary>A point on the board's plane.</summary>
    public readonly struct HexPoint
    {
        /// <summary>Makes the point.</summary>
        public HexPoint(float x, float z)
        {
            X = x;
            Z = z;
        }

        /// <summary>East.</summary>
        public float X { get; }

        /// <summary>North.</summary>
        public float Z { get; }
    }

    /// <summary>
    /// Where each cell sits on the board's plane, and which cell is under a point. Pointy puts the k
    /// axis on +x, the m axis to the north-east and the n axis to the north-west; Flat is Pointy
    /// turned a quarter turn, so the k axis runs along +z. Neighbours' centres are √3 · Size + Gap
    /// apart. A layout made with Centred has the board's middle at the origin.
    /// </summary>
    public readonly struct HexLayout
    {
        private const double HalfRootThree = 0.8660254037844386;

        private readonly double _originX; // the board's middle, before it is moved to the origin
        private readonly double _originZ;

        /// <summary>A layout with the cell (0, 0) at the origin.</summary>
        public HexLayout(HexOrientation orientation, float size, float gap)
            : this(orientation, size, gap, 0.0, 0.0)
        {
        }

        private HexLayout(HexOrientation orientation, float size, float gap, double originX, double originZ)
        {
            Orientation = orientation;
            Size = size;
            Gap = gap;
            _originX = originX;
            _originZ = originZ;
        }

        /// <summary>Which way the hexagons face.</summary>
        public HexOrientation Orientation { get; }

        /// <summary>A hexagon's centre to its corner.</summary>
        public float Size { get; }

        /// <summary>The space between neighbours' sides.</summary>
        public float Gap { get; }

        /// <summary>How far apart neighbours' centres are: √3 · Size + Gap.</summary>
        public float Spacing => (float)SpacingExact;

        private double SpacingExact => (2.0 * HalfRootThree * Size) + Gap;

        /// <summary>The layout with the middle of the shape's cells at the origin.</summary>
        public static HexLayout Centred(HexShape shape, HexOrientation orientation, float size, float gap)
        {
            var layout = new HexLayout(orientation, size, gap);
            layout.Raw(shape.CentreQ, shape.CentreR, out double x, out double z);
            return new HexLayout(orientation, size, gap, x, z);
        }

        /// <summary>Where the cell's centre is.</summary>
        public HexPoint Position(HexCoord cell)
        {
            Raw(cell.Q, cell.R, out double x, out double z);
            return new HexPoint((float)(x - _originX), (float)(z - _originZ));
        }

        /// <summary>The cell under a point: Position's inverse, then cube rounding.</summary>
        public HexCoord Pick(float x, float z)
        {
            double px = x + _originX;
            double pz = z + _originZ;
            if (Orientation == HexOrientation.Flat)
            {
                // Undo the quarter turn: (x, z) was (−z, x).
                double turnedX = pz;
                pz = -px;
                px = turnedX;
            }

            double spacing = SpacingExact;
            double r = -pz / (spacing * HalfRootThree);
            double q = (px / spacing) - (r / 2.0);
            return HexCoord.Round(q, r);
        }

        // The cell's centre before the board is centred; fractional coordinates for the board's middle.
        private void Raw(double q, double r, out double x, out double z)
        {
            double spacing = SpacingExact;
            double pointyX = spacing * (q + (r / 2.0));
            double pointyZ = -spacing * HalfRootThree * r;
            if (Orientation == HexOrientation.Flat)
            {
                x = -pointyZ;
                z = pointyX;
            }
            else
            {
                x = pointyX;
                z = pointyZ;
            }
        }
    }
}
