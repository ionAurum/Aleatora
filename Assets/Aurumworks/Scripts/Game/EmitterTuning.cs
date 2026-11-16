using System;
using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The emitter's tuning, a section of AleatoraTuning (Docs/Aleatora_Emitter_Plan.md). As of EM-1,
    /// the board's notes: the set and tonic they come from, how the degrees are laid out (CellNotes),
    /// the octaves, and how a cell shows its note (its colour, ruling 71, and its label). BoardNotes
    /// reads it every frame, so all of it can be tuned in play. The defaults are the plan's.
    /// </summary>
    [Serializable]
    public class EmitterTuning
    {
        [Header("Board: notes")]
        [Tooltip("The melody table the board's notes come from, by its name in Resources/Markov/Index.txt: european_ionian, irish_dorian, …. Only its unigram is used: how often each degree is played.")]
        [SerializeField] private string _set = "european_ionian";
        [Tooltip("The tonic's pitch class: 0 C, 1 C♯, … 11 B.")]
        [SerializeField, Range(0, 11)] private int _tonic;
        [Tooltip("Scattered: the cells shared out among the degrees by how often each is played, then shuffled. Clustered: the same counts, each degree one region. Axes: 1 on the centre cell, each step along an axis moving the degree by that axis's interval.")]
        [SerializeField] private CellLayout _layout = CellLayout.Scattered;
        [Tooltip("Where the layout's random draws start: one seed is one board. 2112, for Rush.")]
        [SerializeField] private long _seed = 2112;
        [Tooltip("Multiplies the weight of each degree outside the set's mode. 0 keeps the board in the mode; 1 as the data has it.")]
        [SerializeField, Min(0f)] private float _chromaticWeight;
        [Tooltip("Axes: how many scale steps a step along +K moves the degree. 1 a second, 2 a third, 3 a fourth, 4 a fifth.")]
        [SerializeField, Range(-7, 7)] private int _kInterval = 2;
        [Tooltip("Axes: how many scale steps a step along +N moves the degree. A step along +M moves it by both.")]
        [SerializeField, Range(-7, 7)] private int _nInterval = 2;

        [Header("Board: octaves")]
        [Tooltip("The lowest octave's number, as MIDI names notes: 3 starts at C3 (48), 4 at middle C (60).")]
        [SerializeField, Range(-1, 8)] private int _lowestOctave = 3;
        [Tooltip("How many octaves the board's rings are shared among, from the centre cell outward.")]
        [SerializeField, Range(1, CellNotes.MostOctaves)] private int _octaves = 3;
        [Tooltip("Off: the centre cell lowest, rising outward. On: the centre highest, falling outward.")]
        [SerializeField] private bool _outwardLower;

        [Header("Board: look")]
        [Tooltip("The colour of the root's cells.")]
        [SerializeField] private Color _rootColor = Color.red;
        [Tooltip("The colour of a degree never played. The others lie between this and Likely Colour, by how often each is played next to the likeliest degree but the root.")]
        [SerializeField] private Color _unlikelyColor = Color.white;
        [Tooltip("The colour of the likeliest degree but the root.")]
        [SerializeField] private Color _likelyColor = Color.blue;
        [Tooltip("Each cell's degree written on its top: ♭3.")]
        [SerializeField] private bool _showDegrees = true;
        [Tooltip("On: the labels in the UI theme's text colour. Off: in Label Color.")]
        [SerializeField] private bool _labelColorFromTheme;
        [Tooltip("The labels' colour when Label Color From Theme is off.")]
        [SerializeField] private Color _labelColor = new(0.1f, 0.1f, 0.1f, 1f);
        [Tooltip("The labels' size, as TextMeshPro's font size.")]
        [SerializeField, Range(0.5f, 20f)] private float _labelSize = 5f;
        [Tooltip("How far above the cells' tops the labels sit, in metres.")]
        [SerializeField, Range(0f, 1f)] private float _labelHeight = 0.01f;

        /// <summary>The board's set's name, as the index lists it.</summary>
        public string Set
        {
            get => _set;
            set => _set = value;
        }

        /// <summary>The tonic's pitch class, 0 (C) to 11 (B).</summary>
        public int Tonic
        {
            get => Mathf.Clamp(_tonic, 0, 11);
            set => _tonic = value;
        }

        /// <summary>How the degrees are laid out.</summary>
        public CellLayout Layout
        {
            get => _layout;
            set => _layout = value;
        }

        /// <summary>The layout's seed.</summary>
        public ulong Seed
        {
            get => unchecked((ulong)_seed);
            set => _seed = unchecked((long)value);
        }

        /// <summary>The weight of a degree outside the mode, at least 0.</summary>
        public float ChromaticWeight
        {
            get => Mathf.Max(0f, _chromaticWeight);
            set => _chromaticWeight = value;
        }

        /// <summary>Axes: scale steps a step along +K.</summary>
        public int KInterval
        {
            get => Mathf.Clamp(_kInterval, -7, 7);
            set => _kInterval = value;
        }

        /// <summary>Axes: scale steps a step along +N.</summary>
        public int NInterval
        {
            get => Mathf.Clamp(_nInterval, -7, 7);
            set => _nInterval = value;
        }

        /// <summary>The lowest octave's number, MIDI's (C4 is 60).</summary>
        public int LowestOctave
        {
            get => Mathf.Clamp(_lowestOctave, -1, 8);
            set => _lowestOctave = value;
        }

        /// <summary>How many octaves the rings are shared among, 1 to CellNotes.MostOctaves.</summary>
        public int Octaves
        {
            get => Mathf.Clamp(_octaves, 1, CellNotes.MostOctaves);
            set => _octaves = value;
        }

        /// <summary>Whether the octaves fall outward.</summary>
        public bool OutwardLower
        {
            get => _outwardLower;
            set => _outwardLower = value;
        }

        /// <summary>The root's cells' colour.</summary>
        public Color RootColor => _rootColor;

        /// <summary>A degree never played's colour.</summary>
        public Color UnlikelyColor => _unlikelyColor;

        /// <summary>The likeliest degree but the root's colour.</summary>
        public Color LikelyColor => _likelyColor;

        /// <summary>Whether each cell's degree is written on it.</summary>
        public bool ShowDegrees
        {
            get => _showDegrees;
            set => _showDegrees = value;
        }

        /// <summary>Whether the labels take the UI theme's text colour.</summary>
        public bool LabelColorFromTheme => _labelColorFromTheme;

        /// <summary>The labels' own colour.</summary>
        public Color LabelColor => _labelColor;

        /// <summary>The labels' font size.</summary>
        public float LabelSize => Mathf.Clamp(_labelSize, 0.5f, 20f);

        /// <summary>The labels' height above the cells' tops.</summary>
        public float LabelHeight => Mathf.Clamp(_labelHeight, 0f, 1f);

        /// <summary>A cell's colour at rest for its degree: the root's, or between unlikely and likely by blue (0 to 1).</summary>
        public Color CellColor(int degree, float blue) =>
            degree == 0 ? _rootColor : Color.Lerp(_unlikelyColor, _likelyColor, Mathf.Clamp01(blue));
    }
}
