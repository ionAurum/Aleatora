using System;
using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The emitter's tuning, a section of AleatoraTuning (Docs/Aleatora_Emitter_Plan.md). The
    /// board's notes (EM-1): the set and tonic they come from, how the degrees are laid out
    /// (CellNotes), the octaves, and how a cell shows its note (its colour, ruling 71, and its
    /// label). The emitter (EM-2): its spray, its spheres, what counts as a hit, how it moves, and
    /// how the spheres, the aim point and a hit look. Its notes (EM-3): the channel, the quantizing,
    /// velocity from impact speed, length from sphere size, the voices. BoardNotes and Emitter read it every frame, so
    /// all of it can be tuned in play. The defaults are the plan's.
    /// </summary>
    [Serializable]
    public class EmitterTuning
    {
        /// <summary>The widest spray, edge to edge, in degrees: the whole lower half.</summary>
        public const float MostSpread = 180f;

        /// <summary>The highest Rate, spheres a second.</summary>
        public const float MostRate = 50f;

        /// <summary>The lowest Rate the wheel steps to, spheres a second (the panel still sets 0).</summary>
        public const float LeastWheelRate = 0.5f;

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

        [Header("Emitter: spray")]
        [Tooltip("Spheres launched a second while spraying. The gamepad's right trigger scales it by how far it is pressed.")]
        [SerializeField, Range(0f, 50f)] private float _rate = 4f;
        [Tooltip("The spray's whole angle, edge to edge, in degrees: 0 is a jet, 180 the whole lower half. Right button and the wheel step it.")]
        [SerializeField, Range(0f, 180f)] private float _spread = 20f;
        [Tooltip("The spheres' launch speed, in units a second.")]
        [SerializeField, Range(2f, 30f)] private float _speed = 8f;
        [Tooltip("How far each sphere's speed may stray either side of Speed, in percent.")]
        [SerializeField, Range(0f, 50f)] private float _speedJitter = 10f;

        [Header("Emitter: spheres")]
        [Tooltip("The smallest sphere's diameter, in units. Each sphere's size is drawn between this and Max Size.")]
        [SerializeField, Range(0.1f, 1f)] private float _minSize = 0.2f;
        [Tooltip("The largest sphere's diameter, in units.")]
        [SerializeField, Range(0.1f, 1f)] private float _maxSize = 0.5f;
        [Tooltip("How much a sphere bounces: 0 not at all, 1 back as fast as it came.")]
        [SerializeField, Range(0f, 1f)] private float _bounciness = 0.6f;
        [Tooltip("How long a sphere lives, in seconds, unless it falls below the board first.")]
        [SerializeField, Range(1f, 30f)] private float _lifetime = 8f;
        [Tooltip("The most spheres there are at once. While there are this many, spraying launches none.")]
        [SerializeField, Range(1, 2000)] private int _maxSpheres = 300;

        [Header("Emitter: hits")]
        [Tooltip("The slowest impact that counts as a hit: the speed along the contact's normal, in units a second. Softer touches (a sphere rolling) do nothing.")]
        [SerializeField, Range(0f, 30f)] private float _minSpeed = 1f;
        [Tooltip("Writes every hit to the Console: the cell, its note, the impact speed, the sphere's size.")]
        [SerializeField] private bool _logHits;

        [Header("Emitter: notes")]
        [Tooltip("The MIDI channel the hits' notes go out on, 1–16.")]
        [SerializeField, Range(1, 16)] private int _channel = 1;
        [Tooltip("The note value a hit's note is quantized to, on the clock's beat line (Reaper's while following it). Off: it goes on as the hit lands.")]
        [SerializeField] private QuantizeGrid _grid = QuantizeGrid.Sixteenth;
        [Tooltip("How far toward the grid: 0 not at all, 1 on the next grid point. A note only ever moves later, never earlier.")]
        [SerializeField, Range(0f, 1f)] private float _strength = 1f;
        [Tooltip("The note's length quantized too: toward the nearest whole number of grid steps, at least one.")]
        [SerializeField] private bool _quantizeLength = true;
        [Tooltip("The impact speed that plays at Max Velocity, in units a second. Min Speed plays at Min Velocity.")]
        [SerializeField, Range(0.1f, 60f)] private float _maxSpeed = 15f;
        [Tooltip("The velocity of the softest hit (at Min Speed).")]
        [SerializeField, Range(1, 127)] private int _minVelocity = 30;
        [Tooltip("The velocity of a hit at Max Speed or harder.")]
        [SerializeField, Range(1, 127)] private int _maxVelocity = 127;
        [Tooltip("Shapes speed to velocity: 1 straight; above 1 soft hits stay softer; below 1 they come up louder.")]
        [SerializeField, Range(0.1f, 5f)] private float _velocityCurve = 1f;
        [Tooltip("The length of a note from the smallest sphere (Min Size), in beats.")]
        [SerializeField, Range(0.05f, 8f)] private float _shortest = 0.25f;
        [Tooltip("The length of a note from the largest sphere (Max Size), in beats.")]
        [SerializeField, Range(0.05f, 8f)] private float _longest = 2f;
        [Tooltip("The most of the emitter's notes sounding at once; a note past it is dropped.")]
        [SerializeField, Range(1, HitScheduler.MostVoices)] private int _maxVoices = HitScheduler.DefaultMaxVoices;

        [Header("Emitter: moving")]
        [Tooltip("How fast W A S D and the left stick move the emitter over the board, in units a second.")]
        [SerializeField, Range(0.5f, 30f)] private float _moveSpeed = 6f;
        [Tooltip("How fast Q and E lower and raise it, in units a second.")]
        [SerializeField, Range(0.5f, 30f)] private float _raiseSpeed = 4f;
        [Tooltip("The emitter's lowest height above the cells' tops, in units.")]
        [SerializeField, Range(0.5f, 50f)] private float _minHeight = 2f;
        [Tooltip("The emitter's highest height above the cells' tops, in units.")]
        [SerializeField, Range(1f, 100f)] private float _maxHeight = 30f;

        [Header("Emitter: controls")]
        [Tooltip("How far the cone tilts for each count of the mouse's movement while the emitter is held, in degrees.")]
        [SerializeField, Range(0.01f, 2f)] private float _tiltSensitivity = 0.15f;
        [Tooltip("How long the cone takes to follow the tilt set, in seconds (most of the way): 0 at once; more, the cone lags and swings after a flick.")]
        [SerializeField, Range(0f, 1f)] private float _swing;
        [Tooltip("How fast the right stick tilts the cone at full push, in degrees a second.")]
        [SerializeField, Range(10f, 720f)] private float _stickTiltSpeed = 120f;
        [Tooltip("Each notch of the wheel (or press of the D-pad) multiplies or divides Rate by this.")]
        [SerializeField, Range(1.01f, 2f)] private float _wheelStep = 1.25f;
        [Tooltip("Each notch of the wheel with the right button (or the D-pad with the left trigger) widens or narrows Spread by this, in degrees.")]
        [SerializeField, Range(0.5f, 30f)] private float _spreadStep = 5f;

        [Header("Emitter: look")]
        [Tooltip("The spheres' material. Empty: Unity's default for a sphere.")]
        [SerializeField] private Material _sphereMaterial;
        [Tooltip("The aim point's marker's material, tinted Aim Color. Empty: no marker.")]
        [SerializeField] private Material _aimMaterial;
        [Tooltip("The aim point's marker's colour.")]
        [SerializeField] private Color _aimColor = new(1f, 0.85f, 0f, 1f);
        [Tooltip("The aim point's marker's diameter, in units.")]
        [SerializeField, Range(0.05f, 3f)] private float _aimSize = 0.4f;
        [Tooltip("The width of the outline drawn where the spray's edge lands, in units. 0: no outline.")]
        [SerializeField, Range(0f, 0.5f)] private float _aimLineWidth = 0.05f;
        [Tooltip("The cone's colour while the emitter is held (taken with G or a click on it).")]
        [SerializeField] private Color _heldColor = new(1f, 0.85f, 0f, 1f);
        [Tooltip("The colour a struck cell glows.")]
        [SerializeField] private Color _hitColor = Color.white;
        [Tooltip("How brightly a struck cell glows at first (as the grid's Selected Glow).")]
        [SerializeField, Range(0f, 1f)] private float _hitGlow = 1f;
        [Tooltip("How long a struck cell's glow takes to fade, in seconds.")]
        [SerializeField, Range(0f, 2f)] private float _hitFade = 0.25f;

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

        /// <summary>Spheres a second while spraying, 0 to 50.</summary>
        public float Rate
        {
            get => Mathf.Clamp(_rate, 0f, MostRate);
            set => _rate = value;
        }

        /// <summary>The spray's whole angle, edge to edge, 0 to 180 degrees (MostSpread).</summary>
        public float Spread
        {
            get => Mathf.Clamp(_spread, 0f, MostSpread);
            set => _spread = value;
        }

        /// <summary>The launch speed, 2 to 30 units a second.</summary>
        public float Speed
        {
            get => Mathf.Clamp(_speed, 2f, 30f);
            set => _speed = value;
        }

        /// <summary>The speed's jitter either side, as a fraction (0 to 0.5).</summary>
        public float SpeedJitter
        {
            get => Mathf.Clamp(_speedJitter, 0f, 50f) / 100f;
            set => _speedJitter = value * 100f;
        }

        /// <summary>The smallest sphere's diameter (never above the largest's).</summary>
        public float MinSize
        {
            get => Mathf.Min(Mathf.Clamp(_minSize, 0.1f, 1f), Mathf.Clamp(_maxSize, 0.1f, 1f));
            set => _minSize = value;
        }

        /// <summary>The largest sphere's diameter (never below the smallest's).</summary>
        public float MaxSize
        {
            get => Mathf.Max(Mathf.Clamp(_minSize, 0.1f, 1f), Mathf.Clamp(_maxSize, 0.1f, 1f));
            set => _maxSize = value;
        }

        /// <summary>The spheres' bounciness, 0 to 1.</summary>
        public float Bounciness
        {
            get => Mathf.Clamp01(_bounciness);
            set => _bounciness = value;
        }

        /// <summary>A sphere's lifetime, 1 to 30 seconds.</summary>
        public float Lifetime
        {
            get => Mathf.Clamp(_lifetime, 1f, 30f);
            set => _lifetime = value;
        }

        /// <summary>The most spheres at once, 1 to 2000.</summary>
        public int MaxSpheres
        {
            get => Mathf.Clamp(_maxSpheres, 1, 2000);
            set => _maxSpheres = value;
        }

        /// <summary>The slowest impact that is a hit, in units a second.</summary>
        public float MinSpeed
        {
            get => Mathf.Max(0f, _minSpeed);
            set => _minSpeed = value;
        }

        /// <summary>Whether every hit is written to the Console.</summary>
        public bool LogHits
        {
            get => _logHits;
            set => _logHits = value;
        }

        /// <summary>The hits' notes' MIDI channel, 1–16.</summary>
        public int Channel
        {
            get => Mathf.Clamp(_channel, 1, 16);
            set => _channel = value;
        }

        /// <summary>The note value the hits are quantized to.</summary>
        public QuantizeGrid Grid
        {
            get => _grid;
            set => _grid = value;
        }

        /// <summary>How far toward the grid, 0 to 1.</summary>
        public float Strength
        {
            get => Mathf.Clamp01(_strength);
            set => _strength = value;
        }

        /// <summary>Whether a note's length is quantized as well as its start.</summary>
        public bool QuantizeLength
        {
            get => _quantizeLength;
            set => _quantizeLength = value;
        }

        /// <summary>The impact speed that plays at Max Velocity (never below Min Speed).</summary>
        public float MaxSpeed
        {
            get => Mathf.Max(MinSpeed, _maxSpeed);
            set => _maxSpeed = value;
        }

        /// <summary>The softest hit's velocity, 1–127.</summary>
        public int MinVelocity
        {
            get => Mathf.Clamp(_minVelocity, 1, 127);
            set => _minVelocity = value;
        }

        /// <summary>The hardest hit's velocity, 1–127.</summary>
        public int MaxVelocity
        {
            get => Mathf.Clamp(_maxVelocity, 1, 127);
            set => _maxVelocity = value;
        }

        /// <summary>The exponent shaping speed to velocity, 0.1 to 5.</summary>
        public float VelocityCurve
        {
            get => Mathf.Clamp(_velocityCurve, 0.1f, 5f);
            set => _velocityCurve = value;
        }

        /// <summary>The smallest sphere's note's length, in beats.</summary>
        public float Shortest
        {
            get => Mathf.Clamp(_shortest, 0.05f, 8f);
            set => _shortest = value;
        }

        /// <summary>The largest sphere's note's length, in beats.</summary>
        public float Longest
        {
            get => Mathf.Clamp(_longest, 0.05f, 8f);
            set => _longest = value;
        }

        /// <summary>The most of the emitter's notes sounding at once.</summary>
        public int MaxVoices
        {
            get => Mathf.Clamp(_maxVoices, 1, HitScheduler.MostVoices);
            set => _maxVoices = value;
        }

        /// <summary>How fast the emitter moves over the board, units a second.</summary>
        public float MoveSpeed => Mathf.Max(0f, _moveSpeed);

        /// <summary>How fast it is lowered and raised, units a second.</summary>
        public float RaiseSpeed => Mathf.Max(0f, _raiseSpeed);


        /// <summary>The emitter's lowest height above the cells' tops.</summary>
        public float MinHeight => Mathf.Max(0.5f, Mathf.Min(_minHeight, _maxHeight));

        /// <summary>The emitter's highest height above the cells' tops.</summary>
        public float MaxHeight => Mathf.Max(MinHeight, _maxHeight);

        /// <summary>Degrees of tilt a count of the mouse's movement.</summary>
        public float TiltSensitivity => Mathf.Max(0f, _tiltSensitivity);

        /// <summary>The cone's lag behind the tilt set, in seconds; 0 none.</summary>
        public float Swing
        {
            get => Mathf.Max(0f, _swing);
            set => _swing = value;
        }

        /// <summary>The right stick's tilt at full push, degrees a second.</summary>
        public float StickTiltSpeed => Mathf.Max(0f, _stickTiltSpeed);

        /// <summary>The factor a notch of the wheel multiplies or divides Rate by.</summary>
        public float WheelStep => Mathf.Max(1.001f, _wheelStep);

        /// <summary>The degrees a notch of the wheel widens or narrows Spread by.</summary>
        public float SpreadStep => Mathf.Max(0f, _spreadStep);

        /// <summary>The spheres' material, or null for Unity's default.</summary>
        public Material SphereMaterial => _sphereMaterial;

        /// <summary>The aim marker's material, or null for no marker.</summary>
        public Material AimMaterial => _aimMaterial;

        /// <summary>The aim marker's colour.</summary>
        public Color AimColor => _aimColor;

        /// <summary>The aim marker's diameter.</summary>
        public float AimSize => Mathf.Clamp(_aimSize, 0.05f, 3f);

        /// <summary>The landing outline's width; 0 draws none.</summary>
        public float AimLineWidth => Mathf.Clamp(_aimLineWidth, 0f, 0.5f);

        /// <summary>The cone's colour while the emitter is held.</summary>
        public Color HeldColor => _heldColor;

        /// <summary>The colour a struck cell glows.</summary>
        public Color HitColor => _hitColor;

        /// <summary>How brightly a struck cell glows at first, 0 to 1.</summary>
        public float HitGlow => Mathf.Clamp01(_hitGlow);

        /// <summary>How long a struck cell's glow takes to fade, in seconds.</summary>
        public float HitFade => Mathf.Clamp(_hitFade, 0f, 2f);

        /// <summary>A cell's colour at rest for its degree: the root's, or between unlikely and likely by blue (0 to 1).</summary>
        public Color CellColor(int degree, float blue) =>
            degree == 0 ? _rootColor : Color.Lerp(_unlikelyColor, _likelyColor, Mathf.Clamp01(blue));
    }
}
