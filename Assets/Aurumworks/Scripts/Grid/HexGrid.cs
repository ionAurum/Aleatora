using System;
using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The board, built in code from the tuning's Grid section: a k × m × n hexagon of cells
    /// (HexShape), laid out flat or pointy (HexLayout) with its middle on this object, one
    /// HexCellView a cell, made from the tuning's prefab. It reads the tuning every frame: a change
    /// to the dimensions, orientation, size, gap or prefab rebuilds the board (Rebuilt), and a
    /// change to the height or the colours shows at once, so the look can be tuned in play. Made by
    /// the bootstrap (Aleatora), not in a scene.
    /// </summary>
    public class HexGrid : MonoBehaviour
    {
        private GridTuning _tuning;
        private HexCellView[] _cells = Array.Empty<HexCellView>(); // by HexShape.IndexOf
        private Transform _centre;
        private bool _warnedNoPrefab;

        // What the board was last built and coloured from, to see a change in the tuning.
        private int _k, _m, _n;
        private HexOrientation _orientation;
        private float _size, _gap, _height;
        private GameObject _prefab;
        private Color _restColor, _selectedColor;

        /// <summary>The board's shape; null before it is first built.</summary>
        public HexShape Shape { get; private set; }

        /// <summary>Where the cells sit, in this object's space, the board's middle at its origin.</summary>
        public HexLayout Layout { get; private set; }

        /// <summary>How many cells are drawn: the shape's count, or none without a prefab.</summary>
        public int CellCount => _cells.Length;

        /// <summary>The height of the cells' tops above this object, where a pointer's ray is cast to.</summary>
        public float TopHeight { get; private set; }

        /// <summary>The board's middle, for a camera to follow.</summary>
        public Transform Centre => _centre;

        /// <summary>Raised after the board has been built again: every cell before it is gone.</summary>
        public event Action Rebuilt;

        /// <summary>Gives the grid its tuning and builds the board.</summary>
        public void Begin(GridTuning tuning)
        {
            _tuning = tuning;
            _centre = new GameObject("Centre").transform;
            _centre.SetParent(transform, false);
            Build();
        }

        /// <summary>The cell at a coordinate, if it is on the board and drawn.</summary>
        public bool TryGetCell(HexCoord coord, out HexCellView cell)
        {
            int index = Shape != null ? Shape.IndexOf(coord) : -1;
            cell = index >= 0 && index < _cells.Length ? _cells[index] : null;
            return cell != null;
        }

        /// <summary>Where a cell's top is, in the world.</summary>
        public Vector3 TopOf(HexCoord coord)
        {
            HexPoint point = Layout.Position(coord);
            return transform.TransformPoint(new Vector3(point.X, TopHeight, point.Z));
        }

        //----------

        private void Update()
        {
            if (_tuning == null)
            {
                return;
            }

            if (_tuning.K != _k || _tuning.M != _m || _tuning.N != _n || _tuning.Orientation != _orientation
                || !Mathf.Approximately(_tuning.Size, _size) || !Mathf.Approximately(_tuning.Gap, _gap)
                || _tuning.CellPrefab != _prefab)
            {
                Build();
                Rebuilt?.Invoke();
            }
            else if (!Mathf.Approximately(_tuning.Height, _height) || _tuning.RestColor != _restColor
                || _tuning.SelectedColor != _selectedColor)
            {
                ApplyLook();
            }
        }

        private void Build()
        {
            foreach (HexCellView cell in _cells)
            {
                if (cell != null)
                {
                    Destroy(cell.gameObject);
                }
            }

            _k = _tuning.K;
            _m = _tuning.M;
            _n = _tuning.N;
            _orientation = _tuning.Orientation;
            _size = _tuning.Size;
            _gap = _tuning.Gap;
            _prefab = _tuning.CellPrefab;

            Shape = new HexShape(_k, _m, _n);
            Layout = HexLayout.Centred(Shape, _orientation, _size, _gap);
            if (_prefab == null)
            {
                _cells = Array.Empty<HexCellView>();
                if (!_warnedNoPrefab)
                {
                    _warnedNoPrefab = true;
                    Debug.LogError("HexGrid: the tuning names no cell prefab (AleatoraTuning, Grid, Cell Prefab), so no board is drawn.");
                }

                ApplyLook();
                return;
            }

            // The prefab is flat-topped; a quarter turn makes it pointy.
            Quaternion facing = _orientation == HexOrientation.Pointy ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            _cells = new HexCellView[Shape.Count];
            for (int i = 0; i < _cells.Length; i++)
            {
                HexCoord coord = Shape.Cells[i];
                HexPoint point = Layout.Position(coord);
                GameObject cell = Instantiate(_prefab, transform);
                cell.name = $"Cell {coord}";
                cell.transform.SetLocalPositionAndRotation(new Vector3(point.X, 0f, point.Z), facing);
                if (!cell.TryGetComponent(out HexCellView view))
                {
                    view = cell.AddComponent<HexCellView>();
                }

                view.Place(coord);
                _cells[i] = view;
            }

            ApplyLook();
        }

        // The cells' scale and colours, and from them where their tops are.
        private void ApplyLook()
        {
            _height = _tuning.Height;
            _restColor = _tuning.RestColor;
            _selectedColor = _tuning.SelectedColor;

            var scale = new Vector3(_size, _height, _size);
            foreach (HexCellView cell in _cells)
            {
                cell.transform.localScale = scale;
                cell.Refresh(_tuning);
            }

            TopHeight = 0f;
            if (_cells.Length > 0)
            {
                Renderer first = _cells[0].GetComponentInChildren<Renderer>();
                if (first != null)
                {
                    TopHeight = transform.InverseTransformPoint(first.bounds.max).y;
                }
            }
        }
    }
}
