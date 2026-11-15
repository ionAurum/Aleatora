using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Selecting a cell and pointing the camera at it. A click (Player/Left Click) casts the
    /// pointer's ray onto the plane of the cells' tops and asks the layout which cell is there, so
    /// cells need no colliders; a click off the board selects nothing. Focus (Player/Focus) has the
    /// scene's Cinemachine camera follow the selected cell, or the board's middle with none
    /// selected. The actions are the project-wide ones, found by name. While a menu is open
    /// (MenuFocus.IsAnyOpen) it does nothing, and the camera's own input (orbit, zoom) is off.
    /// Made by the bootstrap (Aleatora), beside HexGrid.
    /// </summary>
    public class GridSelection : MonoBehaviour
    {
        private const string SelectActionPath = "Player/Left Click";
        private const string FocusActionPath = "Player/Focus";

        private HexGrid _grid;
        private GridTuning _tuning;
        private Camera _camera;
        private CinemachineCamera _cinemachineCamera;
        private CinemachineInputAxisController _cameraInput;
        private InputAction _selectAction;
        private InputAction _focusAction;
        private bool _hasSelection;
        private bool _followsSelection;

        /// <summary>The selected cell, if HasSelection.</summary>
        public HexCoord Selected { get; private set; }

        /// <summary>Whether a cell is selected.</summary>
        public bool HasSelection => _hasSelection;

        /// <summary>Gives the selection its grid and tuning, and points the camera at the board's middle.</summary>
        public void Begin(HexGrid grid, GridTuning tuning)
        {
            _grid = grid;
            _tuning = tuning;
            _grid.Rebuilt += OnRebuilt;

            _camera = Camera.main;
            _cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
            if (_cinemachineCamera != null)
            {
                _cameraInput = _cinemachineCamera.GetComponent<CinemachineInputAxisController>();
            }
            else
            {
                Debug.LogWarning("GridSelection: the scene has no Cinemachine camera, so Focus does nothing.");
            }

            _selectAction = FindAction(SelectActionPath);
            _focusAction = FindAction(FocusActionPath);
            Follow();
        }

        /// <summary>
        /// Selects the cell under a point of the screen, as a click there does; a point off the
        /// board selects nothing. False, and nothing changes, while a menu is open.
        /// </summary>
        public bool PickAt(Vector2 screenPoint)
        {
            if (MenuFocus.IsAnyOpen || _grid == null || _grid.Shape == null || _camera == null)
            {
                return false;
            }

            // Onto the plane of the cells' tops, in the grid's own space.
            Ray ray = _camera.ScreenPointToRay(screenPoint);
            Vector3 origin = _grid.transform.InverseTransformPoint(ray.origin);
            Vector3 direction = _grid.transform.InverseTransformDirection(ray.direction);
            float distance = Mathf.Abs(direction.y) > 1e-6f ? (_grid.TopHeight - origin.y) / direction.y : -1f;
            if (distance < 0f)
            {
                Deselect();
                return true;
            }

            Vector3 point = origin + (direction * distance);
            HexCoord coord = _grid.Layout.Pick(point.x, point.z);
            if (_grid.Shape.Contains(coord))
            {
                Select(coord);
            }
            else
            {
                Deselect();
            }

            return true;
        }

        /// <summary>Selects a cell of the board; one that isn't on it selects nothing.</summary>
        public void Select(HexCoord coord)
        {
            Deselect();
            if (_grid.TryGetCell(coord, out HexCellView cell))
            {
                Selected = coord;
                _hasSelection = true;
                cell.SetSelected(true, _tuning);
            }
        }

        /// <summary>Selects nothing.</summary>
        public void Deselect()
        {
            if (_hasSelection && _grid.TryGetCell(Selected, out HexCellView cell))
            {
                cell.SetSelected(false, _tuning);
            }

            _hasSelection = false;
        }

        /// <summary>Has the camera follow the selected cell, or the board's middle with none selected.</summary>
        public void Focus()
        {
            _followsSelection = _hasSelection;
            Follow();
        }

        //----------

        private void OnDestroy()
        {
            if (_grid != null)
            {
                _grid.Rebuilt -= OnRebuilt;
            }
        }

        private void Update()
        {
            bool menuOpen = MenuFocus.IsAnyOpen;
            if (_cameraInput != null && _cameraInput.enabled == menuOpen)
            {
                _cameraInput.enabled = !menuOpen;
            }

            if (menuOpen)
            {
                return;
            }

            if (_selectAction != null && _selectAction.WasPressedThisFrame() && Pointer.current != null)
            {
                PickAt(Pointer.current.position.ReadValue());
            }

            if (_focusAction != null && _focusAction.WasPressedThisFrame())
            {
                Focus();
            }
        }

        // Every cell is new. The selection stays if its cell is still on the board.
        private void OnRebuilt()
        {
            if (_hasSelection && _grid.TryGetCell(Selected, out HexCellView cell))
            {
                cell.SetSelected(true, _tuning);
            }
            else
            {
                _hasSelection = false;
                _followsSelection = false;
            }

            Follow();
        }

        private void Follow()
        {
            if (_cinemachineCamera == null)
            {
                return;
            }

            Transform target = _grid.Centre;
            if (_followsSelection && _hasSelection && _grid.TryGetCell(Selected, out HexCellView cell))
            {
                target = cell.transform;
            }

            _cinemachineCamera.Follow = target;
        }

        private static InputAction FindAction(string path)
        {
            InputAction action = InputSystem.actions != null ? InputSystem.actions.FindAction(path) : null;
            if (action == null)
            {
                Debug.LogError($"GridSelection: the project-wide input actions have no {path}.");
            }

            return action;
        }
    }
}
