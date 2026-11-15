using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Aleatora's numbers and look, tuned in the Inspector:
    /// Assets/Aurumworks/Resources/AleatoraTuning.asset, one section per system (as Deepglow's
    /// DeepglowTuning). The systems are made in code (Aleatora) and read their values here.
    /// Aleatora > Make Tuning makes the asset if it is missing; without it the defaults below apply.
    /// </summary>
    public class AleatoraTuning : ScriptableObject
    {
        /// <summary>The asset's name in Resources.</summary>
        public const string ResourceName = "AleatoraTuning";

        [Tooltip("The board's three dimensions, which way its hexagons face, and how a cell looks. Tunable in play.")]
        [SerializeField] private GridTuning _grid = new();

        [Tooltip("Which set the melodies come from, how each note is chosen, the range they are played in, and the seed.")]
        [SerializeField] private MelodyTuning _melodies = new();

        /// <summary>The tuning: the asset, or the defaults if there is none.</summary>
        public static AleatoraTuning Load()
        {
            var tuning = Resources.Load<AleatoraTuning>(ResourceName);
            if (tuning == null)
            {
                Debug.LogWarning($"No {ResourceName} in a Resources folder: the defaults apply. Aleatora > Make Tuning makes it.");
                tuning = CreateInstance<AleatoraTuning>();
            }

            return tuning;
        }

        /// <summary>The grid's shape and look.</summary>
        public GridTuning Grid => _grid;

        /// <summary>The melodies' set, generator, voicing and seed.</summary>
        public MelodyTuning Melodies => _melodies;
    }
}
