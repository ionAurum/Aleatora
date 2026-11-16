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
    /// hits into notes. The first controls (decision 8): Spray toggles spraying, the right trigger
    /// sprays by how far it is pressed, Move Emitter moves it over the board relative to the camera
    /// (the aim point goes with it), Raise Emitter lowers and raises it, Aim held has the aim point
    /// follow the pointer, Aim Point moves it. The cone always points at the aim point, which a
    /// marker shows on the board. While a menu is open it reads no input and launches nothing;
    /// spheres in the air land. While a text field has the keyboard (the panel's Seed) it reads no
    /// input either, and Aim ignores the pointer over the UI (EM-4). Its tuning (EmitterTuning) is read every frame. The bootstrap
    /// (Aleatora) adds it to the scene's Emitter object.
    /// </summary>
    public class Emitter : MonoBehaviour
    {
        private const string SprayActionPath = "Player/Spray";
        private const string PressureActionPath = "Player/Spray Pressure";
        private const string MoveActionPath = "Player/Move Emitter";
        private const string RaiseActionPath = "Player/Raise Emitter";
        private const string AimActionPath = "Player/Aim";
        private const string AimPointActionPath = "Player/Aim Point";

        private const ulong SprayStream = 0x5350524159UL; // "SPRAY": the spray's draws apart from the layout's, from one seed
        private const float BelowBoard = 2f;               // a sphere this far below the cells' bottoms is gone
        private const float Clearance = 0.01f;             // between the cone's end and a new sphere
        private const float MarkerThickness = 0.01f;

        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<EmitterSphere> _active = new();
        private readonly Stack<EmitterSphere> _free = new();
        private readonly List<HexCellView> _glowing = new();
        private readonly List<double> _glowStart = new();

        private HexGrid _grid;
        private GridTuning _gridTuning;
        private BoardNotes _board;
        private EmitterTuning _tuning;
        private Camera _camera;
        private InputAction _sprayAction, _pressureAction, _moveAction, _raiseAction, _aimAction, _aimPointAction;
        private Transform _sphereRoot;
        private PhysicsMaterial _physicsMaterial;
        private Material _defaultSphereMaterial, _sphereMaterial;
        private Rng _rng;
        private ulong _seed;
        private Vector3 _nozzle;   // the cone's lower end, in the emitter's own space
        private Vector3 _aimPoint; // on the plane of the cells' tops, in world space
        private float _height;     // the emitter's height above the cells' tops
        private float _rate;       // spheres a second, as the controls and the menus have it this frame
        private float _owed = 1f;  // spheres owed; starts at one, so spraying launches at once
        private float _pressure;
        private Transform _marker;
        private MeshRenderer _markerRenderer;
        private MaterialPropertyBlock _markerBlock;

        /// <summary>Whether spraying is on (Spray toggles it). The right trigger sprays while it is off.</summary>
        public bool Spraying { get; set; }

        /// <summary>Spheres a second being launched this frame: 0 while a menu is open or nothing sprays.</summary>
        public float CurrentRate => _rate;

        /// <summary>The spheres in flight or at rest on the board.</summary>
        public IReadOnlyList<EmitterSphere> Spheres => _active;

        /// <summary>How many spheres have been launched since the emitter began.</summary>
        public int Launched { get; private set; }

        /// <summary>How many hits there have been since the emitter began.</summary>
        public int Hits { get; private set; }

        /// <summary>Where the cone points, on the plane of the cells' tops.</summary>
        public Vector3 AimPoint => _aimPoint;

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
            _aimAction = FindAction(AimActionPath);
            _aimPointAction = FindAction(AimPointActionPath);

            _nozzle = FindNozzle();
            _sphereMaterial = _tuning.SphereMaterial;
            _seed = _tuning.Seed;
            _rng = new Rng(_seed ^ SprayStream);
            _height = transform.position.y - TopY();
            _aimPoint = new Vector3(transform.position.x, TopY(), transform.position.z); // straight down
            _marker = MakeMarker(parent);
            Orient();
        }

        /// <summary>Points the cone at a point of the board (its height is taken as the cells' tops').</summary>
        public void AimAt(Vector3 point)
        {
            _aimPoint = point;
            Orient();
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
            if (!menuOpen && !UiInput.IsTyping)
            {
                ReadControls(Time.deltaTime);
            }

            _rate = menuOpen ? 0f : _tuning.Rate * (Spraying ? 1f : _pressure);
            Orient();
            SyncSphereMaterial();
            FadeGlows();
            PlaceMarker();
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

        private void OnDestroy()
        {
            if (_physicsMaterial != null)
            {
                Destroy(_physicsMaterial);
            }
        }

        // The first controls (decision 8), relative to the camera's view of the board.
        private void ReadControls(float deltaTime)
        {
            if (_sprayAction != null && _sprayAction.WasPressedThisFrame())
            {
                Spraying = !Spraying;
            }

            _pressure = _pressureAction != null ? Mathf.Clamp01(_pressureAction.ReadValue<float>()) : 0f;

            Vector3 forward = Vector3.forward;
            if (_camera != null)
            {
                forward = _camera.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 1e-6f)
                {
                    forward = _camera.transform.up; // looking straight down: up the screen
                    forward.y = 0f;
                }
            }

            forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector2 move = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            Vector3 step = ((right * move.x) + (forward * move.y)) * (_tuning.MoveSpeed * deltaTime);
            transform.position += step;
            _aimPoint += step;

            float raise = _raiseAction != null ? _raiseAction.ReadValue<float>() : 0f;
            _height += raise * _tuning.RaiseSpeed * deltaTime;

            if (_aimAction != null && _aimAction.IsPressed() && Pointer.current != null && !UiInput.PointerOverUi
                && PointOnBoard(Pointer.current.position.ReadValue(), out Vector3 pointed))
            {
                _aimPoint = pointed;
            }

            Vector2 aim = _aimPointAction != null ? _aimPointAction.ReadValue<Vector2>() : Vector2.zero;
            _aimPoint += ((right * aim.x) + (forward * aim.y)) * (_tuning.AimSpeed * deltaTime);
        }

        // Holds the emitter between its lowest and highest heights and points it at the aim point.
        private void Orient()
        {
            float top = TopY();
            _height = Mathf.Clamp(_height, _tuning.MinHeight, _tuning.MaxHeight);
            Vector3 position = transform.position;
            position.y = top + _height;
            transform.position = position;

            _aimPoint.y = top;
            Vector3 toAim = _aimPoint - position;
            if (toAim.sqrMagnitude > 1e-6f)
            {
                transform.rotation = Quaternion.FromToRotation(Vector3.down, toAim.normalized);
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

            SprayDraw draw = SprayDraws.Draw(_rng, _tuning.MinSize, _tuning.MaxSize, _tuning.Speed, _tuning.SpeedJitter, _tuning.Spread);
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

        private void PlaceMarker()
        {
            Material material = _tuning.AimMaterial;
            bool shown = material != null;
            if (_marker.gameObject.activeSelf != shown)
            {
                _marker.gameObject.SetActive(shown);
            }

            if (!shown)
            {
                return;
            }

            if (_markerRenderer.sharedMaterial != material)
            {
                _markerRenderer.sharedMaterial = material;
            }

            float size = _tuning.AimSize;
            _marker.SetPositionAndRotation(_aimPoint + (Vector3.up * (_tuning.LabelHeight + MarkerThickness)), Quaternion.identity);
            _marker.localScale = new Vector3(size, MarkerThickness * 0.5f, size);
            _markerRenderer.GetPropertyBlock(_markerBlock);
            _markerBlock.SetColor(_baseColorId, _tuning.AimColor);
            _markerRenderer.SetPropertyBlock(_markerBlock);
        }

        // The cone's lower end in the emitter's own space: the lowest point of its renderers' bounds, under the pivot.
        private Vector3 FindNozzle()
        {
            float lowest = float.PositiveInfinity;
            foreach (Renderer coneRenderer in GetComponentsInChildren<Renderer>())
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

        private bool PointOnBoard(Vector2 screenPoint, out Vector3 point)
        {
            point = default;
            if (_camera == null)
            {
                return false;
            }

            Ray ray = _camera.ScreenPointToRay(screenPoint);
            var plane = new Plane(Vector3.up, new Vector3(0f, TopY(), 0f));
            if (!plane.Raycast(ray, out float distance) || distance <= 0f)
            {
                return false;
            }

            point = ray.GetPoint(distance);
            return true;
        }

        private static InputAction FindAction(string path)
        {
            InputAction action = InputSystem.actions != null ? InputSystem.actions.FindAction(path) : null;
            if (action == null)
            {
                Debug.LogError($"Emitter: the project-wide input actions have no {path}.");
            }

            return action;
        }
    }
}
