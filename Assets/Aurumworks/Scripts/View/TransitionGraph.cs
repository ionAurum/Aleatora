using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The order-1 table as a directed graph (Design ruling 18; M3, task 4). Twelve nodes, one per
    /// degree, on a chromatic circle with 1 at the top and the rest clockwise, each named; the mode's
    /// in the theme's accent, the rest muted. An arc runs from a degree to each degree that follows
    /// it with at least MelodyViewTuning.MinimumArcShare of its row, bowed inward (so it never runs
    /// through the nodes between its ends) and the two directions between a pair by different
    /// amounts, so they stay apart; as wide as its share, with an arrowhead and its share written at
    /// its middle (".42"). A repeated note is a loop outside its node. One node can be
    /// framed (the newest note's), its arcs drawn in front in the accent. A click on a node raises
    /// NodeClicked (the pointer only, as the grid's rows).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))] // Graphic doesn't, and the Editor never adds one itself
    public class TransitionGraph : MaskableGraphic, IPointerClickHandler
    {
        private const int CurveSteps = 40;
        private const float FrameGap = 4f;
        private const float FrameWidth = 3f;

        private struct Arc
        {
            public int From;
            public int To;
            public float Share;
            public RectTransform Label;
            public TextMeshProUGUI Text;
        }

        private readonly List<Arc> _arcs = new List<Arc>();
        private readonly int[] _context = new int[1]; // a context of one degree, for the table
        private readonly TextMeshProUGUI[] _nodeNames = new TextMeshProUGUI[Degree.Count];
        private readonly List<Vector2> _curve = new List<Vector2>(CurveSteps + 1);
        private readonly List<Vector2> _line = new List<Vector2>(CurveSteps + 1);
        private RectTransform _labels;
        private RectTransform _names;
        private MelodyViewTuning _tuning;
        private int _mask = ModeScale.All;
        private int _framed = -1;

        /// <summary>Raised with the degree whose node was clicked.</summary>
        public event Action<int> NodeClicked;

        /// <summary>How many arcs are drawn, loops included.</summary>
        public int ArcCount => _arcs.Count;

        /// <summary>The smallest share of the arcs drawn, or 1 if none is.</summary>
        public float SmallestShare
        {
            get
            {
                float smallest = 1f;
                foreach (Arc arc in _arcs)
                {
                    smallest = Mathf.Min(smallest, arc.Share);
                }

                return smallest;
            }
        }

        /// <summary>The node framed, or −1.</summary>
        public int Framed => _framed;

        /// <summary>Adds the graph to rect, which it fills.</summary>
        public static TransitionGraph On(RectTransform rect)
        {
            var graph = rect.gameObject.AddComponent<TransitionGraph>();
            graph.raycastTarget = true; // for the clicks on its nodes
            graph._labels = UiKit.Anchor(UiKit.Rect("Shares", rect), 0f, 0f, 1f, 1f);
            graph._names = UiKit.Anchor(UiKit.Rect("Names", rect), 0f, 0f, 1f, 1f);
            return graph;
        }

        /// <summary>Shows a set's order-1 table as the tuning says to draw it. Called again when the tuning changes.</summary>
        public void Show(MarkovTable table, int mask, MelodyViewTuning tuning)
        {
            _tuning = tuning;
            _mask = mask;
            UiTheme theme = UiKit.Theme;

            foreach (Arc arc in _arcs)
            {
                Destroy(arc.Label.gameObject);
            }

            _arcs.Clear();
            float minimum = Mathf.Clamp01(tuning.MinimumArcShare);
            int[] context = _context;
            for (int from = 0; from < Degree.Count; from++)
            {
                context[0] = from;
                if (!table.TryGetCounts(context, out ReadOnlySpan<int> counts))
                {
                    continue;
                }

                float total = 0f;
                foreach (int count in counts)
                {
                    total += count;
                }

                for (int to = 0; to < Degree.Count && total > 0f; to++)
                {
                    float share = counts[to] / total;
                    if (counts[to] > 0 && share >= minimum)
                    {
                        _arcs.Add(MakeArc(from, to, share, tuning.ArcLabelSize));
                    }
                }
            }

            for (int d = 0; d < Degree.Count; d++)
            {
                if (_nodeNames[d] == null)
                {
                    _nodeNames[d] = UiKit.Text(_names, ViewText.DegreeName(d), 20f, theme.OnAccent, FontStyles.Bold,
                        TextAlignmentOptions.Center);
                    _nodeNames[d].textWrappingMode = TextWrappingModes.NoWrap;
                    _nodeNames[d].rectTransform.anchorMin = _nodeNames[d].rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                }

                _nodeNames[d].fontSize = tuning.NodeLabelSize;
                _nodeNames[d].color = theme.OnAccent;
            }

            Frame(_framed);
        }

        /// <summary>Frames a degree's node and draws its arcs in front; −1 frames none.</summary>
        public void Frame(int degree)
        {
            _framed = Degree.IsValid(degree) ? degree : -1;
            UiTheme theme = UiKit.Theme;
            // The framed node's shares are drawn last, over the others.
            foreach (Arc arc in _arcs)
            {
                bool front = arc.From == _framed;
                arc.Text.color = front ? theme.Text : theme.Muted;
                if (front)
                {
                    arc.Label.SetAsLastSibling();
                }
            }

            Place();
            SetVerticesDirty();
        }

        /// <summary>Where a degree's node is drawn, in the graph's local space (once it has been shown).</summary>
        public Vector2 NodeCentre(int degree)
        {
            if (_tuning == null)
            {
                return rectTransform.rect.center;
            }

            Layout(out Vector2 centre, out float radius);
            return Node(degree, centre, radius);
        }

        /// <summary>The degree whose node is under a point on the screen, or −1.</summary>
        public int NodeAt(Vector2 screenPoint, Camera eventCamera)
        {
            if (_tuning == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 local))
            {
                return -1;
            }

            Layout(out Vector2 centre, out float radius);
            for (int d = 0; d < Degree.Count; d++)
            {
                if ((local - Node(d, centre, radius)).sqrMagnitude <= Sq(_tuning.NodeRadius * 1.2f))
                {
                    return d;
                }
            }

            return -1;
        }

        /// <summary>A click on a node raises NodeClicked.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            int node = NodeAt(eventData.position, eventData.pressEventCamera);
            if (node >= 0)
            {
                NodeClicked?.Invoke(node);
            }
        }

        //----------

        private Arc MakeArc(int from, int to, float share, float size)
        {
            UiTheme theme = UiKit.Theme;
            RectTransform label = UiKit.Rect("Share " + Degree.Name(from) + " " + Degree.Name(to), _labels);
            label.anchorMin = label.anchorMax = new Vector2(0.5f, 0.5f);
            // A patch of the panel behind the share, so an arc under it doesn't cross the figures.
            Color patch = theme.Panel;
            patch.a = 0.85f;
            UiKit.Fill(label, patch).raycastTarget = false;
            TextMeshProUGUI text = UiKit.Text(label, ViewText.Share(share), size, theme.Muted, align: TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Anchor(text.rectTransform, 0f, 0f, 1f, 1f);
            label.sizeDelta = new Vector2(text.GetPreferredValues().x + 6f, size + 4f);
            return new Arc { From = from, To = to, Share = share, Label = label, Text = text };
        }

        // The circle the nodes sit on: as large as leaves room round it for the loops and their shares.
        private void Layout(out Vector2 centre, out float radius)
        {
            Rect area = rectTransform.rect;
            centre = area.center;
            float margin = _tuning.NodeRadius * 2.4f + _tuning.ArcLabelSize * 1.6f;
            radius = Mathf.Max(Mathf.Min(area.width, area.height) / 2f - margin, _tuning.NodeRadius * 4f);
        }

        // A node's centre: 1 at the top, then clockwise a semitone every twelfth of the circle.
        private static Vector2 Node(int degree, Vector2 centre, float radius)
        {
            float angle = Mathf.PI / 2f - degree * 2f * Mathf.PI / Degree.Count;
            return centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private static Vector2 Outward(int degree)
        {
            float angle = Mathf.PI / 2f - degree * 2f * Mathf.PI / Degree.Count;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        // Moves the names and the shares to where the mesh draws their nodes and arcs.
        private void Place()
        {
            if (_tuning == null)
            {
                return;
            }

            Layout(out Vector2 centre, out float radius);
            Vector2 middle = rectTransform.rect.center;
            for (int d = 0; d < Degree.Count; d++)
            {
                if (_nodeNames[d] != null)
                {
                    RectTransform name = _nodeNames[d].rectTransform;
                    name.sizeDelta = new Vector2(_tuning.NodeRadius * 2f, _tuning.NodeRadius * 2f);
                    name.anchoredPosition = Node(d, centre, radius) - middle;
                }
            }

            foreach (Arc arc in _arcs)
            {
                arc.Label.anchoredPosition = LabelAt(arc, centre, radius) - middle;
            }
        }

        // Where an arc's share is written: the middle of its curve, or beyond its loop.
        private Vector2 LabelAt(Arc arc, Vector2 centre, float radius)
        {
            Vector2 node = Node(arc.From, centre, radius);
            if (arc.From == arc.To)
            {
                Loop(arc.From, node, out Vector2 loopCentre, out float loopRadius);
                return loopCentre + Outward(arc.From) * (loopRadius + _tuning.ArcLabelSize * 0.8f);
            }

            Vector2 end = Node(arc.To, centre, radius);
            Vector2 control = Control(arc.From, arc.To, node, end);
            return Point(node, control, end, 0.5f);
        }

        // The control point of the quadratic Bézier from a (degree from) to b (degree to). Its middle
        // bows toward the circle's centre: by ArcBend of the chord for a step clockwise (up to six
        // semitones; for six exactly, from the lower degree) and by twice that the other way, so the
        // two directions between a pair stay apart. Bowed outward, an arc between degrees two apart
        // would run through the node between them.
        private Vector2 Control(int from, int to, Vector2 a, Vector2 b)
        {
            int steps = (to - from + Degree.Count) % Degree.Count;
            bool clockwise = steps < Degree.Count / 2 || (steps == Degree.Count / 2 && from < to);
            Vector2 chord = b - a;
            var right = new Vector2(chord.y, -chord.x); // inward for a step clockwise round the circle
            Vector2 inward = clockwise ? right : -right;
            float bend = clockwise ? _tuning.ArcBend : 2f * _tuning.ArcBend;
            return (a + b) / 2f + inward * (2f * bend); // a quadratic's middle is half way to its control
        }

        private static Vector2 Point(Vector2 a, Vector2 control, Vector2 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * control + t * t * b;
        }

        // A loop: a circle outside the node, partly under it.
        private void Loop(int degree, Vector2 node, out Vector2 loopCentre, out float loopRadius)
        {
            loopRadius = _tuning.NodeRadius * 0.62f;
            loopCentre = node + Outward(degree) * (_tuning.NodeRadius + loopRadius * 0.55f);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Place();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_tuning == null)
            {
                return;
            }

            UiTheme theme = UiKit.Theme;
            Layout(out Vector2 centre, out float radius);
            Color32 behind = theme.Muted;
            behind.a = 150;
            Color32 front = theme.Accent;

            foreach (Arc arc in _arcs)
            {
                if (arc.From != _framed)
                {
                    DrawArc(vh, arc, centre, radius, behind);
                }
            }

            foreach (Arc arc in _arcs)
            {
                if (arc.From == _framed)
                {
                    DrawArc(vh, arc, centre, radius, front);
                }
            }

            float r = _tuning.NodeRadius;
            for (int d = 0; d < Degree.Count; d++)
            {
                Vector2 node = Node(d, centre, radius);
                ViewMesh.Disc(vh, node, r, ModeScale.Has(_mask, d) ? theme.Accent : theme.Muted);
                if (d == _framed)
                {
                    ViewMesh.Ring(vh, node, r + FrameGap, r + FrameGap + FrameWidth, theme.Text);
                }
            }
        }

        private void DrawArc(VertexHelper vh, Arc arc, Vector2 centre, float radius, Color32 color)
        {
            float width = Mathf.Lerp(_tuning.ThinnestArc, _tuning.ThickestArc, arc.Share);
            float r = _tuning.NodeRadius;
            float arrow = _tuning.ArrowLength;
            Vector2 a = Node(arc.From, centre, radius);
            _curve.Clear();

            if (arc.From == arc.To)
            {
                // The loop leaves the node on one side and comes back into it on the other: the part of its
                // circle outside the node, which meets the node where |p − node| = r.
                Loop(arc.From, a, out Vector2 loopCentre, out float loopRadius);
                float distance = (loopCentre - a).magnitude;
                float cos = (r * r - distance * distance - loopRadius * loopRadius) / (2f * distance * loopRadius);
                float reach = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f));
                float outward = Mathf.Atan2(Outward(arc.From).y, Outward(arc.From).x);
                for (int i = 0; i <= CurveSteps; i++)
                {
                    float angle = outward - reach + 2f * reach * i / CurveSteps;
                    _curve.Add(loopCentre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * loopRadius);
                }

                DrawWithArrow(vh, a, a, _curve, width, Mathf.Min(arrow, loopRadius), color);
                return;
            }

            Vector2 b = Node(arc.To, centre, radius);
            Vector2 control = Control(arc.From, arc.To, a, b);
            for (int i = 0; i <= CurveSteps; i++)
            {
                _curve.Add(Point(a, control, b, (float)i / CurveSteps));
            }

            DrawWithArrow(vh, a, b, _curve, width, arrow, color);
        }

        // Draws the curve from where it leaves the node it starts at to the arrowhead's base, and the
        // arrowhead, its tip where the curve reaches the node it ends at. A loop starts and ends at one.
        private void DrawWithArrow(VertexHelper vh, Vector2 startNode, Vector2 endNode, List<Vector2> curve, float width,
            float arrow, Color32 color)
        {
            float inside = Sq(_tuning.NodeRadius * 0.98f); // a loop's ends lie on its node's edge
            int first = 0;
            while (first < curve.Count - 1 && (curve[first] - startNode).sqrMagnitude < inside)
            {
                first++;
            }

            int tip = curve.Count - 1;
            while (tip > first && (curve[tip] - endNode).sqrMagnitude < inside)
            {
                tip--;
            }

            int baseIndex = tip;
            while (baseIndex > first && (curve[baseIndex] - curve[tip]).sqrMagnitude < Sq(arrow))
            {
                baseIndex--;
            }

            if (baseIndex <= first)
            {
                return;
            }

            _line.Clear();
            for (int i = first; i <= baseIndex; i++)
            {
                _line.Add(curve[i]);
            }

            ViewMesh.Polyline(vh, _line, width, color);
            Vector2 tipPoint = curve[tip];
            Vector2 basePoint = curve[baseIndex];
            Vector2 along = (tipPoint - basePoint).normalized;
            var side = new Vector2(-along.y, along.x) * Mathf.Max(arrow * 0.45f, width * 1.1f);
            ViewMesh.Triangle(vh, tipPoint, basePoint + side, basePoint - side, color);
        }

        private static float Sq(float value) => value * value;
    }
}
