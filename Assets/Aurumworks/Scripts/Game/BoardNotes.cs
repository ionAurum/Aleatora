using System;
using TMPro;
using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The board's notes (the emitter plan, EM-1): gives every cell of the grid a degree and an
    /// octave (CellNotes) from the tuning's Emitter section and the set it names, colours each cell
    /// for its degree (the root red, the others white to blue by how often they are played next to
    /// the likeliest; rulings 65 and 71) and writes the degree on its top. It reads the tuning every
    /// frame, and lays the board out again when the grid rebuilds or a value changes, so all of it
    /// can be tuned in play. Made by the bootstrap (Aleatora) on the grid's object.
    /// </summary>
    public class BoardNotes : MonoBehaviour
    {
        private static readonly int[] _counts = new int[Degree.Count];

        private HexGrid _grid;
        private GridTuning _gridTuning;
        private EmitterTuning _tuning;
        private Transform _labelRoot;
        private TextMeshPro[] _labels = Array.Empty<TextMeshPro>();
        private string _warnedSet;

        // What the notes were last laid out and drawn from, to see a change in the tuning.
        private HexShape _shape;
        private string _set;
        private CellLayout _layout;
        private ulong _seed;
        private float _chromaticWeight;
        private int _kInterval, _nInterval, _octaves;
        private bool _outwardLower;
        private Color _rootColor, _unlikelyColor, _likelyColor, _labelColor;
        private bool _showDegrees;
        private float _labelSize, _labelHeight, _top;
        private int _cellCount;

        /// <summary>The cells' notes; null while the set doesn't read or the board has no cells.</summary>
        public CellNotes Notes { get; private set; }

        /// <summary>The set the notes come from; null while it doesn't read.</summary>
        public MarkovTable Table { get; private set; }

        /// <summary>The labels, one a cell in the order of the grid's shape (empty without cells).</summary>
        public System.Collections.Generic.IReadOnlyList<TextMeshPro> Labels => _labels;

        /// <summary>Raised after the notes have been laid out again.</summary>
        public event Action Changed;

        /// <summary>Gives the board's notes the grid and the tunings, and lays them out.</summary>
        public void Begin(HexGrid grid, GridTuning gridTuning, EmitterTuning tuning)
        {
            _grid = grid;
            _gridTuning = gridTuning;
            _tuning = tuning;
            _labelRoot = new GameObject("Labels").transform;
            _labelRoot.SetParent(grid.transform, false);
            _grid.Rebuilt += Apply;
            Apply();
        }

        /// <summary>The MIDI note of the cell at a coordinate, or −1 if it has none.</summary>
        public int NoteAt(HexCoord coord)
        {
            int index = Notes != null ? Notes.Shape.IndexOf(coord) : -1;
            return index >= 0 ? Notes.Note(index, _tuning.Tonic, _tuning.LowestOctave) : -1;
        }

        /// <summary>The degree of the cell at a coordinate, or −1 if it has none.</summary>
        public int DegreeAt(HexCoord coord)
        {
            int index = Notes != null ? Notes.Shape.IndexOf(coord) : -1;
            return index >= 0 ? Notes.DegreeAt(index) : -1;
        }

        /// <summary>The colour at rest of a degree's cells, from the tuning and the notes' probabilities.</summary>
        public Color ColorOf(int degree) => _tuning.CellColor(degree, Notes != null ? Notes.Blue(degree) : 0f);

        //----------

        private void Update()
        {
            if (_tuning == null)
            {
                return;
            }

            if (_grid.Shape != _shape || _grid.CellCount != _cellCount || _tuning.Set != _set || _tuning.Layout != _layout
                || _tuning.Seed != _seed || !Mathf.Approximately(_tuning.ChromaticWeight, _chromaticWeight)
                || _tuning.KInterval != _kInterval || _tuning.NInterval != _nInterval || _tuning.Octaves != _octaves
                || _tuning.OutwardLower != _outwardLower)
            {
                Apply();
            }
            else if (_tuning.RootColor != _rootColor || _tuning.UnlikelyColor != _unlikelyColor || _tuning.LikelyColor != _likelyColor
                || _tuning.ShowDegrees != _showDegrees || LabelColor() != _labelColor
                || !Mathf.Approximately(_tuning.LabelSize, _labelSize) || !Mathf.Approximately(_tuning.LabelHeight, _labelHeight)
                || !Mathf.Approximately(_grid.TopHeight, _top))
            {
                Draw();
            }
        }

        private void OnDestroy()
        {
            if (_grid != null)
            {
                _grid.Rebuilt -= Apply;
            }
        }

        // Lays the notes out from the tuning, then draws them.
        private void Apply()
        {
            _shape = _grid.Shape;
            _cellCount = _grid.CellCount;
            _set = _tuning.Set;
            _layout = _tuning.Layout;
            _seed = _tuning.Seed;
            _chromaticWeight = _tuning.ChromaticWeight;
            _kInterval = _tuning.KInterval;
            _nInterval = _tuning.NInterval;
            _octaves = _tuning.Octaves;
            _outwardLower = _tuning.OutwardLower;

            Notes = null;
            Table = null;
            if (_shape != null && MarkovLibrary.TryGet(_set, out MarkovTable table))
            {
                Table = table;
                int mask = ModeScale.Mask(table.Mode);
                for (int d = 0; d < Degree.Count; d++)
                {
                    _counts[d] = table.Unigram(d);
                }

                double[] probabilities = CellNotes.Probabilities(_counts, mask, _chromaticWeight);
                Notes = new CellNotes(_shape, probabilities, mask, _layout, _seed, _kInterval, _nInterval, _octaves, _outwardLower);
            }
            else if (_warnedSet != _set)
            {
                _warnedSet = _set;
                Debug.LogWarning($"BoardNotes: the set '{_set}' (AleatoraTuning, Emitter, Set) isn't in the index, so the board has no notes.");
            }

            MakeLabels();
            Draw();
            Changed?.Invoke();
        }

        // One label a cell, kept from one layout to the next while the count holds.
        private void MakeLabels()
        {
            int wanted = Notes != null ? _grid.CellCount : 0;
            if (_labels.Length == wanted)
            {
                return;
            }

            foreach (TextMeshPro label in _labels)
            {
                if (label != null)
                {
                    Destroy(label.gameObject);
                }
            }

            _labels = new TextMeshPro[wanted];
            TMP_FontAsset font = UiKit.Theme.Body != null ? UiKit.Theme.Body : TMP_Settings.defaultFontAsset;
            for (int i = 0; i < wanted; i++)
            {
                var label = new GameObject("Label").AddComponent<TextMeshPro>();
                label.transform.SetParent(_labelRoot, false);
                if (font != null)
                {
                    label.font = font;
                }

                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.raycastTarget = false;
                _labels[i] = label;
            }
        }

        // The cells' colours and the labels, from the notes and the tuning's look.
        private void Draw()
        {
            _rootColor = _tuning.RootColor;
            _unlikelyColor = _tuning.UnlikelyColor;
            _likelyColor = _tuning.LikelyColor;
            _showDegrees = _tuning.ShowDegrees;
            _labelColor = LabelColor();
            _labelSize = _tuning.LabelSize;
            _labelHeight = _tuning.LabelHeight;
            _top = _grid.TopHeight;

            if (_shape == null)
            {
                return;
            }

            for (int i = 0; i < _shape.Count; i++)
            {
                HexCoord coord = _shape.Cells[i];
                if (!_grid.TryGetCell(coord, out HexCellView cell))
                {
                    continue;
                }

                cell.SetRestColor(Notes != null ? ColorOf(Notes.DegreeAt(i)) : null, _gridTuning);
                if (i >= _labels.Length)
                {
                    continue;
                }

                TextMeshPro label = _labels[i];
                label.gameObject.SetActive(_showDegrees);
                HexPoint point = _grid.Layout.Position(coord);
                label.transform.SetLocalPositionAndRotation(new Vector3(point.X, _top + _labelHeight, point.Z), Quaternion.Euler(90f, 0f, 0f));
                label.rectTransform.sizeDelta = new Vector2(2f, 1f) * _gridTuning.Size;
                label.fontSize = _labelSize;
                label.color = _labelColor;
                label.text = ViewText.DegreeName(Notes.DegreeAt(i));
            }
        }

        private Color LabelColor() => _tuning.LabelColorFromTheme ? UiKit.Theme.Text : _tuning.LabelColor;
    }
}
