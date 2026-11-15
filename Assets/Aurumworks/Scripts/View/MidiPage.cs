using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Static: the MIDI page (MIDI-3; the MIDI plan's decision 2), a dialog on the menu stack opened
    /// from the Settings page's MIDI row and from Output on the Melodies page. It lists the output
    /// ports as buttons, None first, the chosen one in the accent colour (so it has the selection as
    /// the page opens), then Refresh and Back. A saved port that isn't there shows as "Greedo (not
    /// found)"; why the chosen port isn't open shows under the list. Pressing a port saves it as this
    /// machine's choice and opens it (MidiPlayback.Choose); playing goes on, on the new port. Each
    /// button is named by the kit ("Button: Greedo", "Button: None", "Button: Refresh").
    /// </summary>
    public static class MidiPage
    {
        /// <summary>None's label: no port.</summary>
        public const string None = "None";

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

            return menus.OpenDialog("MIDI", column => Build(column, menus, midi, closed), closed);
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

        //----------

        private static void Build(RectTransform column, MenuStack menus, MidiPlayback midi, Action closed)
        {
            UiTheme theme = UiKit.Theme;
            UiKit.Title(column, "MIDI", 60f, theme.Accent, TextAlignmentOptions.Center);
            UiKit.Text(column, "Output", 26f, theme.Muted);

            string chosen = midi.Chosen;
            IReadOnlyList<string> outputs = MidiPorts.Outputs;
            PortButton(column, None, string.Empty, chosen, menus, midi, closed);
            bool listed = string.IsNullOrEmpty(chosen);
            foreach (string name in outputs)
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

            UiKit.Spacer(column, 10f);
            UiKit.Button(column, "Refresh", () => Reopen(menus, midi, closed, "Button: Refresh"), 56f);
            UiKit.Button(column, "Back", menus.Close, 56f);
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

        // Built afresh: the list as Windows has it now, and the choice. The selection goes to select,
        // if given, else to the chosen port.
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
