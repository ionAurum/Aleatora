using UnityEngine;
using UnityEngine.EventSystems;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The in-game menu, built in code on the framework's menu shell (SessionMenuPage): Resume,
    /// Settings, Melodies and Quit to Menu. The UI Menu action (Escape, the gamepad's Start) opens it during
    /// play; Menu or UI Cancel (Escape, the gamepad's East) closes the menu on top. It doesn't pause
    /// anything; what the player steers checks MenuFocus.IsAnyOpen.
    ///
    /// Also owns what every menu is drawn on and opens through: the Canvas, the EventSystem and the
    /// menu stack (Menus). No scene holds them: the Menus prefab in Resources, which this is on with
    /// MainMenu and SettingsMenu, is made as the game starts and outlives scene loads.
    /// </summary>
    public class SessionMenu : MonoBehaviour
    {
        private const string PrefabName = "Menus"; // in a Resources folder

        [Tooltip("How everything built in code looks: colours, corners, motion and sounds (Create > Aurumworks > " +
                 "UI Theme). Empty: the kit's plain default.")]
        [SerializeField] private UiTheme _theme;

        private SessionMenuPage _page; // the open menu, or null

        /// <summary>The stack every menu opens through, or null before the game has started.</summary>
        public static MenuStack Menus { get; private set; }

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Menus = null;

        // After the first scene's Awakes, whichever scene that is, and before any Start.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var prefab = Resources.Load<SessionMenu>(PrefabName);
            if (prefab == null)
            {
                Debug.LogError($"No {PrefabName} prefab with a SessionMenu in a Resources folder: the game has no menus.");
                return;
            }

            DontDestroyOnLoad(Instantiate(prefab).gameObject);
        }

        //----------

        // Before anything is built: MainMenu builds in its Start.
        private void Awake()
        {
            if (_theme != null)
            {
                UiKit.Theme = _theme;
            }

            Canvas canvas = UiKit.CreateCanvas();
            canvas.transform.SetParent(transform, false);

            // The kit made the EventSystem in the scene, which Quit to Menu loads afresh.
            if (EventSystem.current != null)
            {
                EventSystem.current.transform.SetParent(transform, false);
            }

            Menus = MenuStack.On((RectTransform)canvas.transform);
            Menus.MenuPressed += Open;
        }

        private void OnDestroy()
        {
            if (Menus != null && Menus.transform.IsChildOf(transform))
            {
                Menus = null;
            }
        }

        //----------

        // Menu was pressed with no menu of the stack's open. On the main menu there is no game to
        // come back from.
        private void Open()
        {
            if (_page != null || MainMenu.IsShowing)
            {
                return;
            }

            _page = new SessionMenuPage
            {
                Settings = OnSettings,
                LeaveLabel = "Quit to Menu",
                Leave = OnQuitToMenu,
                Closed = () => _page = null,
            };
            _page.Button("Melodies", () => MelodiesPage.Open(Menus));
            _page.Open(Menus);
        }

        private void OnSettings()
        {
            if (SettingsMenu.Instance != null)
            {
                SettingsMenu.Instance.Open();
            }
        }

        private void OnQuitToMenu()
        {
            Menus.CloseAll();
            if (MainMenu.Instance != null)
            {
                MainMenu.Instance.EndGame();
            }
        }
    }
}
