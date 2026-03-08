using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WallManager: gộp chức năng của RoomWallHider + AutoHideWall.
/// - Tự động lấy toàn bộ tường hiện có (và tường mới spawn thêm) theo quy ước:
///   Room/Walls/Wall_*
/// - Ẩn/hiện tường theo hướng camera so với tâm phòng để không che tầm nhìn.
/// - Không cần gắn script lên từng Wall.
/// 
/// Quy ước:
/// Room
/// ├── Pivot (tâm phòng)
/// └── Walls
///     ├── Wall_01 ...
///     └── Wall_XX ...
/// </summary>
public class WallManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Room/Pivot (tâm phòng). Nếu null sẽ tự tìm Pivot dưới Room.")]
    public Transform roomCenter;

    [Tooltip("Root chứa các wall. Nếu null sẽ tự tìm Walls dưới Room.")]
    public Transform wallsRoot;

    [Tooltip("Camera dùng để quyết định hide. Nếu null -> Camera.main")]
    public Camera cam;

    [Header("Auto collect walls")]
    [Tooltip("Chỉ lấy các object con trực tiếp của Walls có tên bắt đầu bằng 'Wall_'")]
    public bool onlyDirectChildren = true;

    [Tooltip("Tự refresh danh sách walls theo chu kỳ để bắt được wall spawn thêm.")]
    public bool autoRefresh = true;

    [Tooltip("Số giây giữa mỗi lần refresh danh sách walls.")]
    [Min(0.1f)] public float refreshInterval = 0.5f;

    [Header("Hide logic")]
    [Tooltip("Chỉ xét mặt phẳng XZ (ổn định cho decor game).")]
    public bool ignoreY = true;

    [Tooltip("Ngưỡng dot để bắt đầu hide. Tăng nếu hide quá nhiều.")]
    [Range(0f, 0.9f)] public float hideDotThreshold = 0.15f;

    [Tooltip("Nếu true: ẩn các wall ở phía camera (thường dùng).")]
    public bool hideWallsFacingCamera = true;

    [Header("Debug")]
    public bool debugDraw = false;

    // internal
    private readonly List<WallEntry> _walls = new();
    private float _nextRefreshTime;

    private struct WallEntry
    {
        public Transform wallTf;
        public Renderer[] renderers;
        // Không lưu normal nữa - sẽ tính realtime
    }

    void Awake()
    {
        AutoFindRefs();
        RefreshWalls(force: true);
    }

    void Start()
    {
        if (cam == null) cam = Camera.main;
        _nextRefreshTime = Time.time + refreshInterval;
    }

    void LateUpdate()
    {
        AutoFindRefs();

        if (autoRefresh && Time.time >= _nextRefreshTime)
        {
            RefreshWalls(force: false);
            _nextRefreshTime = Time.time + refreshInterval;
        }

        ApplyHideLogic();
    }

    [ContextMenu("Refresh Walls Now")]
    public void RefreshWallsNow()
    {
        RefreshWalls(force: true);
    }

    private void AutoFindRefs()
    {
        if (roomCenter == null)
        {
            // ưu tiên Pivot
            var pivot = transform.Find("Pivot");
            if (pivot != null) roomCenter = pivot;
        }

        if (wallsRoot == null)
        {
            var w = transform.Find("Walls");
            if (w != null) wallsRoot = w;
        }

        if (cam == null) cam = Camera.main;
    }

    private void RefreshWalls(bool force)
    {
        if (wallsRoot == null) return;

        // Nếu không force, chỉ rebuild khi count thay đổi (nhẹ)
        if (!force)
        {
            int currentCount = CountCandidateWalls();
            if (currentCount == _walls.Count) return;
        }

        _walls.Clear();

        if (onlyDirectChildren)
        {
            for (int i = 0; i < wallsRoot.childCount; i++)
            {
                var child = wallsRoot.GetChild(i);
                TryAddWall(child);
            }
        }
        else
        {
            // quét sâu toàn bộ descendants của Walls
            foreach (Transform t in wallsRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t == wallsRoot) continue;
                // chỉ lấy Wall_*
                TryAddWall(t);
            }
        }
    }

    private int CountCandidateWalls()
    {
        if (wallsRoot == null) return 0;

        int count = 0;

        if (onlyDirectChildren)
        {
            for (int i = 0; i < wallsRoot.childCount; i++)
            {
                var child = wallsRoot.GetChild(i);
                if (child != null && child.name.StartsWith("Wall_")) count++;
            }
        }
        else
        {
            foreach (Transform t in wallsRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t == wallsRoot) continue;
                if (t != null && t.name.StartsWith("Wall_")) count++;
            }
        }

        return count;
    }

    private void TryAddWall(Transform wallTf)
    {
        if (wallTf == null) return;
        if (!wallTf.name.StartsWith("Wall_")) return;

        // lấy toàn bộ renderer trong wall (cả con) để bật/tắt đồng bộ
        var rends = wallTf.GetComponentsInChildren<Renderer>(true);
        if (rends == null || rends.Length == 0) return;

        _walls.Add(new WallEntry
        {
            wallTf = wallTf,
            renderers = rends
        });
    }

    /// <summary>
    /// Tính normal hướng ra ngoài cho tường dựa trên vị trí hiện tại
    /// Gọi mỗi frame để luôn đúng khi room thay đổi kích thước
    /// </summary>
    private Vector3 CalculateWallNormal(Transform wallTf)
    {
        if (roomCenter == null || wallTf == null) return Vector3.forward;

        Vector3 wallPos = wallTf.position;
        Vector3 centerPos = roomCenter.position;

        if (ignoreY)
        {
            wallPos.y = 0f;
            centerPos.y = 0f;
        }

        // Hướng từ center ra wall = hướng ra ngoài phòng
        Vector3 centerToWall = wallPos - centerPos;
        if (centerToWall.sqrMagnitude < 0.0001f) return Vector3.forward;

        return centerToWall.normalized;
    }

    private void ApplyHideLogic()
    {
        if (cam == null || roomCenter == null) return;
        if (_walls.Count == 0) return;

        // Lấy vị trí center và camera (flatten Y nếu cần)
        Vector3 center = roomCenter.position;
        Vector3 camPos = cam.transform.position;
        
        if (ignoreY)
        {
            center.y = 0f;
            camPos.y = 0f;
        }

        // Hướng từ center đến camera
        Vector3 centerToCam = camPos - center;
        if (centerToCam.sqrMagnitude < 0.0001f) return;
        centerToCam.Normalize();

        if (debugDraw)
            Debug.DrawRay(center, centerToCam * 2f, Color.green);

        for (int i = 0; i < _walls.Count; i++)
        {
            var w = _walls[i];
            if (w.wallTf == null) continue;

            // Tính normal realtime (cập nhật khi room thay đổi kích thước)
            Vector3 n = CalculateWallNormal(w.wallTf);

            // So sánh hướng centerToCam với normal của tường
            // Nếu dot > 0 => camera và tường cùng phía (tường ở giữa camera và center)
            // => tường chắn tầm nhìn => cần ẩn
            float d = Vector3.Dot(centerToCam, n);

            // Ẩn tường nếu camera nhìn qua tường vào center
            bool hide = hideWallsFacingCamera ? (d > hideDotThreshold) : (d < -hideDotThreshold);

            SetWallRenderersEnabled(w.renderers, enabled: !hide);

            if (debugDraw)
                Debug.DrawRay(w.wallTf.position, n * 1.2f, hide ? Color.red : Color.cyan);
        }
    }

    private void SetWallRenderersEnabled(Renderer[] rends, bool enabled)
    {
        if (rends == null) return;
        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i];
            if (r == null) continue;
            r.enabled = enabled;
        }
    }
}
