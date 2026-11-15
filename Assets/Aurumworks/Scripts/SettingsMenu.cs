using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The settings menu, built in code on the framework's menu shell (SettingsPage): fullscreen,
    /// kept in the framework's Settings and applied as it changes. The main menu and the session
    /// menu open it over themselves (SettingsMenu.Instance.Open); Back, UI Cancel and UI Menu
    /// (Escape, the gamepad's East and Start) close it, which writes the settings to disk. Aleatora
    /// makes no sound, so it lists no volumes and has no mixer (the template's has both). Its own
    /// row is MIDI (MIDI-3): a button showing the MIDI output, which opens the MIDI page. On the
    /// Menus prefab, with SessionMenu.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        /// <summary>The game's settings menu, or null before the game has started.</summary>
        public static SettingsMenu Instance { get; private set; }

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        //----------

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        //----------

        /// <summary>
        /// Opens the menu over whatever is showing, with the values of this moment, and returns it;
        /// null before the menu stack exists.
        /// </summary>
        public RectTransform Open()
        {
            MenuStack menus = SessionMenu.Menus;
            return menus != null ? new SettingsPage { More = column => MidiRow(column, menus) }.Open(menus) : null;
        }

        // MIDI: the output's name, on a button that opens the MIDI page; it shows the choice made there as that closes.
        private static void MidiRow(RectTransform column, MenuStack menus)
        {
            MidiPlayback midi = Aleatora.Instance != null ? Aleatora.Instance.Midi : null;
            if (midi == null)
            {
                return;
            }

            TextMeshProUGUI label = null;
            Button button = UiKit.Button(column, "MIDI", () =>
                MidiPage.Open(menus, midi, () =>
                {
                    if (label != null)
                    {
                        label.text = Label(midi);
                    }
                }), 56f);
            label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.text = Label(midi);
        }

        private static string Label(MidiPlayback midi) => "MIDI: " + MidiPage.OutputName(midi);
    }
}
