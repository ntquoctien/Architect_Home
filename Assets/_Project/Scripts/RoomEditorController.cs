using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates the runtime room editor.
/// Subscribes to WallInteraction events, mutates RoomData, and keeps NodeHandles in sync.
/// Also exposes runtime dimension setters (SetWidth / SetLength / SetHeight).
/// </summary>
[RequireComponent(typeof(RoomMeshGenerator))]
[RequireComponent(typeof(RoomMeasurementView))]
[RequireComponent(typeof(WallInteraction))]
public class RoomEditorController : MonoBehaviour
{
    [Header("Initial Room Settings")]
    [SerializeField] private float initialWidth  = 5f;
    [SerializeField] private float initialLength = 5f;

    [Header("Node Settings")]
    [SerializeField] private GameObject nodeHandlePrefab;
    [SerializeField] private float nodeHandleSize = 0.3f;
    [SerializeField] private Color nodeNormalColor   = Color.green;
    [SerializeField] private Color nodeHoverColor    = Color.yellow;
    [SerializeField] private Color nodeSelectedColor = Color.cyan;

    [Header("UI")]
    [SerializeField] private RadialToolMenu radialToolMenu;

    [Header("Drag")]
    [SerializeField] private LayerMask groundPlaneLayer;

    [Header("Integration")]
    [SerializeField] private RoomThemeController roomThemeController;
    [SerializeField] private CameraController    cameraController;

    [Header("Camera Auto Setup")]
    [SerializeField] private bool autoCreateCamera = true;
    [SerializeField] private string cameraPivotName = "RoomCameraPivot";

    // ── Core references ────────────────────────────────────────────────────
    private RoomData            roomData;
    private RoomMeshGenerator   meshGenerator;
    private RoomMeasurementView measurementView;
    private WallInteraction     wallInteraction;

    // ── NodeHandle pool ────────────────────────────────────────────────────
    private List<NodeHandle> nodeHandles = new List<NodeHandle>();
    private NodeHandle selectedNode;
    private int  selectedNodeIndex = -1;
    private bool isDraggingNode    = false;

    // ── State ──────────────────────────────────────────────────────────────
    private bool isInitialized = false;
    private Camera mainCamera;
    private Transform cameraPivot;

    // ─────────────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        mainCamera      = Camera.main;
        meshGenerator   = GetComponent<RoomMeshGenerator>();
        measurementView = GetComponent<RoomMeasurementView>();
        wallInteraction = GetComponent<WallInteraction>();

        if (roomThemeController == null)
            roomThemeController = FindAnyObjectByType<RoomThemeController>();
        if (cameraController == null)
            cameraController = FindAnyObjectByType<CameraController>();

        // Subscribe to WallInteraction events (E1)
        wallInteraction.OnVertexClicked      += OnVertexClicked;
        wallInteraction.OnVertexRightClicked += OnVertexRightClicked;
        wallInteraction.OnEdgeClicked        += OnEdgeClicked;
    }

    private void Start()
    {
        if (!isInitialized)
            InitializeRoom(initialWidth, initialLength);  // E7
    }

    private void Update()
    {
        if (isDraggingNode && selectedNode != null)
            HandleNodeDrag();

        if (Input.GetMouseButtonUp(0))
            EndDrag();
    }

    private void OnDestroy()
    {
        if (roomData != null)
            roomData.OnGeometryChanged -= OnRoomGeometryChanged;

        if (wallInteraction != null)
        {
            wallInteraction.OnVertexClicked      -= OnVertexClicked;
            wallInteraction.OnVertexRightClicked -= OnVertexRightClicked;
            wallInteraction.OnEdgeClicked        -= OnEdgeClicked;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────

    public RoomData GetRoomData() => roomData;

    /// <summary>Re-initializes from external room dimensions (e.g. transitioning from static prefab).</summary>
    public void InitializeFromExisting(float width, float length)
    {
        CleanupExistingState();
        InitializeRoom(width, length);
    }

    // ── Runtime dimension controls (E8, E9) ─────────────────────────────

    /// <summary>
    /// Scales all corner X-coordinates proportionally to achieve the new width.
    /// Existing polygon shape is preserved; only the X extents change.
    /// </summary>
    public void SetWidth(float newWidth)
    {
        if (roomData == null || newWidth <= 0f) return;
        ScaleDimension(newWidth, axis: 0); // X
    }

    /// <summary>
    /// Scales all corner Z-coordinates proportionally to achieve the new length.
    /// </summary>
    public void SetLength(float newLength)
    {
        if (roomData == null || newLength <= 0f) return;
        ScaleDimension(newLength, axis: 2); // Z
    }

    /// <summary>Delegates to RoomData.SetRoomHeight — no corner repositioning needed.</summary>
    public void SetHeight(float newHeight)
    {
        if (roomData == null || newHeight <= 0f) return;
        roomData.SetRoomHeight(newHeight);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Initialization
    // ─────────────────────────────────────────────────────────────────────

    private void InitializeRoom(float width, float length)
    {
        roomData = new RoomData(width, length, centerPosition: transform.position);

        measurementView.Initialize(roomData);
        meshGenerator.Initialize(roomData);
        roomData.OnGeometryChanged += OnRoomGeometryChanged;
        EnsureCameraSetup();
        UpdateCameraPivot();
        IntegrateWithExistingSystems();

        CreateAllNodeHandles();
        isInitialized = true;
    }

    private void CleanupExistingState()
    {
        if (roomData != null)
            roomData.OnGeometryChanged -= OnRoomGeometryChanged;

        foreach (var h in nodeHandles)
            if (h != null) Destroy(h.gameObject);
        nodeHandles.Clear();

        selectedNode      = null;
        selectedNodeIndex = -1;
        isDraggingNode    = false;
        isInitialized     = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  WallInteraction event handlers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>E2 — Begin dragging the clicked vertex.</summary>
    private void OnVertexClicked(int index)
    {
        if (index < 0 || index >= nodeHandles.Count) return;
        selectedNode      = nodeHandles[index];
        selectedNodeIndex = index;
        isDraggingNode    = true;
        if (cameraController != null)
            cameraController.SetOrbitInputBlocked(true);
        selectedNode.SetState(NodeHandle.NodeState.Selected);
    }

    /// <summary>E4 — Show the RadialToolMenu at the right-clicked vertex.</summary>
    private void OnVertexRightClicked(int index)
    {
        if (roomData == null || index < 0 || index >= roomData.Corners.Count) return;
        if (radialToolMenu != null)
            radialToolMenu.Show(roomData.Corners[index], index);
    }

    /// <summary>E5 — Insert a new vertex on the clicked edge.</summary>
    private void OnEdgeClicked(int edgeIndex, Vector3 hitPoint)
    {
        hitPoint.y = 0f;
        roomData.InsertCorner(edgeIndex + 1, hitPoint);
        SyncNodeHandles();  // E6
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Dragging (E3)
    // ─────────────────────────────────────────────────────────────────────

    private void HandleNodeDrag()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);

        if (ground.Raycast(ray, out float dist))
        {
            Vector3 worldPos = ray.GetPoint(dist);
            if (roomData.MoveCorner(selectedNodeIndex, worldPos))
                selectedNode.transform.position = roomData.Corners[selectedNodeIndex];
        }
    }

    private void EndDrag()
    {
        if (isDraggingNode && selectedNode != null)
        {
            selectedNode.SetState(NodeHandle.NodeState.Normal);
            selectedNode = null;
            selectedNodeIndex = -1;
        }
        isDraggingNode = false;
        if (cameraController != null)
            cameraController.SetOrbitInputBlocked(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Geometry changed callback
    // ─────────────────────────────────────────────────────────────────────

    private void OnRoomGeometryChanged()
    {
        UpdateCameraPivot();
        IntegrateWithExistingSystems();
    }

    private void EnsureCameraSetup()
    {
        if (cameraPivot == null)
        {
            Transform existingPivot = transform.Find(cameraPivotName);
            if (existingPivot != null)
            {
                cameraPivot = existingPivot;
            }
            else
            {
                GameObject pivotGo = new GameObject(cameraPivotName);
                pivotGo.transform.SetParent(transform);
                pivotGo.transform.localPosition = Vector3.zero;
                cameraPivot = pivotGo.transform;
            }
        }

        if (cameraController == null)
            cameraController = FindAnyObjectByType<CameraController>();

        if (cameraController == null && autoCreateCamera)
        {
            Camera sceneCamera = Camera.main;
            GameObject cameraGo = sceneCamera != null ? sceneCamera.gameObject : new GameObject("Main Camera");

            if (sceneCamera == null)
                sceneCamera = cameraGo.AddComponent<Camera>();

            if (cameraGo.GetComponent<AudioListener>() == null)
                cameraGo.AddComponent<AudioListener>();

            cameraGo.tag = "MainCamera";
            cameraController = cameraGo.GetComponent<CameraController>() ?? cameraGo.AddComponent<CameraController>();
        }

        if (cameraController == null) return;

        cameraController.enabled = true;
        cameraController.target = cameraPivot;
        cameraController.floorRenderer = meshGenerator.GetFloorRenderer();
        cameraController.SetOrbitInputBlocked(isDraggingNode);
        cameraController.RebuildBoundsFromFloor(true);
        mainCamera = Camera.main;
    }

    private void UpdateCameraPivot()
    {
        if (cameraPivot == null || roomData == null || roomData.Corners.Count == 0) return;

        Vector3 min = roomData.Corners[0];
        Vector3 max = roomData.Corners[0];
        for (int i = 1; i < roomData.Corners.Count; i++)
        {
            min = Vector3.Min(min, roomData.Corners[i]);
            max = Vector3.Max(max, roomData.Corners[i]);
        }

        Vector3 center = (min + max) * 0.5f;
        cameraPivot.position = new Vector3(center.x, transform.position.y, center.z);
    }

    private void IntegrateWithExistingSystems()
    {
        if (roomThemeController != null)
        {
            Renderer floorR = meshGenerator.GetFloorRenderer();
            if (floorR != null)
            {
                // SetProceduralRenderers: sets both refs, marks _proceduralRenderersBound=true
                // so AutoFindIfNeeded() is bypassed, then calls ApplyTheme().
                roomThemeController.SetProceduralRenderers(
                    floorR,
                    meshGenerator.GetWallRenderers().ToArray()
                );
            }
        }

        if (cameraController != null)
        {
            Renderer floorR = meshGenerator.GetFloorRenderer();
            if (floorR != null)
                cameraController.floorRenderer = floorR;
            cameraController.RebuildBoundsFromFloor(false);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  NodeHandle management (E6)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sync NodeHandle list to current RoomData.Corners without full teardown.
    /// Creates new handles for new corners, repositions existing ones, deactivates surplus.
    /// </summary>
    private void SyncNodeHandles()
    {
        int cornerCount = roomData.Corners.Count;

        // Grow pool if needed
        while (nodeHandles.Count < cornerCount)
        {
            nodeHandles.Add(null);
        }

        for (int i = 0; i < nodeHandles.Count; i++)
        {
            if (i < cornerCount)
            {
                if (nodeHandles[i] == null)
                {
                    nodeHandles[i] = SpawnNodeHandle(i);
                }
                else
                {
                    // Reactivate and reposition
                    nodeHandles[i].gameObject.SetActive(true);
                    nodeHandles[i].transform.position = roomData.Corners[i];
                    SetHandleWorldSize(nodeHandles[i].transform);
                    nodeHandles[i].SetNodeIndex(i);
                }
            }
            else
            {
                // Deactivate surplus handles
                if (nodeHandles[i] != null)
                    nodeHandles[i].gameObject.SetActive(false);
            }
        }

        // Tell WallInteraction about the updated handle list
        wallInteraction.SetNodeHandles(GetActiveHandles());
    }

    private void CreateAllNodeHandles()
    {
        // Full creation from scratch (used only at init)
        foreach (var h in nodeHandles)
            if (h != null) Destroy(h.gameObject);
        nodeHandles.Clear();

        for (int i = 0; i < roomData.Corners.Count; i++)
            nodeHandles.Add(SpawnNodeHandle(i));

        wallInteraction.SetNodeHandles(GetActiveHandles());
    }

    private NodeHandle SpawnNodeHandle(int index)
    {
        GameObject go;
        if (nodeHandlePrefab != null)
        {
            go = Instantiate(nodeHandlePrefab, roomData.Corners[index], Quaternion.identity, transform);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(transform);
            go.transform.position   = roomData.Corners[index];
        }
        go.name = $"NodeHandle_{index}";
        SetHandleWorldSize(go.transform);

        NodeHandle handle = go.GetComponent<NodeHandle>() ?? go.AddComponent<NodeHandle>();
        handle.Initialize(index, nodeNormalColor, nodeHoverColor, nodeSelectedColor);

        // Wire deletion (only from RadialMenu now, but kept for safety)
        int capturedIndex = index;
        handle.OnNodeDeleted += () => OnNodeDeleted(capturedIndex);

        return handle;
    }

    private void SetHandleWorldSize(Transform handleTransform)
    {
        float size = Mathf.Max(0.01f, nodeHandleSize);
        Transform parent = handleTransform.parent;
        if (parent == null)
        {
            handleTransform.localScale = Vector3.one * size;
            return;
        }

        Vector3 parentScale = parent.lossyScale;
        handleTransform.localScale = new Vector3(
            SafeScale(size, parentScale.x),
            SafeScale(size, parentScale.y),
            SafeScale(size, parentScale.z)
        );
    }

    private static float SafeScale(float worldSize, float parentAxisScale)
    {
        float axis = Mathf.Abs(parentAxisScale);
        return axis > 0.0001f ? worldSize / axis : worldSize;
    }

    private List<NodeHandle> GetActiveHandles()
    {
        var active = new List<NodeHandle>();
        foreach (var h in nodeHandles)
            if (h != null && h.gameObject.activeSelf) active.Add(h);
        return active;
    }

    private void OnNodeDeleted(int nodeIndex)
    {
        if (roomData.RemoveCorner(nodeIndex))
            SyncNodeHandles();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Dimension scaling helpers (E8)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Scales all corners along a single axis (0=X, 2=Z) proportionally from the polygon centre.
    /// </summary>
    private void ScaleDimension(float targetSize, int axis)
    {
        var corners = roomData.Corners;
        if (corners.Count == 0) return;

        // Compute current extent along axis
        float min = float.MaxValue, max = float.MinValue;
        foreach (var c in corners)
        {
            float v = axis == 0 ? c.x : c.z;
            if (v < min) min = v;
            if (v > max) max = v;
        }
        float current = max - min;
        if (Mathf.Abs(current) < 0.001f) return;

        float scale  = targetSize / current;
        float center = (min + max) * 0.5f;

        for (int i = 0; i < corners.Count; i++)
        {
            Vector3 c = corners[i];
            if (axis == 0) c.x = center + (c.x - center) * scale;
            else           c.z = center + (c.z - center) * scale;
            roomData.MoveCorner(i, c);
        }
        SyncNodeHandles();
    }
}
