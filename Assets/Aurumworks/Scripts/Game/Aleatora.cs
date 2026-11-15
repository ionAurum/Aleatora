using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Aleatora, made in code as each scene loads (Docs/Aleatora_Tech.md, Building in code): this
    /// object reads the tuning (AleatoraTuning) and makes the board (HexGrid, on a child at the
    /// origin) and what selects its cells (GridSelection). The scene holds the camera, the
    /// Cinemachine camera and the lights. Quit to Menu loads the scene afresh, and with it a fresh
    /// Aleatora. It makes no sound.
    /// </summary>
    public class Aleatora : MonoBehaviour
    {
        /// <summary>Aleatora as it runs, or null before the scene has loaded.</summary>
        public static Aleatora Instance { get; private set; }

        /// <summary>The tuning: the asset, or the defaults if there is none.</summary>
        public AleatoraTuning Tuning { get; private set; }

        /// <summary>The board.</summary>
        public HexGrid Grid { get; private set; }

        /// <summary>What selects the board's cells.</summary>
        public GridSelection Selection { get; private set; }

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // The first scene has loaded by now; later loads (Quit to Menu) come through sceneLoaded.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Begin()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Create();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
            {
                Create();
            }
        }

        private static void Create()
        {
            if (Instance == null)
            {
                _ = new GameObject("Aleatora", typeof(Aleatora));
            }
        }

        //----------

        private void Awake()
        {
            Instance = this;
            Tuning = AleatoraTuning.Load();

            var board = new GameObject("HexGrid");
            board.transform.SetParent(transform, false);
            Grid = board.AddComponent<HexGrid>();
            Grid.Begin(Tuning.Grid);

            Selection = board.AddComponent<GridSelection>();
            Selection.Begin(Grid, Tuning.Grid);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
