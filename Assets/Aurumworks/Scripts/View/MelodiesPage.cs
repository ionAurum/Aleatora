using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The Melodies page (M3; the plan's decision 4), opened through the menu stack from the main
    /// menu and the session menu, so Back, Cancel, the selection and MenuFocus work as for every
    /// menu. At the left its controls: the set, the order, the temperature, the tonic, the seed,
    /// Generate (32 notes from the seed), Step (one more) and Back. At the right what they make: the
    /// set's unigram and the distribution the last note was drawn from (DegreeBars), the order-1
    /// table as a grid and as a graph (TransitionGrid, TransitionGraph), and the melody (MelodyStrip).
    /// It works on a copy of the tuning's Melodies section, never the asset, and plays nothing.
    /// </summary>
    public class MelodiesPage : MonoBehaviour
    {
        /// <summary>How many notes Generate makes.</summary>
        public const int MelodyLength = 32;

        /// <summary>The Temperature slider's lowest value; its highest is the reciprocal, on a log scale between.</summary>
        public const float LowestTemperature = 0.25f;

        private const float ControlsWidth = 560f;
        private const float Margin = 40f;

        private readonly List<int> _degrees = new List<int>();
        private readonly List<int> _notes = new List<int>();
        private readonly List<bool> _inMode = new List<bool>();
        private readonly float[] _distribution = new float[Degree.Count];
        private readonly int[] _context = new int[1]; // a context of one degree, for the table
        private IReadOnlyList<string> _sets;
        private MelodyTuning _work;
        private MelodyViewTuning _view;
        private MelodyViewTuning _viewSeen;
        private MarkovTable _table;
        private int _mask = ModeScale.All;
        private MelodyGenerator _generator;
        private Voicing _voicing;
        private bool _hasStep;
        private MelodyStep _last;

        private DegreeBars _setBars;
        private DegreeBars _nextBars;
        private TransitionGrid _grid;
        private TransitionGraph _graph;
        private MelodyStrip _strip;
        private TMP_InputField _seedField;

        /// <summary>The working copy of the tuning the page changes.</summary>
        public MelodyTuning Work => _work;

        /// <summary>The set's table shown.</summary>
        public MarkovTable Table => _table;

        /// <summary>The melody so far, as degrees, oldest first.</summary>
        public IReadOnlyList<int> Degrees => _degrees;

        /// <summary>The last step made, and whether there is one.</summary>
        public bool TryGetLastStep(out MelodyStep step)
        {
            step = _last;
            return _hasStep;
        }

        /// <summary>The set's unigram.</summary>
        public DegreeBars SetBars => _setBars;

        /// <summary>Next note: the distribution the last note was drawn from, or a clicked row.</summary>
        public DegreeBars NextBars => _nextBars;

        /// <summary>The order-1 table as a grid.</summary>
        public TransitionGrid Grid => _grid;

        /// <summary>The order-1 table as a graph.</summary>
        public TransitionGraph Graph => _graph;

        /// <summary>The melody's last notes.</summary>
        public MelodyStrip Strip => _strip;

        /// <summary>
        /// Opens the page over whatever is open, from the game's tuning (or the defaults if the game
        /// hasn't started), with Generate selected and a melody made from the tuning's seed.
        /// </summary>
        public static MelodiesPage Open(MenuStack menus)
        {
            AleatoraTuning tuning = Aleatora.Instance != null ? Aleatora.Instance.Tuning : AleatoraTuning.Load();
            RectTransform panel = menus.Open("Melodies");
            var page = panel.gameObject.AddComponent<MelodiesPage>();
            page.Build(panel, menus, tuning);
            MenuStack.SelectMainAction(panel);
            return page;
        }

        //---------- What the controls do

        /// <summary>A new melody of MelodyLength notes from the seed, on the set and settings as they are.</summary>
        public void Generate()
        {
            if (_table == null)
            {
                return;
            }

            _generator = new MelodyGenerator(_table, _work.Generator, _work.Seed);
            _voicing = _work.MakeVoicing();
            _degrees.Clear();
            _notes.Clear();
            _inMode.Clear();
            for (int i = 0; i < MelodyLength; i++)
            {
                StepOnce();
            }

            Refresh();
        }

        /// <summary>One more note.</summary>
        public void Step()
        {
            if (_generator == null)
            {
                Generate();
                return;
            }

            StepOnce();
            Refresh();
        }

        private void StepOnce()
        {
            _last = _generator.Next(_distribution);
            _hasStep = true;
            _degrees.Add(_last.Degree);
            _notes.Add(_voicing.Place(_last.Degree));
            _inMode.Add(ModeScale.Has(_mask, _last.Degree));
        }

        private void OnSet(int index)
        {
            if (index < 0 || index >= _sets.Count || !MarkovLibrary.TryGet(_sets[index], out MarkovTable table))
            {
                return;
            }

            _work.Set = _sets[index];
            ShowSet(table);
            Generate();
        }

        private void OnOrder(float value)
        {
            GeneratorSettings settings = _work.Generator;
            settings.Order = Mathf.RoundToInt(value);
            _work.Generator = settings;
            if (_generator != null)
            {
                _generator.Settings = _work.Generator;
            }
        }

        private void OnTemperature(float value)
        {
            GeneratorSettings settings = _work.Generator;
            settings.Temperature = Temperature(value);
            _work.Generator = settings;
            if (_generator != null)
            {
                _generator.Settings = _work.Generator;
            }
        }

        // The tonic's pitch class changes in its octave; the melody so far is placed again from it.
        private void OnTonic(int pitchClass)
        {
            int tonic = _work.Tonic;
            _work.Tonic = tonic - ((tonic % Degree.Count) + Degree.Count) % Degree.Count + pitchClass;
            _voicing = _work.MakeVoicing();
            _notes.Clear();
            foreach (int degree in _degrees)
            {
                _notes.Add(_voicing.Place(degree));
            }

            Refresh();
        }

        private void OnSeed(string text)
        {
            if (ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed))
            {
                _work.Seed = seed;
            }

            _seedField.SetTextWithoutNotify(_work.Seed.ToString(CultureInfo.InvariantCulture));
        }

        // A row clicked in the grid or a node in the graph: what follows that degree, in Next note.
        private void ShowRow(int degree)
        {
            int[] context = _context;
            context[0] = degree;
            string after = "after " + ViewText.DegreeName(degree) + " · order 1 · ";
            if (_table.TryGetCounts(context, out ReadOnlySpan<int> counts))
            {
                _nextBars.Show(counts, _mask, -1, after + ViewText.Count(_table.Total(context)) + " notes");
            }
            else
            {
                Span<float> none = stackalloc float[Degree.Count];
                _nextBars.Show(none, _mask, -1, after + "never heard");
            }
        }

        /// <summary>The Temperature slider's value (0–1) as a temperature, LowestTemperature to its reciprocal on a log scale.</summary>
        public static float Temperature(float slider) => LowestTemperature * Mathf.Pow(1f / (LowestTemperature * LowestTemperature), Mathf.Clamp01(slider));

        /// <summary>A temperature as the Temperature slider's value: the inverse of Temperature.</summary>
        public static float TemperatureSlider(float temperature) =>
            Mathf.Clamp01(Mathf.Log(Mathf.Max(temperature, 1e-6f) / LowestTemperature) / Mathf.Log(1f / (LowestTemperature * LowestTemperature)));

        //---------- Showing

        private void ShowSet(MarkovTable table)
        {
            _table = table;
            _mask = ModeScale.Mask(table.Mode);
            Span<int> unigram = stackalloc int[Degree.Count];
            for (int d = 0; d < Degree.Count; d++)
            {
                unigram[d] = table.Unigram(d);
            }

            _setBars.Show(unigram, _mask, -1, ViewText.Count(table.Tunes) + " melodies · " + ViewText.Count(table.Notes) + " notes");
            _grid.Show(table, _mask);
            _viewSeen = _view.Copy();
            _graph.Show(table, _mask, _view);
        }

        private void Refresh()
        {
            _strip.Show(_notes, _inMode, _voicing.Low, _voicing.High, _voicing.Tonic);
            int newest = _degrees.Count > 0 ? _degrees[_degrees.Count - 1] : -1;
            _grid.Frame(newest);
            _graph.Frame(newest);
            if (_hasStep)
            {
                _nextBars.Show(_distribution, _mask, _last.Degree, StepCaption());
            }
        }

        // "after 3 5 · order 2 of 4 · 1,526 notes": the context used, how far it backed off, and its evidence.
        private string StepCaption()
        {
            if (_last.IsFirst)
            {
                return "first note · from how " + ViewText.Count(_last.ContextTotal) + " melodies begin";
            }

            var caption = new System.Text.StringBuilder();
            if (_last.OrderUsed > 0)
            {
                caption.Append("after");
                int chosen = _degrees.Count - 1;
                for (int i = chosen - _last.OrderUsed; i < chosen; i++)
                {
                    caption.Append(' ').Append(ViewText.DegreeName(_degrees[i]));
                }

                caption.Append(" · ");
            }

            caption.Append("order ").Append(_last.OrderUsed).Append(" of ").Append(_last.OrderAsked).Append(" · ")
                .Append(ViewText.Count(_last.ContextTotal)).Append(" notes");
            return caption.ToString();
        }

        // The tuning is read live: a change to how the graph draws shows at once.
        private void Update()
        {
            if (_table != null && !_view.SameAs(_viewSeen))
            {
                _viewSeen = _view.Copy();
                _graph.Show(_table, _mask, _view);
            }
        }

        //---------- Building

        private void Build(RectTransform panel, MenuStack menus, AleatoraTuning tuning)
        {
            UiTheme theme = UiKit.Theme;
            _work = tuning.Melodies.Copy();
            _view = tuning.MelodyView;
            _sets = MarkovLibrary.Names;

            // The page covers what it opens over: the menu under it would show through its panels.
            if (panel.TryGetComponent(out Image backdrop))
            {
                backdrop.color = theme.Background;
            }

            // The controls, in a column at the left.
            RectTransform box = UiKit.Rect("Controls", panel);
            box.anchorMin = new Vector2(0f, 0f);
            box.anchorMax = new Vector2(0f, 1f);
            box.offsetMin = new Vector2(Margin, Margin);
            box.offsetMax = new Vector2(Margin + ControlsWidth, -Margin);
            UiKit.PanelBox(box);
            RectTransform column = UiKit.Anchor(UiKit.Rect("Column", box), 0f, 0f, 1f, 1f);
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.padding = new RectOffset(32, 32, 30, 32);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            UiKit.Title(column, "Melodies", 56f, theme.Accent);
            UiKit.Spacer(column, 4f);

            var setNames = new List<string>();
            int setIndex = 0;
            for (int i = 0; i < _sets.Count; i++)
            {
                setNames.Add(ViewText.SetName(_sets[i]));
                if (_sets[i] == _work.Set)
                {
                    setIndex = i;
                }
            }

            UiKit.Dropdown(column, "Set", setNames, setIndex, OnSet);

            GeneratorSettings settings = _work.Generator;
            Slider order = UiKit.Slider(column, "Order", 0f, OnOrder,
                v => Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture));
            order.minValue = 0f;
            order.maxValue = GeneratorSettings.HighestOrder;
            order.wholeNumbers = true;
            order.value = settings.Order; // through the listener, which writes the amount

            UiKit.Slider(column, "Temperature", TemperatureSlider(settings.Temperature), OnTemperature,
                v => Temperature(v).ToString("0.00", CultureInfo.InvariantCulture), 70f);

            var tonics = new List<string>();
            for (int pc = 0; pc < Degree.Count; pc++)
            {
                tonics.Add(ViewText.TonicName(pc));
            }

            UiKit.Dropdown(column, "Tonic", tonics, ((_work.Tonic % Degree.Count) + Degree.Count) % Degree.Count, OnTonic);
            _seedField = SeedRow(column);

            UiKit.Spacer(column, 6f);
            UiKit.Button(column, "Generate", Generate, 64f, theme.Accent);
            UiKit.Button(column, "Step", Step, 56f);
            UiKit.Rect("Space", column).gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            UiKit.Button(column, "Back", menus.Close, 56f);

            // What they make, at the right.
            RectTransform views = UiKit.Anchor(UiKit.Rect("Views", panel), 0f, 0f, 1f, 1f);
            views.offsetMin = new Vector2(Margin + ControlsWidth + 30f, Margin);
            views.offsetMax = new Vector2(-Margin, -Margin);
            // The graph, which needs the most room, takes the height above the strip; the bars and the
            // grid are stacked at its left.
            _setBars = DegreeBars.On(Panel(views, "The set", 0f, 0.8f, 0.43f, 1f));
            _nextBars = DegreeBars.On(Panel(views, "Next note", 0f, 0.59f, 0.43f, 0.79f));
            _grid = TransitionGrid.On(Panel(views, "Transitions", 0f, 0.215f, 0.43f, 0.58f));
            _graph = TransitionGraph.On(Panel(views, "Transition graph", 0.44f, 0.215f, 1f, 1f));
            _strip = MelodyStrip.On(Panel(views, "Melody", 0f, 0f, 1f, 0.2f));
            _grid.RowClicked += ShowRow;
            _graph.NodeClicked += ShowRow;

            if (MarkovLibrary.TryGet(_work.Set, out MarkovTable table)
                || (_sets.Count > 0 && MarkovLibrary.TryGet(_sets[setIndex], out table)))
            {
                _work.Set = table.Name;
                ShowSet(table);
                Generate();
            }
        }

        // The seed: a whole number from 0, in a field labelled as the kit labels its controls.
        private TMP_InputField SeedRow(Transform column)
        {
            UiTheme theme = UiKit.Theme;
            RectTransform row = UiKit.Rect("Seed", column);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var size = row.gameObject.AddComponent<LayoutElement>();
            size.preferredHeight = size.minHeight = 56f;

            TextMeshProUGUI name = UiKit.Text(row, "Seed", 24f, theme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft,
                theme.BodyBold);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;

            TMP_InputField field = UiKit.InputField(row, _work.Seed.ToString(CultureInfo.InvariantCulture), OnSeed,
                TMP_InputField.ContentType.IntegerNumber);
            field.gameObject.name = "InputField: Seed";
            field.characterLimit = 20; // ulong.MaxValue has 20 digits
            field.GetComponent<LayoutElement>().flexibleWidth = 1f;
            return field;
        }

        // A framed panel with its name at the top left; returns the area inside it to draw in.
        private static RectTransform Panel(RectTransform parent, string title, float xMin, float yMin, float xMax, float yMax)
        {
            RectTransform rect = UiKit.Anchor(UiKit.Rect("Panel: " + title, parent), xMin, yMin, xMax, yMax);
            UiKit.PanelBox(rect);
            TextMeshProUGUI label = UiKit.Label(rect, title);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            RectTransform labelRect = UiKit.Anchor(label.rectTransform, 0f, 1f, 1f, 1f);
            labelRect.offsetMin = new Vector2(20f, -40f);
            labelRect.offsetMax = new Vector2(-20f, -14f);
            RectTransform area = UiKit.Anchor(UiKit.Rect("Area", rect), 0f, 0f, 1f, 1f);
            area.offsetMin = new Vector2(20f, 14f);
            area.offsetMax = new Vector2(-20f, -46f);
            return area;
        }
    }
}
