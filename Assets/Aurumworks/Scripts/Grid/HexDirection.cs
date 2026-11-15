namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The six ways from a hexagon to a neighbour, named by the board's three dimensions so they
    /// read the same in either orientation, in turning order. The opposite of d is (d + 3) % 6.
    /// </summary>
    public enum HexDirection
    {
        /// <summary>Along the k axis: (ΔQ, ΔR) = (+1, 0).</summary>
        PlusK = 0,

        /// <summary>Along the m axis: (+1, −1).</summary>
        PlusM = 1,

        /// <summary>Along the n axis: (0, −1).</summary>
        PlusN = 2,

        /// <summary>Back along the k axis: (−1, 0).</summary>
        MinusK = 3,

        /// <summary>Back along the m axis: (−1, +1).</summary>
        MinusM = 4,

        /// <summary>Back along the n axis: (0, +1).</summary>
        MinusN = 5,
    }

    /// <summary>What the directions are in coordinates.</summary>
    public static class HexDirections
    {
        /// <summary>How many directions a hexagon has.</summary>
        public const int Count = 6;

        private static readonly int[] _deltaQ = { 1, 1, 0, -1, -1, 0 };
        private static readonly int[] _deltaR = { 0, -1, -1, 0, 1, 1 };

        /// <summary>The step one cell that way.</summary>
        public static HexCoord Offset(HexDirection direction) => new(_deltaQ[(int)direction], _deltaR[(int)direction]);

        /// <summary>The way back.</summary>
        public static HexDirection Opposite(HexDirection direction) => (HexDirection)(((int)direction + 3) % Count);
    }
}
