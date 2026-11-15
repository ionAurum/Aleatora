using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(HexGrid))]
public class SelectionManager : MonoBehaviour
{
    public HexGrid hexGrid;

    public InputActionReference selectActionReference;

    public CinemachineCamera cinemachineCamera;
    public InputActionReference focusActionReference;

    public LayerMask hexagonLayerMask;

    public Transform selected;
    public Color selectedColor = new Color(68f / 255f, 214f / 255f, 44f / 255f);
    private Color originalColor;

    private void Awake()
    {
        hexGrid = GetComponent<HexGrid>();
    }

    private void Update()
    {
        if (selectActionReference.action.WasPressedThisFrame())
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out var hit, 999f, hexagonLayerMask))
                SetSelected(hit.collider.transform);
            else
                SetSelected(null);
        }

        if (cinemachineCamera != null && focusActionReference.action.WasPressedThisFrame())
        {
            if (selected == null)
                cinemachineCamera.Follow = hexGrid.hexagons[hexGrid.rows / 2, hexGrid.columns / 2].transform;
            else
                cinemachineCamera.Follow = selected;
        }
    }

    public void SetSelected(Transform target)
    {
        if (selected != null && selected.TryGetComponent<MeshRenderer>(out var renderer))
        {
            renderer.material.color = originalColor;
            //renderer.material.SetColor("_EmissionColor", originalColor);
            //renderer.material.DisableKeyword("_EMISSION");
        }

        selected = target;
        if (selected != null && selected.TryGetComponent<MeshRenderer>(out renderer))
        {
            originalColor = renderer.material.color;
            
            renderer.material.color = selectedColor;
            //renderer.material.EnableKeyword("_EMISSION");
            //renderer.material.SetColor("_EmissionColor", selectedColor);
        }
    }
}
