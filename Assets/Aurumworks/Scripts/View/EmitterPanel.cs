using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The emitter's panel (the emitter plan, EM-4; decision 9): at the right of the board, built
    /// with the framework's UI kit in the theme's look, operated by the mouse. It sets the
    /// parameters of decisions 1, 5, 6, 10 and 11 live, on the tuning the board and the emitter read
    /// every frame (EmitterTuning), and follows them when they change elsewhere (the Inspector):
    /// Spray (the emitter's Spraying, as Space); the board's Set, Tonic, Layout, Seed and Show
    /// degrees; the spray's Rate, Spread, Speed and Speed jitter; the spheres' sizes, Bounciness,
    /// Lifetime and Max spheres; the notes' Channel, Grid, Strength, Quantize length, the speeds and
    /// velocities, the curve, the lengths and Max voices. Each control is named by the kit ("Slider:
    /// Rate", "Dropdown: Grid", "Toggle: Spray", "InputField: Seed").
    ///
    /// It isn't a menu: it doesn't count for MenuFocus, and it is hidden while a menu is open. Hide
    /// Panel (H) hides and shows it. It takes the pointer, so a click on it selects no cell and the
    /// wheel over it scrolls it rather than zooming (UiInput). It keeps no selection once the pointer
    /// lets go, so Space, W A S D and the gamepad stay the emitter's; while the Seed field has the
    /// keyboard the emitter's keys and H do nothing. Made by the bootstrap (Aleatora); it builds in
    /// its Start, once the menus' EventSystem is there.
    /// </summary>
    public class EmitterPanel : MonoBehaviour
    {
        /// <summary>The panel's width, in reference pixels of the 1920 × 1080 canvas.</summary>
        public const float Width = 560f;

        private const float Margin = 24f;
        private const float LabelWidth = 170f;
        private const float AmountWidth = 84f;
        private const string HideActionPath = "Player/Hide Panel";

        private readonly List<Action> _refreshers = new();
        private readonly List<TMP_Dropdown> _dropdowns = new();
        private EmitterTuning _tuning;
        private Emitter _emitter;
        private RectTransform _box;
        private InputAction _hideAction;
        private IReadOnlyList<string> _sets;

        /// <summary>The panel's box on its canvas; null until it is built.</summary>
        public RectTransform Box => _box;

        /// <summary>Whether Hide Panel has hidden it.</summary>
        public bool Hidden { get; set; }

        /// <summary>Whether it shows: built, not hidden, and no menu open.</summary>
        public bool IsShowing => _box != null && _box.gameObject.activeSelf;

        /// <summary>Makes the panel, under parent, for the emitter's tuning and the emitter (null: no Spray).</summary>
        public static EmitterPanel Make(Transform parent, EmitterTuning tuning, Emitter emitter)
        {
            var made = new GameObject("Emitter Panel");
            made.transform.SetParent(parent, false);
            var panel = made.AddComponent<EmitterPanel>();
            panel._tuning = tuning;
            panel._emitter = emitter;
            return panel;
        }

        //----------

        private void Start()
        {
            if (_tuning == null)
            {
                return;
            }

            _hideAction = InputSystem.actions != null ? InputSystem.actions.FindAction(HideActionPath) : null;
            if (_hideAction == null)
            {
                Debug.LogError($"EmitterPanel: the project-wide input actions have no {HideActionPath}.");
            }

            Canvas canvas = UiKit.CreateCanvas();
            canvas.name = "Emitter Panel Canvas";
            canvas.transform.SetParent(transform, false);
            canvas.sortingOrder = -1; // under the menus
            Build((RectTransform)canvas.transform);
            Show();
        }

        private void Update()
        {
            if (_box == null)
            {
                return;
            }

            if (_hideAction != null && _hideAction.WasPressedThisFrame() && !MenuFocus.IsAnyOpen && !UiInput.IsTyping)
            {
                Hidden = !Hidden;
            }

            Show();
            if (!IsShowing)
            {
                return;
            }

            foreach (Action refresh in _refreshers)
            {
                refresh();
            }
        }

        // Once the pointer lets go, the panel gives up the selection: a selected control would take
        // Space (Submit) and W A S D (Navigate) from the emitter. A text field keeps it while it has
        // the keyboard, a dropdown while its list is open.
        private void LateUpdate()
        {
            EventSystem events = EventSystem.current;
            GameObject selected = events != null ? events.currentSelectedGameObject : null;
            if (selected == null || _box == null || !selected.transform.IsChildOf(_box) || UiInput.IsTyping || AnyExpanded())
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed))
            {
                return;
            }

            events.SetSelectedGameObject(null);
        }

        private void Show()
        {
            bool show = !Hidden && !MenuFocus.IsAnyOpen;
            if (_box.gameObject.activeSelf == show)
            {
                return;
            }

            _box.gameObject.SetActive(show);
            EventSystem events = EventSystem.current;
            if (!show && events != null && events.currentSelectedGameObject != null
                && events.currentSelectedGameObject.transform.IsChildOf(_box))
            {
                events.SetSelectedGameObject(null);
            }
        }

        private bool AnyExpanded()
        {
            foreach (TMP_Dropdown dropdown in _dropdowns)
            {
                if (dropdown.IsExpanded)
                {
                    return true;
                }
            }

            return false;
        }

        //---------- Building

        private void Build(RectTransform canvas)
        {
            UiTheme theme = UiKit.Theme;
            _box = UiKit.Rect("Panel: Emitter", canvas);
            _box.anchorMin = new Vector2(1f, 0f);
            _box.anchorMax = new Vector2(1f, 1f);
            _box.pivot = new Vector2(1f, 0.5f);
            _box.offsetMin = new Vector2(-Margin - Width, Margin);
            _box.offsetMax = new Vector2(-Margin, -Margin);
            UiKit.PanelBox(_box);
            RectTransform area = UiKit.Anchor(UiKit.Rect("Area", _box), 0f, 0f, 1f, 1f);
            RectTransform column = UiKit.ScrollColumn(area, 12f, 22);

            UiKit.Title(column, "Emitter", 44f, theme.Accent);
            if (_emitter != null)
            {
                Switch(column, "Spray", () => _emitter.Spraying, on => _emitter.Spraying = on);
            }

            Heading(column, "Board");
            _sets = MarkovLibrary.Names;
            var setNames = new List<string>();
            foreach (string set in _sets)
            {
                setNames.Add(ViewText.SetName(set));
            }

            Choice(column, "Set", setNames, SetIndex, index =>
            {
                if (index >= 0 && index < _sets.Count)
                {
                    _tuning.Set = _sets[index];
                }
            });

            var tonics = new List<string>();
            for (int pc = 0; pc < Degree.Count; pc++)
            {
                tonics.Add(ViewText.TonicName(pc));
            }

            Choice(column, "Tonic", tonics, () => _tuning.Tonic, index => _tuning.Tonic = index);
            var layouts = new List<string>();
            foreach (CellLayout layout in Enum.GetValues(typeof(CellLayout)))
            {
                layouts.Add(layout.ToString());
            }

            Choice(column, "Layout", layouts, () => (int)_tuning.Layout, index => _tuning.Layout = (CellLayout)index);
            SeedRow(column);
            Switch(column, "Show degrees", () => _tuning.ShowDegrees, on => _tuning.ShowDegrees = on);

            Heading(column, "Spray");
            Number(column, "Rate", 0f, 50f, false, () => _tuning.Rate, v => _tuning.Rate = v, v => v.ToString("0.0", CultureInfo.InvariantCulture) + "/s");
            Number(column, "Spread", 0f, EmitterTuning.MostSpread, false, () => _tuning.Spread, v => _tuning.Spread = v, v => v.ToString("0", CultureInfo.InvariantCulture) + "°");
            Number(column, "Speed", 2f, 30f, false, () => _tuning.Speed, v => _tuning.Speed = v, Decimal1);
            Number(column, "Speed jitter", 0f, 50f, false, () => _tuning.SpeedJitter * 100f, v => _tuning.SpeedJitter = v / 100f,
                v => v.ToString("0", CultureInfo.InvariantCulture) + "%");

            Heading(column, "Spheres");
            Number(column, "Min size", 0.1f, 1f, false, () => _tuning.MinSize, v =>
            {
                float max = _tuning.MaxSize;
                _tuning.MinSize = v;
                if (v > max)
                {
                    _tuning.MaxSize = v; // the smallest pushes the largest up
                }
            }, Decimal2);
            Number(column, "Max size", 0.1f, 1f, false, () => _tuning.MaxSize, v =>
            {
                float min = _tuning.MinSize;
                _tuning.MaxSize = v;
                if (v < min)
                {
                    _tuning.MinSize = v;
                }
            }, Decimal2);
            Number(column, "Bounciness", 0f, 1f, false, () => _tuning.Bounciness, v => _tuning.Bounciness = v, Decimal2);
            Number(column, "Lifetime", 1f, 30f, false, () => _tuning.Lifetime, v => _tuning.Lifetime = v,
                v => v.ToString("0.0", CultureInfo.InvariantCulture) + " s");
            Number(column, "Max spheres", 1f, 2000f, true, () => _tuning.MaxSpheres, v => _tuning.MaxSpheres = Mathf.RoundToInt(v), Whole);

            Heading(column, "Notes");
            var channels = new List<string>();
            for (int channel = MidiMessage.LowestChannel; channel <= MidiMessage.HighestChannel; channel++)
            {
                channels.Add(channel.ToString(CultureInfo.InvariantCulture));
            }

            Choice(column, "Channel", channels, () => _tuning.Channel - MidiMessage.LowestChannel,
                index => _tuning.Channel = index + MidiMessage.LowestChannel);
            var grids = new List<string>();
            foreach (QuantizeGrid grid in Enum.GetValues(typeof(QuantizeGrid)))
            {
                grids.Add(GridName(grid));
            }

            Choice(column, "Grid", grids, () => (int)_tuning.Grid, index => _tuning.Grid = (QuantizeGrid)index);
            Number(column, "Strength", 0f, 1f, false, () => _tuning.Strength, v => _tuning.Strength = v,
                v => Mathf.RoundToInt(v * 100f).ToString(CultureInfo.InvariantCulture) + "%");
            Switch(column, "Quantize length", () => _tuning.QuantizeLength, on => _tuning.QuantizeLength = on);
            Number(column, "Min speed", 0f, 30f, false, () => _tuning.MinSpeed, v =>
            {
                float max = _tuning.MaxSpeed;
                _tuning.MinSpeed = v;
                if (v > max)
                {
                    _tuning.MaxSpeed = v;
                }
            }, Decimal1);
            Number(column, "Max speed", 0.1f, 60f, false, () => _tuning.MaxSpeed, v =>
            {
                float min = _tuning.MinSpeed;
                _tuning.MaxSpeed = v;
                if (v < min)
                {
                    _tuning.MinSpeed = v;
                }
            }, Decimal1);
            Number(column, "Min velocity", 1f, 127f, true, () => _tuning.MinVelocity, v => _tuning.MinVelocity = Mathf.RoundToInt(v), Whole);
            Number(column, "Max velocity", 1f, 127f, true, () => _tuning.MaxVelocity, v => _tuning.MaxVelocity = Mathf.RoundToInt(v), Whole);
            Number(column, "Velocity curve", 0.1f, 5f, false, () => _tuning.VelocityCurve, v => _tuning.VelocityCurve = v, Decimal2);
            Number(column, "Shortest", 0.05f, 8f, false, () => _tuning.Shortest, v => _tuning.Shortest = v, Beats);
            Number(column, "Longest", 0.05f, 8f, false, () => _tuning.Longest, v => _tuning.Longest = v, Beats);
            Number(column, "Max voices", 1f, HitScheduler.MostVoices, true, () => _tuning.MaxVoices, v => _tuning.MaxVoices = Mathf.RoundToInt(v), Whole);
        }

        private static void Heading(Transform column, string text)
        {
            UiKit.Spacer(column, 6f);
            UiKit.Label(column, text);
        }

        // A slider over min to max, showing format's amount; set is called only for the player's moves.
        private void Number(Transform column, string label, float min, float max, bool whole, Func<float> get, Action<float> set,
            Func<float, string> format)
        {
            bool ready = false;
            Slider slider = UiKit.Slider(column, label, 0f, v =>
            {
                if (ready)
                {
                    set(v);
                }
            }, format, AmountWidth);
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = whole;
            Transform row = slider.transform.parent;
            NameWidth(row);
            TextMeshProUGUI amount = row.GetChild(row.childCount - 1).GetComponent<TextMeshProUGUI>();
            float shown = float.NaN;
            void Refresh()
            {
                float value = get();
                if (value != shown)
                {
                    shown = value;
                    slider.SetValueWithoutNotify(value);
                    amount.text = format(slider.value);
                }
            }

            Refresh();
            ready = true;
            _refreshers.Add(Refresh);
        }

        private void Choice(Transform column, string label, IList<string> options, Func<int> get, Action<int> set)
        {
            TMP_Dropdown dropdown = UiKit.Dropdown(column, label, options, Mathf.Max(0, get()), set);
            NameWidth(dropdown.transform.parent);
            _dropdowns.Add(dropdown);
            _refreshers.Add(() =>
            {
                int value = get();
                if (value >= 0 && value != dropdown.value && !dropdown.IsExpanded)
                {
                    dropdown.SetValueWithoutNotify(value);
                }
            });
        }

        private void Switch(Transform column, string label, Func<bool> get, Action<bool> set)
        {
            Toggle toggle = UiKit.Toggle(column, label, get(), set);
            _refreshers.Add(() =>
            {
                bool value = get();
                if (toggle.isOn != value)
                {
                    toggle.SetIsOnWithoutNotify(value);
                }
            });
        }

        // The seed, a whole number from 0 (as the Melodies page's): anything else puts it back.
        private void SeedRow(Transform column)
        {
            TMP_InputField field = null;
            ulong shown = _tuning.Seed;
            field = MelodiesPage.NumberRow(column, "Seed", shown.ToString(CultureInfo.InvariantCulture), text =>
            {
                if (ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed))
                {
                    _tuning.Seed = seed;
                }

                shown = _tuning.Seed;
                field.SetTextWithoutNotify(shown.ToString(CultureInfo.InvariantCulture));
            }, LabelWidth, 20); // ulong.MaxValue has 20 digits
            field.GetComponent<LayoutElement>().flexibleWidth = 1f;
            _refreshers.Add(() =>
            {
                if (_tuning.Seed != shown && !field.isFocused)
                {
                    shown = _tuning.Seed;
                    field.SetTextWithoutNotify(shown.ToString(CultureInfo.InvariantCulture));
                }
            });
        }

        // The kit's name column is 130 px; the panel's longest names ("Velocity curve") need more.
        private static void NameWidth(Transform row)
        {
            if (row.childCount > 0 && row.GetChild(0).TryGetComponent(out LayoutElement name))
            {
                name.preferredWidth = LabelWidth;
            }
        }

        private int SetIndex()
        {
            string set = _tuning.Set;
            for (int i = 0; i < _sets.Count; i++)
            {
                if (_sets[i] == set)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>A grid's name as the panel lists it: Off, 1/4, 1/8, 1/16, 1/32, 1/8 triplet, 1/16 triplet.</summary>
        public static string GridName(QuantizeGrid grid) => grid switch
        {
            QuantizeGrid.Quarter => "1/4",
            QuantizeGrid.Eighth => "1/8",
            QuantizeGrid.Sixteenth => "1/16",
            QuantizeGrid.ThirtySecond => "1/32",
            QuantizeGrid.EighthTriplet => "1/8 triplet",
            QuantizeGrid.SixteenthTriplet => "1/16 triplet",
            _ => "Off",
        };

        private static string Whole(float value) => Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);

        private static string Decimal1(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);

        private static string Decimal2(float value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string Beats(float value) => value.ToString("0.00", CultureInfo.InvariantCulture) + " b";
    }
}
