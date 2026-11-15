using System;
using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The grid's tuning, a section of AleatoraTuning: the board's three dimensions, which way its
    /// hexagons face, their size and spacing, and how a cell looks (its colours and glow). HexGrid
    /// reads it every frame: a change to the shape rebuilds the board, a change to the look shows
    /// at once, so all of it can be tuned in play. The defaults are the grid as it was found
    /// (Docs/Aleatora_Tech.md, Grid), but for its shape and the glow.
    /// </summary>
    [Serializable]
    public class GridTuning
    {
        /// <summary>The longest side HexGrid builds: 40 × 40 × 40 is 4,681 cells.</summary>
        public const int LongestSide = 40;

        [Header("Shape")]
        [Tooltip("Cells along the k axis: north in Flat, east in Pointy. k = m = n is a regular hexagon.")]
        [SerializeField, Range(1, LongestSide)] private int _k = 5;
        [Tooltip("Cells along the m axis, a sixth of a turn from k.")]
        [SerializeField, Range(1, LongestSide)] private int _m = 5;
        [Tooltip("Cells along the n axis, a third of a turn from k. 1 makes a k × m parallelogram.")]
        [SerializeField, Range(1, LongestSide)] private int _n = 5;
        [Tooltip("Flat: a flat side up the board, the k axis running north. Pointy: a corner up the board, the k axis running east.")]
        [SerializeField] private HexOrientation _orientation = HexOrientation.Flat;
        [Tooltip("A hexagon's centre to its corner, in metres. The cell prefab is scaled by it, so the prefab's own corner should be 1 from its centre.")]
        [SerializeField, Min(0.01f)] private float _size = 1f;
        // Hidden and held at 0 (Jon, 2026-11-15, ruling 22): the cells touch, so the colliders tile
        // (ruling 14); the gap between the pads is in the cell's mesh.
        [SerializeField, HideInInspector] private float _gap;

        [Header("Look")]
        [Tooltip("What each cell is made from. Modelled flat-topped (a corner on its x axis) and one unit to a corner; its colliders are not used.")]
        [SerializeField] private GameObject _cellPrefab;
        [Tooltip("The cells' height: the prefab's own, times this.")]
        [SerializeField, Min(0.01f)] private float _height = 1f;
        [Tooltip("A cell's colour at rest: the material's base colour is multiplied by it. White leaves the material as it is.")]
        [SerializeField] private Color _restColor = Color.white;
        [Tooltip("The selected cell's colour. On a material with a glow (the gel pad shader) it is the colour the cell lights up with; on any other it replaces the base colour.")]
        [SerializeField] private Color _selectedColor = new(68f / 255f, 214f / 255f, 44f / 255f);
        [Tooltip("How brightly a cell at rest glows in the rest colour, 0 to 1. Only on a material with a glow; its brightness at 1 is the material's Glow Intensity.")]
        [SerializeField, Range(0f, 1f)] private float _restGlow;
        [Tooltip("How brightly the selected cell glows in the selected colour, 0 to 1. Only on a material with a glow; its brightness at 1 is the material's Glow Intensity.")]
        [SerializeField, Range(0f, 1f)] private float _selectedGlow = 1f;

        /// <summary>Cells along the k axis, 1 to LongestSide.</summary>
        public int K
        {
            get => Mathf.Clamp(_k, 1, LongestSide);
            set => _k = value;
        }

        /// <summary>Cells along the m axis, 1 to LongestSide.</summary>
        public int M
        {
            get => Mathf.Clamp(_m, 1, LongestSide);
            set => _m = value;
        }

        /// <summary>Cells along the n axis, 1 to LongestSide.</summary>
        public int N
        {
            get => Mathf.Clamp(_n, 1, LongestSide);
            set => _n = value;
        }

        /// <summary>Which way the hexagons face.</summary>
        public HexOrientation Orientation
        {
            get => _orientation;
            set => _orientation = value;
        }

        /// <summary>A hexagon's centre to its corner.</summary>
        public float Size => Mathf.Max(_size, 0.01f);

        /// <summary>The space between neighbours' sides: always 0, whatever the asset holds (ruling 22).</summary>
        public float Gap => Mathf.Clamp(_gap, 0f, 0f);

        /// <summary>What each cell is made from; null until the tuning asset names one.</summary>
        public GameObject CellPrefab
        {
            get => _cellPrefab;
            set => _cellPrefab = value;
        }

        /// <summary>The cells' height, as a multiple of the prefab's own.</summary>
        public float Height => Mathf.Max(_height, 0.01f);

        /// <summary>A cell's colour at rest.</summary>
        public Color RestColor => _restColor;

        /// <summary>The selected cell's colour.</summary>
        public Color SelectedColor => _selectedColor;

        /// <summary>How brightly a cell at rest glows, 0 to 1, on a material with a glow.</summary>
        public float RestGlow => Mathf.Clamp01(_restGlow);

        /// <summary>How brightly the selected cell glows, 0 to 1, on a material with a glow.</summary>
        public float SelectedGlow => Mathf.Clamp01(_selectedGlow);
    }
}
