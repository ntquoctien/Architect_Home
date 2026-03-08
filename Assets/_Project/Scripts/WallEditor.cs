using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WallEditor: Quản lý việc thêm/xóa tường trong phòng
/// 
/// ĐIỀU KIỆN:
/// - Tối thiểu 2 tường (để tạo góc)
/// - Tối đa 8 tường (phòng phức tạp)
/// - Tường mới phải nằm trên chu vi phòng
/// - Không cho phép xóa tường cuối cùng
/// 
/// VỊ TRÍ TƯỜNG:
/// - North (+Z), South (-Z), East (+X), West (-X)
/// - NorthEast, NorthWest, SouthEast, SouthWest (góc - tường phụ)
/// </summary>
public class WallEditor : MonoBehaviour
{
    public enum WallPosition
    {
        North,      // +Z
        South,      // -Z
        East,       // +X
        West,       // -X
        NorthEast,  // góc +X +Z
        NorthWest,  // góc -X +Z
        SouthEast,  // góc +X -Z
        SouthWest   // góc -X -Z
    }

    [Header("References")]
    public RoomController roomController;
    public WallManager wallManager;
    public Transform wallsRoot;

    [Header("Wall Prefab")]
    [Tooltip("Prefab tường cơ bản (Cube). Nếu null sẽ tạo Cube mới.")]
    public GameObject wallPrefab;

    [Header("Constraints")]
    [Min(2)] public int minWalls = 2;
    [Min(2)] public int maxWalls = 8;

    [Header("Wall Settings")]
    public float defaultWallLength = 4f;
    public Material wallMaterial;

    [Header("Events")]
    public System.Action onWallsChanged;

    // Danh sách tường hiện có (theo tên)
    private readonly Dictionary<string, Transform> _wallDict = new();

    void Awake()
    {
        AutoFindRefs();
        RefreshWallDict();
    }

    private void AutoFindRefs()
    {
        if (roomController == null)
            roomController = GetComponent<RoomController>();

        if (wallManager == null)
            wallManager = GetComponent<WallManager>();

        if (wallsRoot == null && roomController != null)
            wallsRoot = roomController.wallsRoot;

        if (wallsRoot == null)
            wallsRoot = transform.Find("Walls");
    }

    /// <summary>
    /// Cập nhật dictionary tường từ hierarchy
    /// </summary>
    public void RefreshWallDict()
    {
        _wallDict.Clear();
        if (wallsRoot == null) return;

        for (int i = 0; i < wallsRoot.childCount; i++)
        {
            var child = wallsRoot.GetChild(i);
            if (child.name.StartsWith("Wall_"))
            {
                _wallDict[child.name] = child;
            }
        }
    }

    /// <summary>
    /// Lấy số tường hiện có
    /// </summary>
    public int GetWallCount()
    {
        RefreshWallDict();
        return _wallDict.Count;
    }

    /// <summary>
    /// Kiểm tra có thể thêm tường không
    /// </summary>
    public bool CanAddWall()
    {
        return GetWallCount() < maxWalls;
    }

    /// <summary>
    /// Kiểm tra có thể xóa tường không
    /// </summary>
    public bool CanRemoveWall()
    {
        return GetWallCount() > minWalls;
    }

    /// <summary>
    /// Kiểm tra tường có tồn tại không
    /// </summary>
    public bool HasWall(string wallName)
    {
        RefreshWallDict();
        return _wallDict.ContainsKey(wallName);
    }

    /// <summary>
    /// Thêm tường mới tại vị trí xác định
    /// </summary>
    /// <param name="position">Vị trí tường (North, South, East, West, ...)</param>
    /// <param name="customLength">Chiều dài tùy chỉnh (0 = tự động)</param>
    /// <returns>Transform của tường mới, null nếu thất bại</returns>
    public Transform AddWall(WallPosition position, float customLength = 0f)
    {
        if (!CanAddWall())
        {
            Debug.LogWarning($"WallEditor: Đã đạt giới hạn {maxWalls} tường!");
            return null;
        }

        AutoFindRefs();
        if (wallsRoot == null)
        {
            Debug.LogError("WallEditor: Không tìm thấy wallsRoot!");
            return null;
        }

        // Tạo tên tường mới
        string wallName = GenerateWallName(position);
        if (HasWall(wallName))
        {
            Debug.LogWarning($"WallEditor: Tường {wallName} đã tồn tại!");
            return null;
        }

        // Tạo tường mới
        GameObject wallGO;
        if (wallPrefab != null)
        {
            wallGO = Instantiate(wallPrefab, wallsRoot);
        }
        else
        {
            wallGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallGO.transform.SetParent(wallsRoot);
        }

        wallGO.name = wallName;

        // Áp dụng material nếu có
        if (wallMaterial != null)
        {
            var renderer = wallGO.GetComponent<Renderer>();
            if (renderer != null) renderer.material = wallMaterial;
        }

        // Tính toán vị trí và kích thước
        ApplyWallTransform(wallGO.transform, position, customLength);

        // Cập nhật dictionary
        _wallDict[wallName] = wallGO.transform;

        // Thông báo cho các hệ thống khác
        NotifyWallsChanged();

        Debug.Log($"WallEditor: Đã thêm tường {wallName}");
        return wallGO.transform;
    }

    /// <summary>
    /// Xóa tường theo tên
    /// </summary>
    public bool RemoveWall(string wallName)
    {
        if (!CanRemoveWall())
        {
            Debug.LogWarning($"WallEditor: Không thể xóa! Cần ít nhất {minWalls} tường.");
            return false;
        }

        RefreshWallDict();
        if (!_wallDict.TryGetValue(wallName, out Transform wallTf))
        {
            Debug.LogWarning($"WallEditor: Không tìm thấy tường {wallName}!");
            return false;
        }

        // Kiểm tra không phải tường chính (4 tường cơ bản)
        if (IsMainWall(wallName))
        {
            Debug.LogWarning($"WallEditor: Không thể xóa tường chính {wallName}. Chỉ có thể ẩn.");
            return false;
        }

        // Xóa tường
        _wallDict.Remove(wallName);

        if (Application.isPlaying)
            Destroy(wallTf.gameObject);
        else
            DestroyImmediate(wallTf.gameObject);

        NotifyWallsChanged();

        Debug.Log($"WallEditor: Đã xóa tường {wallName}");
        return true;
    }

    /// <summary>
    /// Xóa tường theo vị trí
    /// </summary>
    public bool RemoveWall(WallPosition position)
    {
        string wallName = GenerateWallName(position);
        return RemoveWall(wallName);
    }

    /// <summary>
    /// Ẩn/hiện tường (thay vì xóa)
    /// </summary>
    public void SetWallVisible(string wallName, bool visible)
    {
        RefreshWallDict();
        if (_wallDict.TryGetValue(wallName, out Transform wallTf))
        {
            wallTf.gameObject.SetActive(visible);
            NotifyWallsChanged();
        }
    }

    /// <summary>
    /// Thêm tường phân chia phòng (interior wall)
    /// </summary>
    /// <param name="startPoint">Điểm bắt đầu (local position)</param>
    /// <param name="endPoint">Điểm kết thúc (local position)</param>
    public Transform AddInteriorWall(Vector3 startPoint, Vector3 endPoint)
    {
        if (!CanAddWall())
        {
            Debug.LogWarning($"WallEditor: Đã đạt giới hạn {maxWalls} tường!");
            return null;
        }

        AutoFindRefs();
        if (wallsRoot == null) return null;

        // Tạo tên unique
        string wallName = $"Wall_Interior_{System.DateTime.Now.Ticks % 10000}";

        // Tạo tường
        GameObject wallGO;
        if (wallPrefab != null)
        {
            wallGO = Instantiate(wallPrefab, wallsRoot);
        }
        else
        {
            wallGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallGO.transform.SetParent(wallsRoot);
        }

        wallGO.name = wallName;

        // Tính toán transform
        Vector3 center = (startPoint + endPoint) / 2f;
        float wallLength = Vector3.Distance(startPoint, endPoint);
        Vector3 direction = (endPoint - startPoint).normalized;
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        float wallHeight = roomController != null ? roomController.height : 3f;
        float wallThickness = roomController != null ? roomController.wallThickness : 0.1f;

        wallGO.transform.localPosition = center + Vector3.up * (wallHeight / 2f);
        wallGO.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
        wallGO.transform.localScale = new Vector3(wallLength, wallHeight, wallThickness);

        // Áp dụng material
        if (wallMaterial != null)
        {
            var renderer = wallGO.GetComponent<Renderer>();
            if (renderer != null) renderer.material = wallMaterial;
        }

        _wallDict[wallName] = wallGO.transform;
        NotifyWallsChanged();

        Debug.Log($"WallEditor: Đã thêm tường nội thất {wallName}");
        return wallGO.transform;
    }

    /// <summary>
    /// Lấy danh sách tất cả tường
    /// </summary>
    public List<Transform> GetAllWalls()
    {
        RefreshWallDict();
        return new List<Transform>(_wallDict.Values);
    }

    /// <summary>
    /// Lấy danh sách tên tường
    /// </summary>
    public List<string> GetAllWallNames()
    {
        RefreshWallDict();
        return new List<string>(_wallDict.Keys);
    }

    // ===== Private Methods =====

    private string GenerateWallName(WallPosition position)
    {
        // Hỗ trợ format Wall_0, Wall_1... (như hierarchy của user)
        return position switch
        {
            WallPosition.North => "Wall_0",
            WallPosition.South => "Wall_1",
            WallPosition.East => "Wall_2",
            WallPosition.West => "Wall_3",
            WallPosition.NorthEast => "Wall_4_NE",
            WallPosition.NorthWest => "Wall_5_NW",
            WallPosition.SouthEast => "Wall_6_SE",
            WallPosition.SouthWest => "Wall_7_SW",
            _ => $"Wall_{GetWallCount()}"
        };
    }
    
    /// <summary>
    /// Kiểm tra xem tường có phải là tường chính không (4 tường cơ bản)
    /// </summary>
    private bool IsMainWall(string wallName)
    {
        // Hỗ trợ cả 2 format
        return wallName == "Wall_0" || wallName == "Wall_01" ||
               wallName == "Wall_1" || wallName == "Wall_02" ||
               wallName == "Wall_2" || wallName == "Wall_03" ||
               wallName == "Wall_3" || wallName == "Wall_04";
    }

    private void ApplyWallTransform(Transform wallTf, WallPosition position, float customLength)
    {
        if (roomController == null)
        {
            Debug.LogWarning("WallEditor: Cần RoomController để tính toán vị trí tường.");
            return;
        }

        float width = roomController.width;
        float length = roomController.length;
        float height = roomController.height;
        float thickness = roomController.wallThickness;
        float inset = roomController.wallsInsideRoom ? thickness * 0.5f : 0f;

        float halfW = width / 2f;
        float halfL = length / 2f;
        float halfH = height / 2f;

        Vector3 roomPos = roomController.transform.position;
        float wallLen = customLength > 0 ? customLength : defaultWallLength;

        switch (position)
        {
            case WallPosition.North:
                wallTf.localRotation = Quaternion.identity;
                wallTf.localScale = new Vector3(width, height, thickness);
                wallTf.position = roomPos + new Vector3(0f, halfH, +(halfL - inset));
                break;

            case WallPosition.South:
                wallTf.localRotation = Quaternion.identity;
                wallTf.localScale = new Vector3(width, height, thickness);
                wallTf.position = roomPos + new Vector3(0f, halfH, -(halfL - inset));
                break;

            case WallPosition.East:
                wallTf.localRotation = Quaternion.Euler(0f, 90f, 0f);
                wallTf.localScale = new Vector3(length, height, thickness);
                wallTf.position = roomPos + new Vector3(+(halfW - inset), halfH, 0f);
                break;

            case WallPosition.West:
                wallTf.localRotation = Quaternion.Euler(0f, 90f, 0f);
                wallTf.localScale = new Vector3(length, height, thickness);
                wallTf.position = roomPos + new Vector3(-(halfW - inset), halfH, 0f);
                break;

            // Tường góc - nhỏ hơn, đặt ở góc phòng
            case WallPosition.NorthEast:
                wallTf.localRotation = Quaternion.Euler(0f, 45f, 0f);
                wallTf.localScale = new Vector3(wallLen, height, thickness);
                wallTf.position = roomPos + new Vector3(halfW - inset, halfH, halfL - inset);
                break;

            case WallPosition.NorthWest:
                wallTf.localRotation = Quaternion.Euler(0f, -45f, 0f);
                wallTf.localScale = new Vector3(wallLen, height, thickness);
                wallTf.position = roomPos + new Vector3(-halfW + inset, halfH, halfL - inset);
                break;

            case WallPosition.SouthEast:
                wallTf.localRotation = Quaternion.Euler(0f, -45f, 0f);
                wallTf.localScale = new Vector3(wallLen, height, thickness);
                wallTf.position = roomPos + new Vector3(halfW - inset, halfH, -halfL + inset);
                break;

            case WallPosition.SouthWest:
                wallTf.localRotation = Quaternion.Euler(0f, 45f, 0f);
                wallTf.localScale = new Vector3(wallLen, height, thickness);
                wallTf.position = roomPos + new Vector3(-halfW + inset, halfH, -halfL + inset);
                break;
        }
    }

    private void NotifyWallsChanged()
    {
        // Thông báo WallManager refresh
        if (wallManager != null)
        {
            wallManager.RefreshWallsNow();
        }

        // Gọi callback nếu có
        onWallsChanged?.Invoke();
    }

    // ===== Context Menu (Editor) =====

    [ContextMenu("Add Wall - NorthEast Corner")]
    private void AddWallNE() => AddWall(WallPosition.NorthEast);

    [ContextMenu("Add Wall - NorthWest Corner")]
    private void AddWallNW() => AddWall(WallPosition.NorthWest);

    [ContextMenu("Add Wall - SouthEast Corner")]
    private void AddWallSE() => AddWall(WallPosition.SouthEast);

    [ContextMenu("Add Wall - SouthWest Corner")]
    private void AddWallSW() => AddWall(WallPosition.SouthWest);

    [ContextMenu("Refresh Wall List")]
    private void RefreshAndLog()
    {
        RefreshWallDict();
        Debug.Log($"WallEditor: Có {_wallDict.Count} tường: {string.Join(", ", _wallDict.Keys)}");
    }
}
