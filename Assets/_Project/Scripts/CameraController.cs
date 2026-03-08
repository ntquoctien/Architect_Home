using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Decor Orbit Camera (WASD rotate around room)
/// - Camera luôn nhìn về Room center (target = Room/Pivot)
/// - WASD: rotate (A/D yaw, W/S pitch)
/// - Scroll: zoom
/// - Tự giữ khoảng cách an toàn theo kích thước phòng + pitch (không bao giờ chui vào phòng)
/// - Collision chống clip (SphereCast)
/// - Pivot chỉ cần update khi floor thay đổi; gọi RebuildBoundsFromFloor()
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Room Center (Target)")]
    public Transform target;              // Room/Pivot
    public Renderer floorRenderer;        // Room/Floor renderer
    public float lookHeight = 1.2f;

    [Header("Rotate (WASD)")]
    public float yawSpeed = 110f;         // degrees/second
    public float pitchSpeed = 90f;        // degrees/second
    public float minPitch = 15f;
    public float maxPitch = 75f;

    [Header("Mouse Orbit (optional)")]
    public bool enableRmbOrbit = true;
    public float mouseSensitivity = 3f;

    [Header("Zoom (Scroll)")]
    public float zoomSpeed = 12f;
    public float minDistance = 3f;
    public float maxDistance = 40f;
    public float zoomSmoothTime = 0.08f;

    [Header("Safe Distance (anti-enter room)")]
    [Tooltip("Khoảng đệm ngoài phòng (m). Tăng nếu camera hay sát tường.")]
    public float outsidePadding = 0.8f;

    [Tooltip("Thêm đệm theo wall thickness (nếu bạn có).")]
    public float wallThicknessPadding = 0.1f;

    [Header("Smoothing")]
    public bool smoothRotation = true;
    public float rotationSmoothTime = 0.06f;
    public float positionSmoothTime = 0.06f;

    [Header("Input blocking")]
    public bool blockWhenPointerOverUI = true;
    public bool blockWhenPlacing = true;
    public FurniturePlacer furniturePlacer; // optional

    [Header("Wall Editor Integration")]
    [Tooltip("Tham chiếu WallEditor để đăng ký callback khi tường thay đổi")]
    public WallEditor wallEditor;

    [Header("Collision (avoid clipping)")]
    public LayerMask collisionMask; // tick Wall
    public float collisionRadius = 0.25f;
    public float collisionPadding = 0.2f;

    [Header("Auto Spawn")]
    public bool autoSnapOnStart = true;
    public float defaultYaw = 45f;
    public float defaultPitch = 35f;
    public float spawnDistanceFactor = 1.8f;

    // ===== internal =====
    private float targetYaw, targetPitch;
    private float currentYaw, currentPitch;
    private float yawVel, pitchVel;

    private float targetDistance, currentDistance;
    private float zoomVel;

    private Vector3 posVel;

    private Bounds floorBounds;
    private bool hasBounds;
    
    // Cache safe distance để tránh tính lại mỗi frame gây jitter
    private float cachedSafeDistance;
    private float lastPitchForSafe;
    
    // Cache để detect floor bounds thay đổi
    private Vector3 lastFloorScale;
    private float boundsCheckTimer;
    private const float BOUNDS_CHECK_INTERVAL = 0.2f; // check mỗi 0.2s

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraController: thiếu target (Room/Pivot).");
            enabled = false;
            return;
        }

        InitRoomBounds();
        if (floorRenderer != null) lastFloorScale = floorRenderer.transform.localScale;

        // Đăng ký callback khi tường thay đổi
        RegisterWallEditorCallback();

        if (autoSnapOnStart) SnapToDefaultView();
        else InitFromCurrentTransform();

        UpdateCameraTransform(immediate: true);
    }
    
    void OnDestroy()
    {
        // Hủy đăng ký callback
        UnregisterWallEditorCallback();
    }
    
    private void RegisterWallEditorCallback()
    {
        if (wallEditor != null)
        {
            wallEditor.onWallsChanged += OnWallsChanged;
        }
    }
    
    private void UnregisterWallEditorCallback()
    {
        if (wallEditor != null)
        {
            wallEditor.onWallsChanged -= OnWallsChanged;
        }
    }
    
    /// <summary>
    /// Được gọi khi tường thay đổi (thêm/xóa)
    /// </summary>
    private void OnWallsChanged()
    {
        // Reset cache để tính lại safe distance
        cachedSafeDistance = 0f;
        lastPitchForSafe = -999f;
        
        // Cập nhật bounds
        InitRoomBounds();
    }

    void Update()
    {
        if (ShouldBlockInput()) return;

        HandleKeyboardRotate();
        if (enableRmbOrbit) HandleMouseOrbit();
        HandleZoom();
    }

    void LateUpdate()
    {
        // Kiểm tra nếu floor đã thay đổi kích thước
        CheckFloorBoundsChanged();
        
        UpdateCameraTransform(immediate: false);
    }
    
    /// <summary>
    /// Kiểm tra và cập nhật bounds nếu floor thay đổi kích thước
    /// </summary>
    private void CheckFloorBoundsChanged()
    {
        boundsCheckTimer += Time.deltaTime;
        if (boundsCheckTimer < BOUNDS_CHECK_INTERVAL) return;
        boundsCheckTimer = 0f;
        
        if (floorRenderer == null) return;
        
        Vector3 currentScale = floorRenderer.transform.localScale;
        if (currentScale != lastFloorScale)
        {
            lastFloorScale = currentScale;
            InitRoomBounds();
            
            // Reset cached safe distance để tính lại
            cachedSafeDistance = 0f;
            lastPitchForSafe = -999f;
        }
    }

    private bool ShouldBlockInput()
    {
        if (blockWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return true;

        if (blockWhenPlacing && furniturePlacer != null && furniturePlacer.IsPlacing)
            return true;

        return false;
    }

    // ===== Public API =====
    public void RebuildBoundsFromFloor(bool resnap = false)
    {
        InitRoomBounds();
        if (resnap) SnapToDefaultView();
    }

    [ContextMenu("Snap To Default View")]
    public void SnapToDefaultView()
    {
        targetYaw = currentYaw = defaultYaw;
        targetPitch = currentPitch = Mathf.Clamp(defaultPitch, minPitch, maxPitch);

        float suggested = 10f;
        if (hasBounds)
        {
            float r = GetHalfDiagonalXZ(floorBounds);
            suggested = Mathf.Max(minDistance, r * spawnDistanceFactor);
        }

        targetDistance = currentDistance = Mathf.Clamp(suggested, minDistance, maxDistance);

        // Enforce safe distance ngay từ đầu (tránh spawn trong room)
        cachedSafeDistance = CalculateSafeDistance(currentPitch);
        lastPitchForSafe = currentPitch;
        targetDistance = currentDistance = Mathf.Max(targetDistance, cachedSafeDistance);

        // Set ngay position
        Vector3 lookPos = GetLookPos();
        Quaternion rot = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 desired = lookPos + rot * new Vector3(0f, 0f, -currentDistance);

        desired = ResolveCollision(lookPos, desired);

        transform.position = desired;
        transform.LookAt(lookPos);
    }

    // ===== Input =====
    private void HandleKeyboardRotate()
    {
        float yawDir = 0f;
        if (Input.GetKey(KeyCode.A)) yawDir -= 1f;
        if (Input.GetKey(KeyCode.D)) yawDir += 1f;

        float pitchDir = 0f;
        if (Input.GetKey(KeyCode.W)) pitchDir += 1f; // lên cao hơn (nhìn từ trên xuống)
        if (Input.GetKey(KeyCode.S)) pitchDir -= 1f; // xuống thấp (nhìn ngang hơn)

        if (Mathf.Abs(yawDir) > 0.01f)
            targetYaw += yawDir * yawSpeed * Time.deltaTime;

        if (Mathf.Abs(pitchDir) > 0.01f)
        {
            targetPitch += pitchDir * pitchSpeed * Time.deltaTime;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        }
    }

    private void HandleMouseOrbit()
    {
        if (!Input.GetMouseButton(1)) return;

        float dx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float dy = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (Mathf.Abs(dx) < 0.0005f) dx = 0f;
        if (Mathf.Abs(dy) < 0.0005f) dy = 0f;
        if (dx == 0f && dy == 0f) return;

        targetYaw += dx;
        targetPitch -= dy;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f) return;

        targetDistance -= scroll * zoomSpeed;
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
    }

    // ===== Update =====
    private void UpdateCameraTransform(bool immediate)
    {
        // Smooth rotation
        if (immediate || !smoothRotation)
        {
            currentYaw = targetYaw;
            currentPitch = targetPitch;
        }
        else
        {
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVel, rotationSmoothTime);
            currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVel, rotationSmoothTime);
        }

        // Smooth zoom
        if (immediate)
            currentDistance = targetDistance;
        else
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVel, zoomSmoothTime);

        // Chỉ tính lại safe distance khi pitch thay đổi đáng kể (tránh jitter)
        if (Mathf.Abs(currentPitch - lastPitchForSafe) > 0.5f || cachedSafeDistance < minDistance)
        {
            cachedSafeDistance = CalculateSafeDistance(currentPitch);
            lastPitchForSafe = currentPitch;
        }

        // Enforce khoảng cách tối thiểu (đơn giản, không có hysteresis phức tạp)
        float finalDistance = Mathf.Max(currentDistance, cachedSafeDistance);

        Vector3 lookPos = GetLookPos();
        Quaternion rot = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 desiredPos = lookPos + rot * new Vector3(0f, 0f, -finalDistance);

        // Smooth position - dùng smoothing mạnh hơn để tránh giật
        if (immediate)
        {
            transform.position = desiredPos;
        }
        else
        {
            // Dùng Lerp thay vì SmoothDamp để ổn định hơn
            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime / positionSmoothTime * 0.5f);
        }

        transform.LookAt(lookPos);
    }

    private Vector3 GetLookPos()
    {
        return target.position + Vector3.up * lookHeight;
    }

    private Vector3 ResolveCollision(Vector3 lookPos, Vector3 desiredPos)
    {
        if (collisionMask.value == 0) return desiredPos;

        Vector3 dirToCam = desiredPos - lookPos;
        float dist = dirToCam.magnitude;
        if (dist < 0.001f) return desiredPos;

        Vector3 dir = dirToCam / dist;

        if (Physics.SphereCast(lookPos, collisionRadius, dir, out RaycastHit hit, dist, collisionMask))
        {
            float newDist = Mathf.Max(hit.distance - collisionPadding, minDistance * 0.5f);
            return lookPos + dir * newDist;
        }

        return desiredPos;
    }

    // ===== Safe Distance Core =====
    private float CalculateSafeDistance(float pitchDeg)
    {
        if (!hasBounds) return minDistance;

        // Bán kính an toàn trong mặt phẳng XZ: half diagonal + padding
        float r = GetHalfDiagonalXZ(floorBounds);
        float pad = outsidePadding + wallThicknessPadding;

        float requiredHorizontal = r + pad;

        float pitchRad = pitchDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(pitchRad);

        // Tránh chia 0 khi pitch gần 90
        cos = Mathf.Max(0.2f, cos);

        // horizontalDistance = dist * cos(pitch)
        float minDistByRoom = requiredHorizontal / cos;

        return Mathf.Clamp(minDistByRoom, minDistance, maxDistance);
    }

    private float GetHalfDiagonalXZ(Bounds b)
    {
        float ex = b.extents.x;
        float ez = b.extents.z;
        return Mathf.Sqrt(ex * ex + ez * ez);
    }

    // ===== Bounds =====
    private void InitRoomBounds()
    {
        hasBounds = false;

        if (floorRenderer == null)
        {
            Transform room = target != null ? target.parent : null;
            Transform f = room != null ? room.Find("Floor") : null;
            if (f != null) floorRenderer = f.GetComponentInChildren<Renderer>(true);
        }

        if (floorRenderer != null)
        {
            floorBounds = floorRenderer.bounds;
            hasBounds = true;
        }
    }

    private void InitFromCurrentTransform()
    {
        Vector3 lookPos = GetLookPos();
        Vector3 toCam = transform.position - lookPos;

        float d = Mathf.Max(toCam.magnitude, 0.001f);
        targetDistance = currentDistance = Mathf.Clamp(d, minDistance, maxDistance);

        Vector3 flat = new Vector3(toCam.x, 0f, toCam.z);
        float yaw = (flat.sqrMagnitude > 0.0001f) ? Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg : defaultYaw;

        Vector3 dir = toCam.normalized;
        float pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        targetYaw = currentYaw = yaw;
        targetPitch = currentPitch = pitch;

        // enforce safe distance ngay
        cachedSafeDistance = CalculateSafeDistance(targetPitch);
        lastPitchForSafe = targetPitch;
        targetDistance = currentDistance = Mathf.Max(targetDistance, cachedSafeDistance);
    }
}
