using UnityEngine;
using UnityEngine.EventSystems; // chặn click UI

public class FurniturePlacer : MonoBehaviour
{
    [Header("Placement settings")]
    public LayerMask floorMask;
    [Tooltip("Layer mask cho tường (chỉ dùng khi đặt đồ Wall). Có thể để 0 nếu chưa làm wall placement.")]
    public LayerMask wallMask;

    [Tooltip("Kích thước snap grid (m). 1 = 1m, 0.5 = nửa mét.")]
    public float gridSize = 1f;

    [Header("Input")]
    public KeyCode rotateKey = KeyCode.R;
    public float rotateStep = 90f;
    public KeyCode cancelKey = KeyCode.Escape;

    [Header("Conflict control")]
    public ObjectHoverInteractor hoverInteractor; // kéo ObjectHoverInteractor vào đây

    public bool IsPlacing { get; private set; }

    private Camera mainCamera;
    private GameObject previewObject;
    private FurnitureData currentData;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!IsPlacing || currentData == null || previewObject == null || mainCamera == null)
            return;

        // 1) Không xử lý click đặt nếu chuột đang nằm trên UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // 2) Chọn mask theo loại placement
        LayerMask maskToUse = floorMask;
        if (currentData.placementType == FurniturePlacementType.Wall && wallMask.value != 0)
            maskToUse = wallMask;

        // 3) Raycast
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, maskToUse))
        {
            Vector3 pos = hit.point;

            // 4) Snap grid cho X/Z (đồ sàn)
            if (currentData.placementType == FurniturePlacementType.Floor)
                pos = SnapToGridXZ(pos, gridSize);

            previewObject.transform.position = pos;

            // 5) Rotate
            if (Input.GetKeyDown(rotateKey))
                previewObject.transform.Rotate(0f, rotateStep, 0f);

            // 6) Place
            if (Input.GetMouseButtonDown(0))
                PlaceObject();
        }

        // 7) Cancel
        if (Input.GetKeyDown(cancelKey))
            CancelPlacement();
    }

    public void StartPlacement(FurnitureData data)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogWarning("FurniturePlacer: data hoặc prefab bị null!");
            return;
        }

        currentData = data;

        if (previewObject != null) Destroy(previewObject);

        previewObject = Instantiate(data.prefab);

        // Disable colliders để preview không cản raycast / hover
        foreach (Collider col in previewObject.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // làm preview trong suốt
        foreach (Renderer r in previewObject.GetComponentsInChildren<Renderer>())
        {
            if (r.material != null)
            {
                Color c = r.material.color;
                c.a = 0.4f;
                r.material.color = c;
            }
        }

        IsPlacing = true;

        // Tắt hover/focus để không xung đột click/double-click
        if (hoverInteractor != null) hoverInteractor.enabled = false;
    }

    private void PlaceObject()
    {
        Instantiate(currentData.prefab, previewObject.transform.position, previewObject.transform.rotation);

        Destroy(previewObject);
        previewObject = null;
        currentData = null;

        IsPlacing = false;
        if (hoverInteractor != null) hoverInteractor.enabled = true;
    }

    public void CancelPlacement()
    {
        currentData = null;

        if (previewObject != null) Destroy(previewObject);
        previewObject = null;

        IsPlacing = false;
        if (hoverInteractor != null) hoverInteractor.enabled = true;
    }

    private Vector3 SnapToGridXZ(Vector3 p, float step)
    {
        if (step <= 0.0001f) step = 1f;
        p.x = Mathf.Round(p.x / step) * step;
        p.z = Mathf.Round(p.z / step) * step;
        return p;
    }
}
