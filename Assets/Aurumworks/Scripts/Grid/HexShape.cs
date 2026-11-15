using System;
using System.Collections.Generic;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The board's shape: MathWorld's k × m × n hexagonal grid graph, a hexagon whose opposite sides
    /// are k, m and n cells long. k = m = n is a regular hexagon, n = 1 a k × m parallelogram,
    /// 1 × 1 × 1 one cell. A cell is in it when 0 ≤ R ≤ m + n − 2, −(m − 1) ≤ Q ≤ k − 1 and
    /// −(k + n − 2) ≤ S ≤ 0: its sides of constant R have k cells, those of constant S m, those of
    /// constant Q n. Cells are numbered row by row (R, then Q), so arrays can hold one thing a cell.
    /// </summary>
    public sealed class HexShape
    {
        private readonly int[] _rowStart; // the index of each row's first cell
        private readonly HexCoord[] _cells;

        /// <summary>The board with sides of k, m and n cells, each at least 1.</summary>
        public HexShape(int k, int m, int n)
        {
            if (k < 1 || m < 1 || n < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(k), $"A board's sides are at least 1 cell: {k} × {m} × {n}.");
            }

            K = k;
            M = m;
            N = n;
            Count = (k * m) + (m * n) + (n * k) - k - m - n + 1;

            int rows = m + n - 1;
            _rowStart = new int[rows];
            _cells = new HexCoord[Count];
            int index = 0;
            for (int r = 0; r < rows; r++)
            {
                _rowStart[r] = index;
                for (int q = FirstQ(r); q <= LastQ(r); q++)
                {
                    _cells[index++] = new HexCoord(q, r);
                }
            }
        }

        /// <summary>The cells along the sides of constant R (the k axis).</summary>
        public int K { get; }

        /// <summary>The cells along the sides of constant S (the m axis).</summary>
        public int M { get; }

        /// <summary>The cells along the sides of constant Q (the n axis).</summary>
        public int N { get; }

        /// <summary>How many cells the board has: km + mn + nk − k − m − n + 1.</summary>
        public int Count { get; }

        /// <summary>Every cell, row by row: R, then Q. A cell's place in it is its IndexOf.</summary>
        public IReadOnlyList<HexCoord> Cells => _cells;

        /// <summary>The mean of the cells' Q coordinates: with CentreR, the board's middle.</summary>
        public double CentreQ
        {
            get
            {
                long sum = 0;
                foreach (HexCoord cell in _cells)
                {
                    sum += cell.Q;
                }

                return (double)sum / Count;
            }
        }

        /// <summary>The mean of the cells' R coordinates.</summary>
        public double CentreR
        {
            get
            {
                long sum = 0;
                foreach (HexCoord cell in _cells)
                {
                    sum += cell.R;
                }

                return (double)sum / Count;
            }
        }

        /// <summary>Whether the cell is on the board.</summary>
        public bool Contains(HexCoord cell) =>
            cell.R >= 0 && cell.R < _rowStart.Length && cell.Q >= FirstQ(cell.R) && cell.Q <= LastQ(cell.R);

        /// <summary>The cell's number, 0 to Count − 1, in the order of Cells; −1 if it isn't on the board.</summary>
        public int IndexOf(HexCoord cell) => Contains(cell) ? _rowStart[cell.R] + cell.Q - FirstQ(cell.R) : -1;

        // Q ≥ −(m − 1), and S ≤ 0 gives Q ≥ −R.
        private int FirstQ(int r) => Math.Max(-(M - 1), -r);

        // Q ≤ k − 1, and S ≥ −(k + n − 2) gives Q ≤ k + n − 2 − R.
        private int LastQ(int r) => Math.Min(K - 1, K + N - 2 - r);
    }
}
