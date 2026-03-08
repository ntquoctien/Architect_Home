using UnityEngine;


// Tự động điều chỉnh kích thước của phòng cơ bản theo tham số cho sẵn.
[ExecuteAlways]
public class RoomController : MonoBehaviour
{
    [Header("Camera Pivot")]
    public Transform pivot; // Room/Pivot
    public bool autoCreatePivot = true;
    public bool pivotUseFloorBoundsCenter = true; 

    [Header("Dimensions (meters)")]
    [Min(1f)] public float width = 8f;      // X
    [Min(1f)] public float length = 10f;    // Z
    [Min(2f)] public float height = 3f;     // Y

    [Header("Thickness (meters)")]
    [Min(0.01f)] public float wallThickness = 0.1f;
    [Min(0.01f)] public float floorThickness = 0.05f;

    [Header("Optional offsets")]
    public bool wallsInsideRoom = true; // true: tường nằm trong mép phòng
    public bool autoUpdate = true;

    [Header("References")]
    public Transform floor;
    public Transform wallsRoot;
    public Transform wall01; // North (+Z) - Wall_0
    public Transform wall02; // South (-Z) - Wall_1
    public Transform wall03; // East  (+X) - Wall_2
    public Transform wall04; // West  (-X) - Wall_3

    [Header("Visual Theme (optional)")]
    [Tooltip("Kéo RoomThemeController vào đây để RoomController gọi apply materials/màu sau khi layout.")]
    public RoomThemeController themeController;

    void OnEnable()
    {
        if (autoUpdate) ApplyLayout();
    }

    void Update()
    {
        if (!Application.isPlaying && autoUpdate)
            ApplyLayout();
    }

    [ContextMenu("Apply Layout")]
    public void ApplyLayout()
    {
        if (!ValidateRefs()) return;

        Vector3 roomPos = transform.position;

        // 1) Floor (Cube)
        floor.position = roomPos + new Vector3(0f, -floorThickness * 0.5f, 0f);
        floor.localScale = new Vector3(width, floorThickness, length);

        // 2) Walls
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;
        float halfH = height * 0.5f;
        

        // Nếu tường nằm "bên trong" phòng, đẩy tường vào trong nửa thickness
        float inset = wallsInsideRoom ? wallThickness * 0.5f : 0f;

        // Wall_01 North (+Z)
        wall01.localRotation = Quaternion.identity;
        wall01.localScale = new Vector3(width, height, wallThickness);
        wall01.position = roomPos + new Vector3(0f, halfH, +(halfL - inset));

        // Wall_02 South (-Z)
        wall02.localRotation = Quaternion.identity;
        wall02.localScale = new Vector3(width, height, wallThickness);
        wall02.position = roomPos + new Vector3(0f, halfH, -(halfL - inset));

        // Wall_03 East (+X) : chạy theo length, xoay 90
        wall03.localRotation = Quaternion.Euler(0f, 90f, 0f);
        wall03.localScale = new Vector3(length, height, wallThickness);
        wall03.position = roomPos + new Vector3(+(halfW - inset), halfH, 0f);

        // Wall_04 West (-X)
        wall04.localRotation = Quaternion.Euler(0f, 90f, 0f);
        wall04.localScale = new Vector3(length, height, wallThickness);
        wall04.position = roomPos + new Vector3(-(halfW - inset), halfH, 0f);


        UpdatePivot();

        // 3) Apply visuals after layout (nếu có)
        if (themeController != null)
            themeController.ApplyTheme();
    }

    private void UpdatePivot()
    {
        if (pivot == null) return;

        // V1: dùng bounds center của Floor (đơn giản, đủ dùng)
        if (pivotUseFloorBoundsCenter)
        {
            var floorRenderer = floor != null ? floor.GetComponentInChildren<Renderer>() : null;
            if (floorRenderer != null)
            {
                Vector3 c = floorRenderer.bounds.center;
                pivot.position = new Vector3(c.x, transform.position.y, c.z);
                return;
            }
        }

        // fallback: dùng vị trí Room
        pivot.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
    }


    private bool ValidateRefs()
    {
        if (pivot == null)
        {
            pivot = transform.Find("Pivot");
            if (pivot == null && autoCreatePivot)
            {
                var go = new GameObject("Pivot");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                pivot = go.transform;
            }
        }

        // auto-find theo hierarchy chuẩn: Room/Floor, Room/Walls/Wall_0..3
        if (floor == null) floor = transform.Find("Floor");
        if (wallsRoot == null) wallsRoot = transform.Find("Walls");

        if (wallsRoot != null)
        {
            // Hỗ trợ cả 2 format: Wall_0 hoặc Wall_01
            if (wall01 == null) wall01 = wallsRoot.Find("Wall_0") ?? wallsRoot.Find("Wall_01");
            if (wall02 == null) wall02 = wallsRoot.Find("Wall_1") ?? wallsRoot.Find("Wall_02");
            if (wall03 == null) wall03 = wallsRoot.Find("Wall_2") ?? wallsRoot.Find("Wall_03");
            if (wall04 == null) wall04 = wallsRoot.Find("Wall_3") ?? wallsRoot.Find("Wall_04");
        }

        bool ok = floor && wallsRoot && wall01 && wall02 && wall03 && wall04;
        if (!ok)
        {
            Debug.LogWarning("RoomController: thiếu reference. Cần hierarchy: Floor, Walls/Wall_0..3 hoặc Wall_01..04");
        }

        // auto-link themeController nếu nó nằm trên Room
        if (themeController == null) themeController = GetComponent<RoomThemeController>();

        return ok;
    }
}
