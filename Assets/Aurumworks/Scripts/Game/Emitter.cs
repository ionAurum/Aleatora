using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The emitter (the emitter plan, EM-2): the scene's cone, which sprays Rigidbody spheres onto
    /// the board. While spraying it launches spheres from the cone's lower end, along its axis,
    /// spread inside a cone, at a rate a second; each sphere's size, speed and direction are drawn
    /// from its own Rng (SprayDraws; ruling 63). Spheres come from a pool, live their lifetime or
    /// until they fall below the board, and are never more than Max Spheres. A sphere striking a
    /// cell hard enough is a hit (EmitterHit): the cell glows, fading, and Hit is raised; EM-3 turns
    /// hits into notes.
    /// The controls (EM-6; `Docs/Aleatora_Emitter_Controls.md`, schemes A and B, rulings 77 and 78):
    /// the player takes the emitter (Grab Emitter, or a click on the cone) and lets it go (Grab
    /// Emitter again; a menu opening, or the window losing the focus). While it is held the pointer
    /// is locked and hidden, the mouse's movement tilts the cone (ConeTilt, on the camera's
    /// flattened axes), the left button sprays while down and the wheel steps Rate, or Spread with
    /// Wheel Modifier (the right button). On the gamepad (View takes and lets go, as G), while it
    /// is held, the right stick tilts, the right trigger sprays from the wheel's least rate up to
    /// Rate by how far it is pulled (TriggerRate), the left trigger widens Spread, narrowing it with
    /// A held, and the D-pad steps Rate as the wheel (ruling 80). Held or not: Spray latches
    /// spraying on and off, Move Emitter moves it relative to the camera, along the grid's lines
    /// while Grid Lock is held, Raise Emitter lowers and raises it, Reset Tilt points it straight
    /// down and Reset Position puts it back where it began. Octave Down and Octave Up (Z and X,
    /// the D-pad's left and right) step the board's lowest octave (ruling 83).
    /// The cone follows the tilt set after Swing. A marker shows where the axis's sphere lands and
    /// an outline where the spray's edge does (SprayLanding). While a menu is open it reads no input
    /// and launches nothing; spheres in the air land. While a text field has the keyboard it reads
    /// no input either. Its tuning (EmitterTuning) is read every frame; the wheel writes Rate and
    /// Spread on it, as the panel does. The bootstrap (Aleatora) adds it to the scene's Emitter.
    /// </summary>
    public class Emitter : MonoBehaviour
    {
        private const string SprayActionPath = "Player/Spray";
        private const string PressureActionPath = "Player/Spray Pressure";
        private const string MoveActionPath = "Player/Move Emitter";
        private const string RaiseActionPath = "Player/Raise Emitter";
        private const string GridLockActionPath = "Player/Grid Lock";
        private const string StickTiltActionPath = "Player/Stick Tilt";
        private const string GrabActionPath = "Player/Grab Emitter";
        private const string TiltActionPath = "Player/Tilt";
        private const string WheelActionPath = "Player/Wheel";
        private const string WheelStepActionPath = "Player/Wheel Step";
        private const string WheelModifierActionPath = "Player/Wheel Modifier";
        private const string ClickActionPath = "Player/Left Click";
        private const string SpreadPressureActionPath = "Player/Spread Pressure";
        private const string NarrowActionPath = "Player/Narrow Spread";
        private const string ResetTiltActionPath = "Player/Reset Tilt";
        private const string ResetPositionActionPath = "Player/Reset Position";
        private const string OctaveDownActionPath = "Player/Octave Down";
        private const string OctaveUpActionPath = "Player/Octave Up";

        private const ulong SprayStream = 0x5350524159UL; // "SPRAY": the spray's draws apart from the layout's, from one seed
        private const float BelowBoard = 2f;               // a sphere this far below the cells' bottoms is gone
        private const float Clearance = 0.01f;             // between the cone's end and a new sphere
        private const float MarkerThickness = 0.01f;
        private const int OutlinePoints = 32;
        private const float Arrived = 0.001f;              // within this of a cell's middle is on it

        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<EmitterSphere> _active = new();
        private readonly Stack<EmitterSphere> _free = new();
        private readonly List<HexCellView> _glowing = new();
        private readonly List<double> _glowStart = new();
        private readonly Vector3[] _outlinePoints = new Vector3[OutlinePoints];

        private HexGrid _grid;
        private GridTuning _gridTuning;
        private BoardNotes _board;
        private EmitterTuning _tuning;
        private Camera _camera;
        private InputAction _sprayAction, _pressureAction, _moveAction, _raiseAction, _gridLockAction, _stickTiltAction;
        private InputAction _grabAction, _tiltAction, _wheelAction, _wheelStepAction, _wheelModifierAction, _clickAction;
        private InputAction _spreadPressureAction, _narrowAction, _resetTiltAction, _resetPositionAction;
        private InputAction _octaveDownAction, _octaveUpAction;
        private WheelBinding[] _wheelBindings;
        private Transform _sphereRoot;
        private PhysicsMaterial _physicsMaterial;
        private Material _defaultSphereMaterial, _sphereMaterial;
        private Rng _rng;
        private ulong _seed;
        private Vector3 _nozzle;   // the cone's lower end, in the emitter's own space
        private Vector3 _aimPoint; // where the axis's sphere lands, on the plane of the cells' tops
        private bool _landed;      // whether it lands at all (it always does under gravity)
        private double _setX, _setZ;   // the tilt the controls set, degrees
        private double _tiltX, _tiltZ; // the cone's tilt, following it after Swing
        private float _height;     // the emitter's height above the cells' tops
        private Vector3 _startPosition; // where it began, for Reset Position
        private float _startHeight;
        private float _rate;       // spheres a second, as the controls and the menus have it this frame
        private float _owed = 1f;  // spheres owed; starts at one, so spraying launches at once
        private float _pressure;
        private bool _sprayHeld;   // the left button, down while the emitter is held
        private bool _clickTook;   // the left button that took the emitter, not yet let go
        private int _lastStep;     // the D-pad's last direction, so a press steps once
        private bool _gliding;     // moving along the grid's lines (Grid Lock)
        private int _glideDirection = -1;
        private HexCoord _glideTarget;
        private Renderer[] _coneRenderers;
        private MaterialPropertyBlock _coneBlock;
        private bool _shownHeld;
        private Transform _marker;
        private MeshRenderer _markerRenderer;
        private MaterialPropertyBlock _markerBlock;
        private LineRenderer _outline;

        private enum WheelTarget
        {
            Rate,
            Spread,
        }

        /// <summary>Whether spraying is latched on (Spray toggles it), at Rate. The left button and the right trigger, held, spray while it is off.</summary>
        public bool Spraying { get; set; }

        /// <summary>Whether the player holds the emitter: the mouse and the right stick tilt it, the left button and the right trigger spray, the wheel, the D-pad and the left trigger step.</summary>
        public bool Held { get; private set; }

        /// <summary>Spheres a second being launched this frame: 0 while a menu is open or nothing sprays.</summary>
        public float CurrentRate => _rate;

        /// <summary>The spheres in flight or at rest on the board.</summary>
        public IReadOnlyList<EmitterSphere> Spheres => _active;

        /// <summary>How many spheres have been launched since the emitter began.</summary>
        public int Launched { get; private set; }

        /// <summary>How many hits there have been since the emitter began.</summary>
        public int Hits { get; private set; }

        /// <summary>Where a sphere launched along the cone's axis at Speed lands, on the plane of the cells' tops.</summary>
        public Vector3 AimPoint => _aimPoint;

        /// <summary>Where spheres along the spread's edge land: the outline, 32 points round the axis, just above the board.</summary>
        public IReadOnlyList<Vector3> Outline => _outlinePoints;

        /// <summary>The cone's tilt toward +X, degrees, −90 to 90 (ConeTilt).</summary>
        public double TiltX => _tiltX;

        /// <summary>The cone's tilt toward +Z, degrees, −90 to 90.</summary>
        public double TiltZ => _tiltZ;

        /// <summary>The direction of the cone's axis, in world space.</summary>
        public Vector3 Axis => transform.rotation * Vector3.down;

        /// <summary>Whether it is moving along the grid's lines, Grid Lock held.</summary>
        public bool Gliding => _gliding;

        /// <summary>Where new spheres start: the cone's lower end, in world space.</summary>
        public Vector3 Nozzle => transform.TransformPoint(_nozzle);

        /// <summary>Raised on each hit, in the physics step that reported it, after the cell has begun to glow.</summary>
        public event Action<EmitterHit> Hit;

        /// <summary>Gives the emitter the board, its notes and the tunings; its spheres and the aim marker go under parent.</summary>
        public void Begin(HexGrid grid, GridTuning gridTuning, BoardNotes board, EmitterTuning tuning, Transform parent)
        {
            _grid = grid;
            _gridTuning = gridTuning;
            _board = board;
            _tuning = tuning;
            _camera = Camera.main;

            _sphereRoot = new GameObject("Spheres").transform;
            _sphereRoot.SetParent(parent, false);
            _physicsMaterial = new PhysicsMaterial("Emitter Sphere")
            {
                bounciness = _tuning.Bounciness,
                bounceCombine = PhysicsMaterialCombine.Maximum, // the spheres' bounciness, whatever the cells'
            };

            _sprayAction = FindAction(SprayActionPath);
            _pressureAction = FindAction(PressureActionPath);
            _moveAction = FindAction(MoveActionPath);
            _raiseAction = FindAction(RaiseActionPath);
            _gridLockAction = FindAction(GridLockActionPath);
            _stickTiltAction = FindAction(StickTiltActionPath);
            _grabAction = FindAction(GrabActionPath);
            _tiltAction = FindAction(TiltActionPath);
            _wheelAction = FindAction(WheelActionPath);
            _wheelStepAction = FindAction(WheelStepActionPath);
            _wheelModifierAction = FindAction(WheelModifierActionPath);
            _clickAction = FindAction(ClickActionPath);
            _spreadPressureAction = FindAction(SpreadPressureActionPath);
            _narrowAction = FindAction(NarrowActionPath);
            _resetTiltAction = FindAction(ResetTiltActionPath);
            _resetPositionAction = FindAction(ResetPositionActionPath);
            _octaveDownAction = FindAction(OctaveDownActionPath);
            _octaveUpAction = FindAction(OctaveUpActionPath);

            // What the wheel steps (A6): the first modifier held picks; none, Rate. A later
            // modifier is one more line here.
            _wheelBindings = new[]
            {
                new WheelBinding(_wheelModifierAction, WheelTarget.Spread),
            };

            _coneRenderers = GetComponentsInChildren<Renderer>();
            _coneBlock = new MaterialPropertyBlock();
            _nozzle = FindNozzle();
            _sphereMaterial = _tuning.SphereMaterial;
            _seed = _tuning.Seed;
            _rng = new Rng(_seed ^ SprayStream);
            _height = transform.position.y - TopY();
            _startPosition = transform.position;
            _startHeight = _height;
            _marker = MakeMarker(parent);
            _outline = MakeOutline(parent);
            Orient();
            Land();
        }

        /// <summary>Points the cone's axis at a point of the board (its height is taken as the cells' tops'), at once.</summary>
        public void AimAt(Vector3 point)
        {
            Orient();
            point.y = TopY();
            Vector3 to = point - transform.position;
            ConeTilt.Toward(to.x, to.y, to.z, out _setX, out _setZ);
            _tiltX = _setX;
            _tiltZ = _setZ;
            Orient();
            Land();
        }

        /// <summary>Sets the cone's tilt, degrees toward +X and toward +Z, each held within ±90, at once.</summary>
        public void SetTilt(double tiltX, double tiltZ)
        {
            _setX = ConeTilt.Clamp(tiltX);
            _setZ = ConeTilt.Clamp(tiltZ);
            _tiltX = _setX;
            _tiltZ = _setZ;
            Orient();
            Land();
        }

        /// <summary>Points the cone straight down (Reset Tilt); with Swing, it swings there.</summary>
        public void ResetTilt()
        {
            _setX = 0.0;
            _setZ = 0.0;
        }

        /// <summary>Puts the emitter back where it began, its height too (Reset Position), at once; its tilt is kept.</summary>
        public void ResetPosition()
        {
            _gliding = false;
            _glideDirection = -1;
            _height = _startHeight;
            transform.position = _startPosition;
            Orient();
            Land();
        }

        /// <summary>Takes the emitter: the mouse tilts it, the left button sprays, the wheel steps; the pointer is locked and hidden.</summary>
        public void Take()
        {
            if (Held)
            {
                return;
            }

            Held = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Lets the emitter go: the mouse is the pointer again.</summary>
        public void Release()
        {
            if (!Held)
            {
                return;
            }

            Held = false;
            _sprayHeld = false;
            _clickTook = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Whether a point of the screen is on the cone (its renderers' bounds; the cone has no collider).</summary>
        public bool IsOnCone(Vector2 screenPoint)
        {
            if (_camera == null || _coneRenderers == null)
            {
                return false;
            }

            Ray ray = _camera.ScreenPointToRay(screenPoint);
            foreach (Renderer coneRenderer in _coneRenderers)
            {
                if (coneRenderer != null && coneRenderer.enabled && coneRenderer.bounds.IntersectRay(ray))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Puts every sphere away and ends every glow.</summary>
        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Retire(i);
            }

            for (int i = _glowing.Count - 1; i >= 0; i--)
            {
                if (_glowing[i] != null)
                {
                    _glowing[i].SetHit(0f, 0f, Color.clear, _gridTuning);
                }
            }

            _glowing.Clear();
            _glowStart.Clear();
        }

        /// <summary>A sphere's collision, from EmitterSphere: a hit if it struck a cell at Min Speed or faster.</summary>
        public void OnSphereHit(EmitterSphere sphere, Collision collision)
        {
            if (_tuning == null || !sphere.gameObject.activeSelf)
            {
                return;
            }

            HexCellView cell = collision.collider.GetComponentInParent<HexCellView>();
            if (cell == null)
            {
                return; // another sphere
            }

            float speed = collision.contactCount > 0
                ? Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(0).normal))
                : collision.relativeVelocity.magnitude;
            if (speed < _tuning.MinSpeed)
            {
                return;
            }

            Glow(cell);
            Hits++;
            var hit = new EmitterHit(cell.Coord, _board != null ? _board.NoteAt(cell.Coord) : -1, speed, sphere.Size, Time.timeAsDouble);
            if (_tuning.LogHits)
            {
                Debug.Log("Emitter: hit " + hit);
            }

            Hit?.Invoke(hit);
        }

        //----------

        private void Update()
        {
            if (_tuning == null)
            {
                return;
            }

            bool menuOpen = MenuFocus.IsAnyOpen;
            if (Held && menuOpen)
            {
                Release(); // Escape opens the menu and lets the emitter go (A1)
            }

            if (!menuOpen && !UiInput.IsTyping)
            {
                ReadControls(Time.deltaTime);
            }
            else
            {
                _sprayHeld = false;
            }

            float on = Spraying || _sprayHeld ? 1f : 0f;
            float squeezed = (float)TriggerRate.Rate(_pressure, EmitterTuning.LeastWheelRate, _tuning.Rate);
            _rate = menuOpen ? 0f : Mathf.Max(_tuning.Rate * on, squeezed);
            Swing(Time.deltaTime);
            Orient();
            SyncSphereMaterial();
            FadeGlows();
            Land();
            PlaceMarker();
            ShowHeld();
        }

        private void FixedUpdate()
        {
            if (_tuning == null)
            {
                return;
            }

            _physicsMaterial.bounciness = _tuning.Bounciness;
            RetireSpent();

            if (_rate <= 0f)
            {
                _owed = 1f;
                return;
            }

            _owed += _rate * Time.fixedDeltaTime;
            while (_owed >= 1f)
            {
                if (!Launch())
                {
                    _owed = 1f; // at Max Spheres: launch as soon as one goes
                    break;
                }

                _owed -= 1f;
            }
        }

        // The window losing the focus lets the emitter go, unless input ignores the focus (the menu check).
        private void OnApplicationFocus(bool focused)
        {
            if (!focused && InputSystem.settings.backgroundBehavior != InputSettings.BackgroundBehavior.IgnoreFocus)
            {
                Release();
            }
        }

        private void OnDisable()
        {
            Release();
        }

        private void OnDestroy()
        {
            if (_physicsMaterial != null)
            {
                Destroy(_physicsMaterial);
            }
        }

        // The controls (EM-6), relative to the camera's view of the board.
        private void ReadControls(float deltaTime)
        {
            // Taking and letting go (A1).
            Pointer pointer = Pointer.current;
            if (_grabAction != null && _grabAction.WasPressedThisFrame())
            {
                if (Held)
                {
                    Release();
                }
                else
                {
                    Take();
                }
            }
            else if (!Held && _clickAction != null && _clickAction.WasPressedThisFrame() && pointer != null && !UiInput.PointerOverUi
                && IsOnCone(pointer.position.ReadValue()))
            {
                Take();
                _clickTook = true;
            }

            bool clickDown = _clickAction != null && _clickAction.IsPressed();
            _clickTook &= clickDown;
            _sprayHeld = Held && clickDown && !_clickTook; // the click that took it doesn't spray (A8)

            if (_sprayAction != null && _sprayAction.WasPressedThisFrame())
            {
                Spraying = !Spraying;
            }

            // The gamepad's tilt, triggers and D-pad work only while it is held (ruling 80).
            _pressure = Held && _pressureAction != null ? Mathf.Clamp01(_pressureAction.ReadValue<float>()) : 0f;

            if (_resetTiltAction != null && _resetTiltAction.WasPressedThisFrame())
            {
                ResetTilt();
            }

            if (_resetPositionAction != null && _resetPositionAction.WasPressedThisFrame())
            {
                ResetPosition();
            }

            // The board's octave, held or not (ruling 83).
            if (_octaveDownAction != null && _octaveDownAction.WasPressedThisFrame())
            {
                _tuning.StepOctave(-1);
            }

            if (_octaveUpAction != null && _octaveUpAction.WasPressedThisFrame())
            {
                _tuning.StepOctave(1);
            }

            CameraAxes(out Vector3 right, out Vector3 forward);

            // The tilt (A3): the mouse while held, the right stick (B1).
            Vector2 turn = Vector2.zero;
            if (Held && _tiltAction != null)
            {
                turn += _tiltAction.ReadValue<Vector2>() * _tuning.TiltSensitivity;
            }

            if (Held && _stickTiltAction != null)
            {
                turn += _stickTiltAction.ReadValue<Vector2>() * (_tuning.StickTiltSpeed * deltaTime);
            }

            Vector3 along = (right * turn.x) + (forward * turn.y);
            ConeTilt.Turn(ref _setX, ref _setZ, along.x, along.z);

            // The wheel while held, the D-pad: Rate, or with a modifier its parameter (A6).
            int notches = Held && _wheelAction != null ? Notches(_wheelAction.ReadValue<Vector2>().y) : 0;
            float stepValue = Held && _wheelStepAction != null ? _wheelStepAction.ReadValue<float>() : 0f;
            int step = Mathf.Abs(stepValue) > 0.5f ? (int)Mathf.Sign(stepValue) : 0;
            if (step != 0 && step != _lastStep)
            {
                notches += step;
            }

            _lastStep = step;
            if (notches != 0)
            {
                Step(WheelTargetNow(), notches);
            }

            // The left trigger widens Spread by how far it is pulled; with A held it narrows it.
            float widen = Held && _spreadPressureAction != null ? Mathf.Clamp01(_spreadPressureAction.ReadValue<float>()) : 0f;
            if (widen > (float)TriggerRate.DeadZone)
            {
                float sign = _narrowAction != null && _narrowAction.IsPressed() ? -1f : 1f;
                _tuning.Spread = Mathf.Clamp(_tuning.Spread + (sign * widen * _tuning.SpreadSpeed * deltaTime), 0f, EmitterTuning.MostSpread);
            }

            // Moving (A9, A10).
            Vector2 move = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            Vector3 wish = (right * move.x) + (forward * move.y);
            if (_gridLockAction != null && _gridLockAction.IsPressed())
            {
                Glide(wish, deltaTime);
            }
            else
            {
                _gliding = false;
                _glideDirection = -1;
                transform.position += wish * (_tuning.MoveSpeed * deltaTime);
            }

            float raise = _raiseAction != null ? _raiseAction.ReadValue<float>() : 0f;
            _height += raise * _tuning.RaiseSpeed * deltaTime;
        }

        // The camera's right and forward, flattened onto the board; looking straight down, forward is up the screen.
        private void CameraAxes(out Vector3 right, out Vector3 forward)
        {
            forward = Vector3.forward;
            if (_camera != null)
            {
                forward = _camera.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 1e-6f)
                {
                    forward = _camera.transform.up;
                    forward.y = 0f;
                }
            }

            forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            right = Vector3.Cross(Vector3.up, forward);
        }

        // Notches of the wheel in a frame: Windows reports 120 a notch, other platforms 1.
        private static int Notches(float scroll)
        {
            float size = Mathf.Abs(scroll);
            if (size < 0.01f)
            {
                return 0;
            }

            float unit = size >= 30f ? 120f : 1f;
            return (int)Mathf.Sign(scroll) * Mathf.Max(1, Mathf.RoundToInt(size / unit));
        }

        private WheelTarget WheelTargetNow()
        {
            foreach (WheelBinding binding in _wheelBindings)
            {
                if (binding.Modifier != null && binding.Modifier.IsPressed())
                {
                    return binding.Target;
                }
            }

            return WheelTarget.Rate;
        }

        // A notch of Rate multiplies or divides it by Wheel Step, between Least Wheel Rate and Most Rate; of Spread, Spread Step degrees.
        private void Step(WheelTarget target, int notches)
        {
            switch (target)
            {
                case WheelTarget.Spread:
                    _tuning.Spread = Mathf.Clamp(_tuning.Spread + (notches * _tuning.SpreadStep), 0f, EmitterTuning.MostSpread);
                    break;
                default:
                    float rate = _tuning.Rate;
                    if (notches < 0 && rate <= EmitterTuning.LeastWheelRate)
                    {
                        break; // the panel sets 0; the wheel doesn't go below its least
                    }

                    _tuning.Rate = Mathf.Clamp(rate * Mathf.Pow(_tuning.WheelStep, notches), EmitterTuning.LeastWheelRate, EmitterTuning.MostRate);
                    break;
            }
        }

        // Grid Lock held (A9): along the nearest of the grid's six directions to the one asked for,
        // over the cells' middles, a cell at a time while it is asked for; let go, on to the
        // nearest cell's middle, and stop there.
        private void Glide(Vector3 wish, float deltaTime)
        {
            Vector3 here = transform.position;
            HexCoord nearest = CellUnder(here);
            float distance = _tuning.MoveSpeed * deltaTime;
            if (wish.sqrMagnitude > 0.01f)
            {
                int direction = NearestDirection(nearest, wish);
                if (!_gliding || direction != _glideDirection)
                {
                    _gliding = true;
                    _glideDirection = direction;
                    _glideTarget = Flat(CellMiddle(nearest) - here).magnitude <= Arrived
                        ? nearest.Neighbour((HexDirection)direction)
                        : nearest; // first onto the grid's lines
                }

                for (int cells = 0; cells < 16; cells++)
                {
                    Vector3 to = Flat(CellMiddle(_glideTarget) - here);
                    if (to.magnitude > distance)
                    {
                        here += to.normalized * distance;
                        break;
                    }

                    here += to;
                    distance -= to.magnitude;
                    _glideTarget = _glideTarget.Neighbour((HexDirection)_glideDirection);
                }
            }
            else if (_gliding)
            {
                _glideTarget = nearest;
                _glideDirection = -1;
                Vector3 to = Flat(CellMiddle(nearest) - here);
                if (to.magnitude > distance)
                {
                    here += to.normalized * distance;
                }
                else
                {
                    here += to;
                    _gliding = false;
                }
            }

            transform.position = here;
        }

        private int NearestDirection(HexCoord from, Vector3 wish)
        {
            Vector3 middle = CellMiddle(from);
            int best = 0;
            float bestDot = float.NegativeInfinity;
            for (int d = 0; d < HexDirections.Count; d++)
            {
                float dot = Vector3.Dot(Flat(CellMiddle(from.Neighbour((HexDirection)d)) - middle).normalized, wish);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = d;
                }
            }

            return best;
        }

        private HexCoord CellUnder(Vector3 world)
        {
            Vector3 local = _grid.transform.InverseTransformPoint(world);
            return _grid.Layout.Pick(local.x, local.z);
        }

        // A cell's middle in world space, at the grid's own height (only its x and z are used).
        private Vector3 CellMiddle(HexCoord coord)
        {
            HexPoint point = _grid.Layout.Position(coord);
            return _grid.transform.TransformPoint(new Vector3(point.X, 0f, point.Z));
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);

        // The cone follows the tilt set: at once, or most of the way (95 %) in Swing seconds (A11).
        private void Swing(float deltaTime)
        {
            float swing = _tuning.Swing;
            if (swing <= 0f)
            {
                _tiltX = _setX;
                _tiltZ = _setZ;
                return;
            }

            double follow = 1.0 - Math.Exp(-3.0 * deltaTime / swing);
            _tiltX += (_setX - _tiltX) * follow;
            _tiltZ += (_setZ - _tiltZ) * follow;
        }

        // Holds the emitter between its lowest and highest heights and turns it to its tilt.
        private void Orient()
        {
            float top = TopY();
            _height = Mathf.Clamp(_height, _tuning.MinHeight, _tuning.MaxHeight);
            Vector3 position = transform.position;
            position.y = top + _height;
            transform.position = position;

            ConeTilt.Direction(_tiltX, _tiltZ, out double x, out double y, out double z);
            transform.rotation = Quaternion.FromToRotation(Vector3.down, new Vector3((float)x, (float)y, (float)z));
        }

        // Where the axis's sphere and the spread's edge land (A4): a sphere of the middle size, at Speed, under gravity.
        private void Land()
        {
            Vector3 axis = Axis;
            float size = (_tuning.MinSize + _tuning.MaxSize) * 0.5f;
            Vector3 start = Nozzle + (axis * ((size * 0.5f) + Clearance));
            double gravity = -Physics.gravity.y;
            double plane = TopY();
            double speed = _tuning.Speed;

            _landed = SprayLanding.Land(start.x, start.y, start.z, axis.x * speed, axis.y * speed, axis.z * speed, gravity, plane,
                out double x, out double z, out _);
            if (_landed)
            {
                _aimPoint = new Vector3((float)x, (float)plane, (float)z);
            }

            double half = _tuning.Spread * 0.5;
            for (int i = 0; i < OutlinePoints; i++)
            {
                SprayLanding.Edge(axis.x, axis.y, axis.z, half, i, OutlinePoints, out double dx, out double dy, out double dz);
                bool lands = SprayLanding.Land(start.x, start.y, start.z, dx * speed, dy * speed, dz * speed, gravity, plane,
                    out double ex, out double ez, out _);
                _outlinePoints[i] = lands ? new Vector3((float)ex, (float)plane, (float)ez) : _aimPoint;
            }
        }

        // Launches one sphere, or none if there are Max Spheres already.
        private bool Launch()
        {
            if (_active.Count >= _tuning.MaxSpheres)
            {
                return false;
            }

            if (_tuning.Seed != _seed)
            {
                _seed = _tuning.Seed;
                _rng.Reset(_seed ^ SprayStream);
            }

            // Spread is the whole angle (A5); the draws take the half-angle, axis to edge.
            SprayDraw draw = SprayDraws.Draw(_rng, _tuning.MinSize, _tuning.MaxSize, _tuning.Speed, _tuning.SpeedJitter, _tuning.Spread * 0.5);
            float size = (float)draw.Size;
            Quaternion rotation = transform.rotation;
            Vector3 axis = rotation * Vector3.down;
            Vector3 direction = rotation * new Vector3((float)draw.X, (float)draw.Y, (float)draw.Z);
            Vector3 position = Nozzle + (axis * ((size * 0.5f) + Clearance));

            EmitterSphere sphere = _free.Count > 0 ? _free.Pop() : MakeSphere();
            sphere.transform.SetPositionAndRotation(position, Quaternion.identity);
            sphere.transform.localScale = Vector3.one * size;
            sphere.gameObject.SetActive(true);

            Rigidbody body = sphere.Body;
            body.position = position;
            body.rotation = Quaternion.identity;
            body.mass = Mathf.Max(0.001f, size * size * size); // by volume: a sphere of 1 weighs 1
            body.linearVelocity = direction * (float)draw.Speed;
            body.angularVelocity = Vector3.zero;

            sphere.Launch(size, Time.timeAsDouble);
            _active.Add(sphere);
            Launched++;
            return true;
        }

        private EmitterSphere MakeSphere()
        {
            GameObject made = GameObject.CreatePrimitive(PrimitiveType.Sphere); // a mesh, a renderer and a sphere collider of diameter 1
            made.SetActive(false);
            made.name = "Sphere";
            made.transform.SetParent(_sphereRoot, false);
            made.GetComponent<SphereCollider>().sharedMaterial = _physicsMaterial;

            MeshRenderer sphereRenderer = made.GetComponent<MeshRenderer>();
            if (_defaultSphereMaterial == null)
            {
                _defaultSphereMaterial = sphereRenderer.sharedMaterial;
            }

            sphereRenderer.sharedMaterial = _sphereMaterial != null ? _sphereMaterial : _defaultSphereMaterial;

            Rigidbody body = made.AddComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // a fast sphere can't pass through a cell
            body.interpolation = RigidbodyInterpolation.Interpolate;

            EmitterSphere sphere = made.AddComponent<EmitterSphere>();
            sphere.Begin(this, body);
            return sphere;
        }

        // A sphere past its lifetime, or fallen below the board, goes back to the pool.
        private void RetireSpent()
        {
            double now = Time.timeAsDouble;
            float lifetime = _tuning.Lifetime;
            float floor = TopY() - _gridTuning.Height - BelowBoard;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                EmitterSphere sphere = _active[i];
                if (now - sphere.Born >= lifetime || sphere.Body.position.y < floor)
                {
                    Retire(i);
                }
            }

            // Max Spheres lowered in play: the oldest go.
            while (_active.Count > _tuning.MaxSpheres)
            {
                int oldest = 0;
                for (int i = 1; i < _active.Count; i++)
                {
                    oldest = _active[i].Born < _active[oldest].Born ? i : oldest;
                }

                Retire(oldest);
            }
        }

        private void Retire(int index)
        {
            EmitterSphere sphere = _active[index];
            sphere.gameObject.SetActive(false);
            int last = _active.Count - 1;
            _active[index] = _active[last];
            _active.RemoveAt(last);
            _free.Push(sphere);
        }

        private void Glow(HexCellView cell)
        {
            int index = _glowing.IndexOf(cell);
            if (index < 0)
            {
                _glowing.Add(cell);
                _glowStart.Add(Time.timeAsDouble);
            }
            else
            {
                _glowStart[index] = Time.timeAsDouble;
            }

            cell.SetHit(1f, _tuning.HitGlow, _tuning.HitColor, _gridTuning);
        }

        // Each glow fades from its hit over Hit Fade; a cell the board has rebuilt away is dropped.
        private void FadeGlows()
        {
            double now = Time.timeAsDouble;
            float fade = _tuning.HitFade;
            for (int i = _glowing.Count - 1; i >= 0; i--)
            {
                HexCellView cell = _glowing[i];
                float amount = fade > 0f ? 1f - (float)((now - _glowStart[i]) / fade) : 0f;
                if (cell != null)
                {
                    cell.SetHit(amount, _tuning.HitGlow, _tuning.HitColor, _gridTuning);
                }

                if (cell == null || amount <= 0f)
                {
                    int last = _glowing.Count - 1;
                    _glowing[i] = _glowing[last];
                    _glowStart[i] = _glowStart[last];
                    _glowing.RemoveAt(last);
                    _glowStart.RemoveAt(last);
                }
            }
        }

        private void SyncSphereMaterial()
        {
            if (_tuning.SphereMaterial == _sphereMaterial)
            {
                return;
            }

            _sphereMaterial = _tuning.SphereMaterial;
            Material material = _sphereMaterial != null ? _sphereMaterial : _defaultSphereMaterial;
            foreach (MeshRenderer sphereRenderer in _sphereRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                sphereRenderer.sharedMaterial = material;
            }
        }

        // While held the cone takes Held Color through a property block; let go, its own colour.
        private void ShowHeld()
        {
            if (!Held && !_shownHeld)
            {
                return;
            }

            foreach (Renderer coneRenderer in _coneRenderers)
            {
                if (coneRenderer == null)
                {
                    continue;
                }

                if (Held)
                {
                    coneRenderer.GetPropertyBlock(_coneBlock);
                    _coneBlock.SetColor(_baseColorId, _tuning.HeldColor);
                    coneRenderer.SetPropertyBlock(_coneBlock);
                }
                else
                {
                    coneRenderer.SetPropertyBlock(null);
                }
            }

            _shownHeld = Held;
        }

        // A flat disc at the aim point, tinted through a property block; no collider.
        private Transform MakeMarker(Transform parent)
        {
            GameObject made = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // 2 high, 1 across
            made.name = "Aim";
            made.transform.SetParent(parent, false);
            Destroy(made.GetComponent<Collider>());
            _markerRenderer = made.GetComponent<MeshRenderer>();
            _markerRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _markerBlock = new MaterialPropertyBlock();
            made.SetActive(false);
            return made.transform;
        }

        // The outline where the spread's edge lands: one closed line on the board.
        private LineRenderer MakeOutline(Transform parent)
        {
            var made = new GameObject("Aim Outline");
            made.transform.SetParent(parent, false);
            LineRenderer line = made.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = true;
            line.positionCount = OutlinePoints;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.generateLightingData = true; // the aim's material is lit
            made.SetActive(false);
            return line;
        }

        private void PlaceMarker()
        {
            Material material = _tuning.AimMaterial;
            bool shown = material != null && _landed;
            if (_marker.gameObject.activeSelf != shown)
            {
                _marker.gameObject.SetActive(shown);
            }

            float width = _tuning.AimLineWidth;
            bool outlined = shown && width > 0f;
            if (_outline.gameObject.activeSelf != outlined)
            {
                _outline.gameObject.SetActive(outlined);
            }

            if (!shown)
            {
                return;
            }

            if (_markerRenderer.sharedMaterial != material)
            {
                _markerRenderer.sharedMaterial = material;
            }

            float lift = _tuning.LabelHeight + MarkerThickness;
            float size = _tuning.AimSize;
            _marker.SetPositionAndRotation(_aimPoint + (Vector3.up * lift), Quaternion.identity);
            _marker.localScale = new Vector3(size, MarkerThickness * 0.5f, size);
            _markerRenderer.GetPropertyBlock(_markerBlock);
            _markerBlock.SetColor(_baseColorId, _tuning.AimColor);
            _markerRenderer.SetPropertyBlock(_markerBlock);

            if (!outlined)
            {
                return;
            }

            for (int i = 0; i < OutlinePoints; i++)
            {
                _outlinePoints[i].y = _aimPoint.y + lift;
            }

            if (_outline.sharedMaterial != material)
            {
                _outline.sharedMaterial = material;
            }

            _outline.widthMultiplier = width;
            _outline.SetPositions(_outlinePoints);
            _outline.SetPropertyBlock(_markerBlock);
        }

        // The cone's lower end in the emitter's own space: the lowest point of its renderers' bounds, under the pivot.
        private Vector3 FindNozzle()
        {
            float lowest = float.PositiveInfinity;
            foreach (Renderer coneRenderer in _coneRenderers)
            {
                Bounds bounds = coneRenderer.localBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var local = new Vector3(
                        (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                    Vector3 own = transform.InverseTransformPoint(coneRenderer.transform.TransformPoint(local));
                    lowest = Mathf.Min(lowest, own.y);
                }
            }

            if (float.IsInfinity(lowest))
            {
                Debug.LogWarning("Emitter: the Emitter object has no renderer, so spheres start at its pivot.");
                return Vector3.zero;
            }

            return new Vector3(0f, lowest, 0f);
        }

        private float TopY() => _grid.transform.TransformPoint(0f, _grid.TopHeight, 0f).y;

        private static InputAction FindAction(string path)
        {
            InputAction action = InputSystem.actions != null ? InputSystem.actions.FindAction(path) : null;
            if (action == null)
            {
                Debug.LogError($"Emitter: the project-wide input actions have no {path}.");
            }

            return action;
        }

        // A modifier of the wheel and the parameter it gives the wheel while held (A6).
        private readonly struct WheelBinding
        {
            public WheelBinding(InputAction modifier, WheelTarget target)
            {
                Modifier = modifier;
                Target = target;
            }

            public InputAction Modifier { get; }

            public WheelTarget Target { get; }
        }
    }
}
