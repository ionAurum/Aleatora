using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The order-1 table as a 12 × 12 grid: a row per degree a step comes from, top to bottom, and a
    /// column per degree it goes to, left to right, each row scaled to its sum. A cell's colour runs
    /// from the theme's panel to its accent with the square root of its share, so small shares still
    /// show. One texture of 12 × 12 pixels, point filtered, made for each set and destroyed with the
    /// grid. The degrees' names run along both edges; one row can be framed (the newest note's), and
    /// a click on a row raises RowClicked (the pointer only: it isn't in the keyboard navigation).
    /// M3, task 3.
    /// </summary>
    public class TransitionGrid : MonoBehaviour, IPointerClickHandler
    {
        private const float Gutter = 34f;
        private const float FrameWidth = 3f;

        private RawImage _cells;
        private readonly int[] _context = new int[1]; // a context of one degree, for the table
        private Texture2D _texture;
        private RectTransform _frame;
        private readonly TextMeshProUGUI[] _rowNames = new TextMeshProUGUI[Degree.Count];
        private readonly TextMeshProUGUI[] _columnNames = new TextMeshProUGUI[Degree.Count];

        /// <summary>Raised with the degree whose row was clicked.</summary>
        public event Action<int> RowClicked;

        /// <summary>The row framed, or −1.</summary>
        public int Framed { get; private set; } = -1;

        /// <summary>
        /// Builds the grid in area: a square as large as fits once the names' gutters are taken,
        /// centred.
        /// </summary>
        public static TransitionGrid On(RectTransform area)
        {
            float side = Mathf.Max(Mathf.Min(area.rect.width, area.rect.height) - Gutter, 12f);
            RectTransform rect = UiKit.Pin(UiKit.Rect("Transition grid", area), new Vector2(0.5f, 0.5f), new Vector2(side, side),
                new Vector2(Gutter / 2f, -Gutter / 2f));
            var grid = rect.gameObject.AddComponent<TransitionGrid>();
            grid.Build(rect);
            return grid;
        }

        /// <summary>Shows a set's order-1 table, the names of its mode's degrees in the text colour and the rest muted.</summary>
        public void Show(MarkovTable table, int mask)
        {
            UiTheme theme = UiKit.Theme;
            if (_texture == null)
            {
                _texture = new Texture2D(Degree.Count, Degree.Count, TextureFormat.RGBA32, false)
                {
                    name = "Transition grid",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                _cells.texture = _texture;
            }

            var pixels = new Color32[Degree.Count * Degree.Count];
            int[] context = _context;
            for (int from = 0; from < Degree.Count; from++)
            {
                context[0] = from;
                bool heard = table.TryGetCounts(context, out ReadOnlySpan<int> counts);
                float total = 0f;
                if (heard)
                {
                    foreach (int count in counts)
                    {
                        total += count;
                    }
                }

                int y = Degree.Count - 1 - from; // the texture's first row is its bottom
                for (int to = 0; to < Degree.Count; to++)
                {
                    float share = total > 0f ? counts[to] / total : 0f;
                    pixels[y * Degree.Count + to] = Color.Lerp(theme.Panel, theme.Accent, Mathf.Sqrt(share));
                }
            }

            _texture.SetPixels32(pixels);
            _texture.Apply(false);

            for (int d = 0; d < Degree.Count; d++)
            {
                Color color = ModeScale.Has(mask, d) ? theme.Text : theme.Muted;
                _rowNames[d].color = color;
                _columnNames[d].color = color;
            }
        }

        /// <summary>Frames a degree's row; −1 frames none.</summary>
        public void Frame(int degree)
        {
            Framed = Degree.IsValid(degree) ? degree : -1;
            _frame.gameObject.SetActive(Framed >= 0);
            if (Framed >= 0)
            {
                float top = 1f - (float)Framed / Degree.Count;
                UiKit.Anchor(_frame, 0f, top - 1f / Degree.Count, 1f, top);
                _frame.offsetMin = new Vector2(-FrameWidth, -FrameWidth);
                _frame.offsetMax = new Vector2(FrameWidth, FrameWidth);
            }
        }

        /// <summary>The row under a point on the screen, or −1 if the point is off the grid.</summary>
        public int RowAt(Vector2 screenPoint, Camera eventCamera)
        {
            var rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, eventCamera, out Vector2 local)
                || !rect.rect.Contains(local))
            {
                return -1;
            }

            float fromTop = (rect.rect.yMax - local.y) / rect.rect.height;
            return Mathf.Clamp((int)(fromTop * Degree.Count), 0, Degree.Count - 1);
        }

        /// <summary>A click on a row raises RowClicked.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            int row = RowAt(eventData.position, eventData.pressEventCamera);
            if (row >= 0)
            {
                RowClicked?.Invoke(row);
            }
        }

        //----------

        private void Build(RectTransform rect)
        {
            UiTheme theme = UiKit.Theme;
            _cells = rect.gameObject.AddComponent<RawImage>();
            _cells.raycastTarget = true;
            _cells.color = Color.white;

            for (int d = 0; d < Degree.Count; d++)
            {
                float near = (float)d / Degree.Count;
                float far = (float)(d + 1) / Degree.Count;
                string name = ViewText.DegreeName(d);

                _rowNames[d] = Name(rect, name, TextAlignmentOptions.MidlineRight);
                RectTransform row = UiKit.Anchor(_rowNames[d].rectTransform, 0f, 1f - far, 0f, 1f - near);
                row.offsetMin = new Vector2(-Gutter, 0f);
                row.offsetMax = new Vector2(-6f, 0f);

                _columnNames[d] = Name(rect, name, TextAlignmentOptions.Bottom);
                RectTransform column = UiKit.Anchor(_columnNames[d].rectTransform, near, 1f, far, 1f);
                column.offsetMin = new Vector2(0f, 4f);
                column.offsetMax = new Vector2(0f, Gutter);
            }

            _frame = UiKit.Rect("Frame", rect);
            UiKit.Shape(_frame, theme.Text, 0f, FrameWidth);
            Frame(-1);
        }

        private static TextMeshProUGUI Name(Transform parent, string text, TextAlignmentOptions align)
        {
            TextMeshProUGUI tmp = UiKit.Text(parent, text, 15f, UiKit.Theme.Text, align: align);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
            }
        }
    }
}
