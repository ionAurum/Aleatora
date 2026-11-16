using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Static: the MIDI page (MIDI-3; the MIDI plan's decision 2), a dialog on the menu stack opened
    /// from the Settings page's MIDI row and from Output on the Melodies page. In two columns:
    ///
    /// At the left the output ports as buttons, None first, the chosen one in the accent colour (so
    /// it has the selection as the page opens). A saved port that isn't there shows as "Greedo (not
    /// found)"; why the chosen port isn't open shows under the list. Pressing a port saves it as this
    /// machine's choice and opens it (MidiPlayback.Choose); playing goes on, on the new port.
    ///
    /// At the right (IN-3; the MIDI in plan's decisions 2, 3 and 5) the inputs as toggles, any number
    /// on, each saved (MidiPlayback.ChooseInput): the ports listed, then inputs open that aren't (a
    /// stand-in), then inputs chosen that aren't there ("Han (not found)"). The output's own port is
    /// greyed, "(the output)", and can't be turned on. Why a chosen input isn't open shows under them.
    /// Then Clock (Aleatora, then each open input; ChooseClock) and Send clock (ChooseSendClock).
    ///
    /// Under both: Monitor (MidiMonitorPage), Refresh and Back. Each control is named by the kit
    /// ("Button: Greedo", "Toggle: Han", "Dropdown: Clock", "Toggle: Send clock").
    /// </summary>
    public static class MidiPage
    {
        /// <summary>None's label: no port.</summary>
        public const string None = "None";

        /// <summary>The Clock dropdown's first option: Aleatora keeps its own clock.</summary>
        public const string OwnClock = "Aleatora";

        /// <summary>The page's width, a share of the screen's: two columns.</summary>
        public const float Width = 0.6f;

        /// <summary>
        /// Opens the page over whatever is open; closed is called each time it closes (a choice
        /// opens it again, built afresh). Null without a MidiPlayback.
        /// </summary>
        public static RectTransform Open(MenuStack menus, MidiPlayback midi, Action closed = null)
        {
            if (menus == null || midi == null)
            {
                return null;
            }

            float width = menus.DialogWidth;
            menus.DialogWidth = Width;
            try
            {
                return menus.OpenDialog("MIDI", column => Build(column, menus, midi, closed), closed);
            }
            finally
            {
                menus.DialogWidth = width;
            }
        }

        /// <summary>What a button showing the port says: the open output's name, None, or the saved port's name and why it isn't open.</summary>
        public static string OutputName(MidiPlayback midi)
        {
            if (midi == null)
            {
                return None;
            }

            if (midi.IsOpen)
            {
                return midi.Output.Name;
            }

            string chosen = midi.Chosen;
            return string.IsNullOrEmpty(chosen) ? None : chosen + (IsListed(chosen) ? " (not open)" : " (not found)");
        }

        /// <summary>
        /// The inputs the page lists, in order, as (name, label): each port listed (the output's own
        /// labelled "(the output)"), then each open input not listed, then each chosen input neither
        /// listed nor open, labelled "(not found)".
        /// </summary>
        public static List<(string Name, string Label)> InputRows(MidiPlayback midi)
        {
            var rows = new List<(string, string)>();
            var seen = new HashSet<string>();
            foreach (string name in MidiPorts.Inputs)
            {
                if (seen.Add(name))
                {
                    rows.Add((name, midi.IsOutputName(name) ? name + " (the output)" : name));
                }
            }

            foreach (string name in midi.InputNames)
            {
                if (seen.Add(name))
                {
                    rows.Add((name, name));
                }
            }

            foreach (string name in midi.ChosenInputs)
            {
                if (seen.Add(name))
                {
                    rows.Add((name, name + " (not found)"));
                }
            }

            return rows;
        }

        //----------

        private static void Build(RectTransform column, MenuStack menus, MidiPlayback midi, Action closed)
        {
            UiTheme theme = UiKit.Theme;
            UiKit.Title(column, "MIDI", 60f, theme.Accent, TextAlignmentOptions.Center);

            RectTransform columns = UiKit.Rect("Columns", column);
            var across = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
            across.spacing = 40f;
            across.childControlWidth = across.childControlHeight = true;
            across.childForceExpandWidth = across.childForceExpandHeight = true;
            RectTransform outputs = Column(columns, "Outputs");
            RectTransform inputs = Column(columns, "Inputs");
            BuildOutputs(outputs, menus, midi, closed);
            BuildInputs(inputs, menus, midi, closed);

            UiKit.Spacer(column, 10f);
            RectTransform row = UiKit.Row(column, 56f, 16f);
            UiKit.Button(row, "Monitor", () => MidiMonitorPage.Open(menus, midi), 56f);
            UiKit.Button(row, "Refresh", () => Reopen(menus, midi, closed, "Button: Refresh"), 56f);
            UiKit.Button(row, "Back", menus.Close, 56f);
        }

        private static RectTransform Column(RectTransform parent, string name)
        {
            RectTransform column = UiKit.Rect(name, parent);
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var size = column.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = 1f; // the same for both, so the room is shared equally whatever they hold
            size.flexibleWidth = 1f;
            return column;
        }

        private static void BuildOutputs(RectTransform column, MenuStack menus, MidiPlayback midi, Action closed)
        {
            UiTheme theme = UiKit.Theme;
            UiKit.Text(column, "Output", 26f, theme.Muted);
            string chosen = midi.Chosen;
            PortButton(column, None, string.Empty, chosen, menus, midi, closed);
            bool listed = string.IsNullOrEmpty(chosen);
            foreach (string name in MidiPorts.Outputs)
            {
                PortButton(column, name, name, chosen, menus, midi, closed);
                listed |= name == chosen;
            }

            if (!listed)
            {
                PortButton(column, chosen + " (not found)", chosen, chosen, menus, midi, closed);
            }

            if (!string.IsNullOrEmpty(midi.Error))
            {
                UiKit.Text(column, midi.Error, 22f, theme.Danger, align: TextAlignmentOptions.Center);
            }
        }

        private static void BuildInputs(RectTransform column, MenuStack menus, MidiPlayback midi, Action closed)
        {
            UiTheme theme = UiKit.Theme;
            UiKit.Text(column, "Inputs", 26f, theme.Muted);
            List<(string Name, string Label)> rows = InputRows(midi);
            var chosen = new HashSet<string>(midi.ChosenInputs);
            var errors = new List<string>();
            if (rows.Count == 0)
            {
                UiKit.Text(column, "No inputs", 22f, theme.Muted);
            }

            foreach ((string name, string label) in rows)
            {
                bool output = midi.IsOutputName(name);
                bool on = !output && (midi.IsInputOpen(name) || chosen.Contains(name));
                Toggle toggle = UiKit.Toggle(column, label, on, isOn =>
                {
                    midi.ChooseInput(name, isOn);
                    Reopen(menus, midi, closed, "Toggle: " + label);
                });
                TextMeshProUGUI text = toggle.GetComponentInChildren<TextMeshProUGUI>();
                text.overflowMode = TextOverflowModes.Ellipsis;
                if (output)
                {
                    toggle.interactable = false; // Aleatora would hear its own notes
                    text.color = theme.Muted;
                }

                string error = output || label != name ? null : midi.InputError(name); // "(not found)" says it already
                if (!string.IsNullOrEmpty(error) && on)
                {
                    errors.Add(error);
                }
            }

            foreach (string error in errors)
            {
                UiKit.Text(column, error, 22f, theme.Danger, align: TextAlignmentOptions.Center);
            }

            UiKit.Spacer(column, 6f);
            var names = new List<string> { string.Empty };
            var options = new List<string> { OwnClock };
            foreach (string name in midi.InputNames)
            {
                names.Add(name);
                options.Add(name);
            }

            string clock = midi.ClockName;
            if (clock.Length > 0 && !names.Contains(clock))
            {
                names.Add(clock);
                options.Add(clock + " (not open)");
            }

            UiKit.Dropdown(column, "Clock", options, Math.Max(0, names.IndexOf(clock)), index =>
            {
                if (index >= 0 && index < names.Count)
                {
                    midi.ChooseClock(names[index]);
                }
            });
            UiKit.Toggle(column, "Send clock", midi.SendClock, midi.ChooseSendClock);
        }

        // A port's button: the chosen one in the accent colour. Pressing it chooses it, and the page opens again to show it.
        private static void PortButton(RectTransform column, string label, string port, string chosen, MenuStack menus,
            MidiPlayback midi, Action closed)
        {
            bool isChosen = port == (chosen ?? string.Empty);
            UiKit.Button(column, label, () =>
            {
                midi.Choose(port);
                Reopen(menus, midi, closed, null);
            }, 56f, isChosen ? UiKit.Theme.Accent : (Color?)null);
        }

        // Built afresh: the lists as Windows has them now, and the choices. The selection goes to
        // select, if given, else to the chosen port.
        private static void Reopen(MenuStack menus, MidiPlayback midi, Action closed, string select)
        {
            menus.Close();
            RectTransform page = Open(menus, midi, closed);
            if (select != null && page != null && EventSystem.current != null)
            {
                foreach (Transform child in page.GetComponentsInChildren<Transform>())
                {
                    if (child.name == select)
                    {
                        EventSystem.current.SetSelectedGameObject(child.gameObject);
                        break;
                    }
                }
            }
        }

        private static bool IsListed(string name)
        {
            foreach (string output in MidiPorts.Outputs)
            {
                if (output == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
