using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The last notes of a melody as a piano roll (M3, task 5): a column per note, the newest at the
    /// right, and a row per semitone over the voicing's range, low at the bottom. The tonic's rows
    /// are marked and named at the left; a note of the mode is in the theme's accent, any other in
    /// danger. The newest note's MIDI number is written beside it. While the melody plays (MIDI-3,
    /// the MIDI plan's decision 9) the playing note's column is framed in the theme's text colour;
    /// a note that has scrolled off the strip's left edge plays with nothing framed.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))] // Graphic doesn't, and the Editor never adds one itself
    public class MelodyStrip : MaskableGraphic
    {
        /// <summary>How many notes it shows.</summary>
        public const int Columns = 32;

        private const float Gutter = 48f;
        private const float NumberBand = 44f;

        private readonly List<int> _notes = new List<int>(Columns);
        private readonly List<bool> _inMode = new List<bool>(Columns);
        private readonly List<TextMeshProUGUI> _tonicNames = new List<TextMeshProUGUI>();
        private TextMeshProUGUI _number;
        private int _low = 55;
        private int _high = 79;
        private int _tonic = 60;
        private int _first;         // the index in the melody of the first note shown
        private int _playhead = -1; // the index in the melody of the note playing, or -1

        /// <summary>How many notes it shows now: up to Columns.</summary>
        public int Count => _notes.Count;

        /// <summary>The notes shown, oldest first, as MIDI note numbers.</summary>
        public IReadOnlyList<int> Notes => _notes;

        /// <summary>The index in the melody of the note playing (SetPlayhead), or −1.</summary>
        public int Playhead => _playhead;

        /// <summary>The column framed as the playhead (0 is the oldest note shown), or −1: none playing, or off the strip.</summary>
        public int PlayheadColumn => _playhead >= _first && _playhead - _first < _notes.Count ? _playhead - _first : -1;

        /// <summary>Adds the strip to rect, which it fills.</summary>
        public static MelodyStrip On(RectTransform rect)
        {
            var strip = rect.gameObject.AddComponent<MelodyStrip>();
            strip.raycastTarget = false;
            strip._number = UiKit.Text(rect, "", 18f, UiKit.Theme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            strip._number.textWrappingMode = TextWrappingModes.NoWrap;
            strip._number.rectTransform.anchorMin = strip._number.rectTransform.anchorMax = new Vector2(0f, 0f);
            strip._number.rectTransform.pivot = new Vector2(0f, 0.5f);
            strip._number.rectTransform.sizeDelta = new Vector2(NumberBand, 24f);
            return strip;
        }

        /// <summary>
        /// Shows the last Columns of a melody's notes (MIDI note numbers, with whether each is a
        /// degree of the mode) over the range low to high, with the tonic's rows marked.
        /// </summary>
        public void Show(IReadOnlyList<int> notes, IReadOnlyList<bool> inMode, int low, int high, int tonic)
        {
            _notes.Clear();
            _inMode.Clear();
            _first = Mathf.Max(notes.Count - Columns, 0);
            for (int i = _first; i < notes.Count; i++)
            {
                _notes.Add(notes[i]);
                _inMode.Add(inMode[i]);
            }

            _low = Mathf.Min(low, high);
            _high = Mathf.Max(low, high);
            _tonic = tonic;
            Place();
            SetVerticesDirty();
        }

        /// <summary>Frames the note at this index in the melody last shown as the one playing; −1 for none.</summary>
        public void SetPlayhead(int index)
        {
            index = index < 0 ? -1 : index;
            if (index == _playhead)
            {
                return;
            }

            _playhead = index;
            SetVerticesDirty();
        }

        //----------

        private float RowHeight(Rect area) => area.height / (_high - _low + 1);

        private float ColumnWidth(Rect area) => (area.width - Gutter - NumberBand) / Columns;

        // The tonic's names in the gutter, and the newest note's number beside it.
        private void Place()
        {
            Rect area = rectTransform.rect;
            float row = RowHeight(area);
            int shown = 0;
            for (int note = _low; note <= _high; note++)
            {
                if (((note - _tonic) % Degree.Count + Degree.Count) % Degree.Count != 0)
                {
                    continue;
                }

                if (shown == _tonicNames.Count)
                {
                    TextMeshProUGUI name = UiKit.Text(transform, "", 16f, UiKit.Theme.Muted, align: TextAlignmentOptions.MidlineLeft);
                    name.textWrappingMode = TextWrappingModes.NoWrap;
                    RectTransform rect = name.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.sizeDelta = new Vector2(Gutter, 22f);
                    _tonicNames.Add(name);
                }

                TextMeshProUGUI tonicName = _tonicNames[shown++];
                tonicName.gameObject.SetActive(true);
                tonicName.text = ViewText.NoteName(note);
                tonicName.rectTransform.anchoredPosition = new Vector2(0f, (note - _low + 0.5f) * row);
            }

            for (int i = shown; i < _tonicNames.Count; i++)
            {
                _tonicNames[i].gameObject.SetActive(false);
            }

            _number.gameObject.SetActive(_notes.Count > 0);
            if (_notes.Count > 0)
            {
                int newest = _notes[_notes.Count - 1];
                _number.text = newest.ToString(System.Globalization.CultureInfo.InvariantCulture);
                _number.rectTransform.anchoredPosition =
                    new Vector2(Gutter + Columns * ColumnWidth(area) + 6f, (Mathf.Clamp(newest, _low, _high) - _low + 0.5f) * row);
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (_number != null)
            {
                Place();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect area = rectTransform.rect;
            UiTheme theme = UiKit.Theme;
            float row = RowHeight(area);
            float column = ColumnWidth(area);
            float left = area.xMin + Gutter;
            float right = left + Columns * column;

            Color32 tonicRow = theme.AccentDim;
            tonicRow.a = 110;
            Color32 lane = theme.Line;
            lane.a = 40;
            for (int note = _low; note <= _high; note++)
            {
                float y = area.yMin + (note - _low) * row;
                if (((note - _tonic) % Degree.Count + Degree.Count) % Degree.Count == 0)
                {
                    ViewMesh.Rect(vh, left, y, right, y + row, tonicRow);
                }
                else if ((note - _low) % 2 == 1)
                {
                    ViewMesh.Rect(vh, left, y, right, y + row, lane);
                }
            }

            int offset = Columns - _notes.Count; // the newest at the right
            float inset = Mathf.Min(column * 0.12f, 3f);
            for (int i = 0; i < _notes.Count; i++)
            {
                int note = Mathf.Clamp(_notes[i], _low, _high);
                float x = left + (offset + i) * column;
                float y = area.yMin + (note - _low) * row;
                ViewMesh.Rect(vh, x + inset, y, x + column - inset, y + row, _inMode[i] ? theme.Accent : theme.Danger);
            }

            if (_notes.Count > 0)
            {
                float x = left + (Columns - 1) * column;
                ViewMesh.Frame(vh, x - 2f, area.yMin, x + column + 2f, area.yMax, 2f, theme.Line);
            }

            int playing = PlayheadColumn;
            if (playing >= 0)
            {
                float x = left + (offset + playing) * column;
                ViewMesh.Frame(vh, x - 3f, area.yMin - 3f, x + column + 3f, area.yMax + 3f, 3f, theme.Text);
            }
        }
    }
}
