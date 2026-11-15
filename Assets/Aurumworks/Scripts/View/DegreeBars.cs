using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Twelve bars, one per degree, each as tall as its probability next to the tallest: the mode's
    /// degrees in the theme's accent, the rest muted. Each bar's share is written over it and its
    /// degree's name under it, and a caption runs under them all. One degree can be outlined: the
    /// note a step chose. The Melodies page shows two: the set's unigram, and Next note (M3, task 2).
    /// </summary>
    public class DegreeBars : MaskableGraphic
    {
        private const float ShareBand = 26f;
        private const float NameBand = 30f;
        private const float CaptionBand = 28f;
        private const float Outline = 3f;

        private readonly float[] _values = new float[Degree.Count];
        private readonly TextMeshProUGUI[] _shares = new TextMeshProUGUI[Degree.Count];
        private readonly TextMeshProUGUI[] _names = new TextMeshProUGUI[Degree.Count];
        private TextMeshProUGUI _caption;
        private int _mask = ModeScale.All;
        private int _outlined = -1;

        /// <summary>The twelve values shown, in the degrees' order.</summary>
        public IReadOnlyList<float> Values => _values;

        /// <summary>The degree outlined, or −1.</summary>
        public int Outlined => _outlined;

        /// <summary>The caption under the bars.</summary>
        public string Caption => _caption != null ? _caption.text : string.Empty;

        /// <summary>Adds the bars to rect, which they fill.</summary>
        public static DegreeBars On(RectTransform rect)
        {
            var bars = rect.gameObject.AddComponent<DegreeBars>();
            bars.raycastTarget = false;
            bars.Build();
            return bars;
        }

        /// <summary>
        /// Shows twelve values (the probabilities of a distribution, or counts: they are scaled to
        /// their sum), the mode's degrees as a mask (ModeScale), the degree to outline (−1 for none)
        /// and the caption.
        /// </summary>
        public void Show(ReadOnlySpan<float> values, int mask, int outlined, string caption)
        {
            float sum = 0f;
            for (int d = 0; d < Degree.Count; d++)
            {
                sum += Mathf.Max(values[d], 0f);
            }

            for (int d = 0; d < Degree.Count; d++)
            {
                _values[d] = sum > 0f ? Mathf.Max(values[d], 0f) / sum : 0f;
            }

            _mask = mask;
            _outlined = Degree.IsValid(outlined) ? outlined : -1;
            UiTheme theme = UiKit.Theme;
            for (int d = 0; d < Degree.Count; d++)
            {
                _shares[d].text = ViewText.Share(_values[d]);
                _shares[d].color = ModeScale.Has(mask, d) ? theme.Text : theme.Muted;
                _names[d].color = ModeScale.Has(mask, d) ? theme.Text : theme.Muted;
            }

            _caption.text = caption;
            SetVerticesDirty();
        }

        /// <summary>Shows twelve counts, scaled to their sum.</summary>
        public void Show(ReadOnlySpan<int> counts, int mask, int outlined, string caption)
        {
            Span<float> values = stackalloc float[Degree.Count];
            for (int d = 0; d < Degree.Count; d++)
            {
                values[d] = counts[d];
            }

            Show(values, mask, outlined, caption);
        }

        //----------

        private void Build()
        {
            UiTheme theme = UiKit.Theme;
            for (int d = 0; d < Degree.Count; d++)
            {
                float left = (float)d / Degree.Count;
                float right = (float)(d + 1) / Degree.Count;
                _shares[d] = Band(left, right, 1f, -ShareBand, 0f, "", 17f, theme.Muted);
                _names[d] = Band(left, right, 0f, CaptionBand, CaptionBand + NameBand, ViewText.DegreeName(d), 20f, theme.Text);
            }

            _caption = Band(0f, 1f, 0f, 0f, CaptionBand, "", 18f, theme.Muted);
        }

        // A one-line text across a share of the width, in a band offset from the bottom (anchorY 0)
        // or the top (anchorY 1).
        private TextMeshProUGUI Band(float left, float right, float anchorY, float bottom, float top, string text, float size,
            Color color)
        {
            TextMeshProUGUI tmp = UiKit.Text(transform, text, size, color, align: TextAlignmentOptions.Center);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            RectTransform rect = tmp.rectTransform;
            rect.anchorMin = new Vector2(left, anchorY);
            rect.anchorMax = new Vector2(right, anchorY);
            rect.offsetMin = new Vector2(0f, bottom);
            rect.offsetMax = new Vector2(0f, top);
            return tmp;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect area = rectTransform.rect;
            float bottom = area.yMin + CaptionBand + NameBand;
            float top = area.yMax - ShareBand;
            if (top <= bottom)
            {
                return;
            }

            float tallest = 0f;
            foreach (float value in _values)
            {
                tallest = Mathf.Max(tallest, value);
            }

            UiTheme theme = UiKit.Theme;
            float column = area.width / Degree.Count;
            float gap = column * 0.18f;
            Color32 baseline = theme.Line;
            ViewMesh.Rect(vh, area.xMin + gap / 2f, bottom - 1.5f, area.xMax - gap / 2f, bottom, baseline);
            for (int d = 0; d < Degree.Count; d++)
            {
                float x0 = area.xMin + d * column + gap;
                float x1 = area.xMin + (d + 1) * column - gap;
                float height = tallest > 0f ? (top - bottom) * _values[d] / tallest : 0f;
                Color32 fill = ModeScale.Has(_mask, d) ? theme.Accent : theme.Muted;
                if (height > 0f)
                {
                    ViewMesh.Rect(vh, x0, bottom, x1, bottom + Mathf.Max(height, 1.5f), fill);
                }

                if (d == _outlined)
                {
                    ViewMesh.Frame(vh, x0 - Outline - 2f, bottom - Outline - 2f, x1 + Outline + 2f, top, Outline, theme.Text);
                }
            }
        }
    }
}
