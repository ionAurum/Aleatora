using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Aleatora, made in code as each scene loads (Docs/Aleatora_Tech.md, Building in code): this
    /// object reads the tuning (AleatoraTuning) and makes the board (HexGrid, on a child at the
    /// origin), what selects its cells (GridSelection), the cells' notes (BoardNotes) and MIDI out (MidiPlayback, which opens the
    /// saved port), and gives the scene's Emitter object (the cone) its Emitter, whose hits go out through MidiPlayback as notes. The scene holds the camera, the Cinemachine camera, the lights and the cone. Quit to Menu
    /// loads the scene afresh, and with it a fresh Aleatora. It makes no sound.
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

        /// <summary>The board's notes: each cell's degree, octave and colour.</summary>
        public BoardNotes Board { get; private set; }

        /// <summary>MIDI out: the open port.</summary>
        public MidiPlayback Midi { get; private set; }

        /// <summary>The emitter, on the scene's Emitter object; null if the scene has none.</summary>
        public Emitter Emitter { get; private set; }

        private const string EmitterObjectName = "Emitter";

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

            Board = board.AddComponent<BoardNotes>();
            Board.Begin(Grid, Tuning.Grid, Tuning.Emitter);

            Midi = gameObject.AddComponent<MidiPlayback>();
            Midi.Begin(Tuning.Midi);

            // The scene's cone becomes the emitter (the emitter plan, EM-2).
            GameObject cone = GameObject.Find(EmitterObjectName);
            if (cone != null)
            {
                Emitter = cone.TryGetComponent(out Emitter emitter) ? emitter : cone.AddComponent<Emitter>();
                Emitter.Begin(Grid, Tuning.Grid, Board, Tuning.Emitter, transform);
                Emitter.Hit += PlayHit; // its notes out (EM-3)
            }
            else
            {
                Debug.LogWarning($"Aleatora: the scene has no active '{EmitterObjectName}' object, so there is no emitter.");
            }
        }

        // A hit's note, out through MIDI (the emitter plan, EM-3).
        private void PlayHit(EmitterHit hit)
        {
            Midi.PlayHit(hit, Tuning.Emitter);
        }

        private void OnDestroy()
        {
            if (Emitter != null)
            {
                Emitter.Hit -= PlayHit;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
