using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The settings menu, built in code on the framework's menu shell (SettingsPage): fullscreen,
    /// kept in the framework's Settings and applied as it changes. The main menu and the session
    /// menu open it over themselves (SettingsMenu.Instance.Open); Back, UI Cancel and UI Menu
    /// (Escape, the gamepad's East and Start) close it, which writes the settings to disk. Aleatora
    /// makes no sound, so it lists no volumes and has no mixer (the template's has both). On the
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
            return menus != null ? new SettingsPage().Open(menus) : null;
        }
    }
}
