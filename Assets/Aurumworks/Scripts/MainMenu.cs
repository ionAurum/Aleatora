using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The main menu, built in code in the frame every Aurumworks game's has (MainMenuFrame): the
    /// title, Play and Melodies under it, and along the bottom Settings and Quit with the version. It is drawn
    /// over the scene the game starts in, which runs behind it; Play hides it, and Quit to Menu
    /// (SessionMenu) loads that scene afresh and shows it again. While it shows it counts as an open
    /// menu (MenuFocus): whatever steers a character or turns a camera checks MenuFocus.IsAnyOpen
    /// and waits. On the Menus prefab, with SessionMenu.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string _title = "Aleatora";
        [Tooltip("How far the scene behind the menu is darkened, so the menu reads over it (0 = not at all).")]
        [SerializeField, Range(0f, 1f)] private float _dim = 0.45f;

        private RectTransform _screen;

        /// <summary>The game's main menu, or null before the game has started.</summary>
        public static MainMenu Instance { get; private set; }

        /// <summary>True while the main menu is what the player sees, with or without Settings over it.</summary>
        public static bool IsShowing => Instance != null && Instance._screen != null && Instance._screen.gameObject.activeSelf;

        private static MenuStack Menus => SessionMenu.Menus;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        //----------

        private void Awake() => Instance = this;

        // SessionMenu's Awake has set the theme and made the stack.
        private void Start()
        {
            if (Menus == null)
            {
                return;
            }

            Build();
            Show();
        }

        private void OnDestroy()
        {
            MenuFocus.SetOpen(this, false);
            if (Instance == this)
            {
                Instance = null;
            }
        }

        //----------

        /// <summary>Ends the game in play: loads its scene afresh, with the main menu over it.</summary>
        public void EndGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Show();
        }

        //----------

        private void Build()
        {
            _screen = UiKit.Anchor(UiKit.Rect("MainMenu", Menus.transform), 0f, 0f, 1f, 1f);
            Color dim = UiKit.Theme.Background;
            dim.a = _dim;
            UiKit.Fill(_screen, dim);

            MainMenuFrame frame = MainMenuFrame.On(_screen, _title);
            UiKit.Button(frame.Choices, "Play", OnPlay, 64f, UiKit.Theme.Accent);
            UiKit.Button(frame.Choices, "Melodies", OnMelodies, 56f);
            frame.FootButton("Settings", OnSettings, 190f);
            frame.FootButton("Quit", OnQuit, 150f);
        }

        private void Show()
        {
            _screen.gameObject.SetActive(true);
            MenuFocus.SetOpen(this, true);
            Menus.Screen = _screen;
            MenuStack.SelectMainAction(_screen);
        }

        private void OnPlay()
        {
            _screen.gameObject.SetActive(false);
            MenuFocus.SetOpen(this, false);
            Menus.Screen = null;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void OnMelodies() => MelodiesPage.Open(Menus);

        private void OnSettings()
        {
            if (SettingsMenu.Instance != null)
            {
                SettingsMenu.Instance.Open();
            }
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}
