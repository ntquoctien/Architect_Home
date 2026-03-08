using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main controller for the runtime room editor.
/// Handles input, node management, and coordinates between data and visualization components.
/// </summary>
[RequireComponent(typeof(RoomMeshGenerator))]
[RequireComponent(typeof(RoomMeasurementView))]
public class RoomEditorController : MonoBehaviour
{
    [Header("Initial Room Settings")]
    [SerializeField] private float initialWidth = 5f;
    [SerializeField] private float initialLength = 5f;

    [Header("Node Settings")]
    [SerializeField] private GameObject nodeHandlePrefab;
    [SerializeField] private float nodeHandleSize = 0.3f;
    [SerializeField] private Color nodeNormalColor = Color.green;
    [SerializeField] private Color nodeHoverColor = Color.yellow;
    [SerializeField] private Color nodeSelectedColor = Color.cyan;

    [Header("Edge Interaction")]
    [SerializeField] private float edgeClickThreshold = 0.5f;
    [SerializeField] private LayerMask groundPlaneLayer;

    [Header("Integration")]
    [SerializeField] private RoomThemeController roomThemeController;
    [SerializeField] private CameraController cameraController;

    // Core Data
    private RoomData roomData;
    private RoomMeshGenerator meshGenerator;
    private RoomMeasurementView measurementView;

    // Node Handles
    private List<NodeHandle> nodeHandles = new List<NodeHandle>();
    private NodeHandle selectedNode;
    private bool isDraggingNode = false;

    // Initialization state
    private bool isInitialized = false;

    // Camera reference
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        meshGenerator = GetComponent<RoomMeshGenerator>();
        measurementView = GetComponent<RoomMeasurementView>();

        // Auto-find components if not assigned
        if (roomThemeController == null)
            roomThemeController = FindAnyObjectByType<RoomThemeController>();
        
        if (cameraController == null)
            cameraController = FindAnyObjectByType<CameraController>();
    }

    private void Start()
    {
        // Only auto-initialize if not externally initialized and autoInitialize is desired
        // For transition mode, call InitializeFromExisting() instead
        if (!isInitialized)
        {
            InitializeRoom(initialWidth, initialLength);
        }
    }

    /// <summary>
    /// Initializes the room editor from existing room dimensions.
    /// Used for transitioning from static RoomController to dynamic editing.
    /// </summary>
    /// <param name="width">Room width (X axis)</param>
    /// <param name="length">Room length (Z axis)</param>
    public void InitializeFromExisting(float width, float length)
    {
        // Clean up any existing state
        CleanupExistingState();
        
        // Initialize with provided dimensions
        InitializeRoom(width, length);
    }

    /// <summary>
    /// Core initialization logic for the room.
    /// </summary>
    private void InitializeRoom(float width, float length)
    {
        // Calculate corner positions centered at (0, 0, 0)
        float halfWidth = width * 0.5f;
        float halfLength = length * 0.5f;

        // Create room data with the specified dimensions
        roomData = new RoomData(width, length, transform.position);
        roomData.OnGeometryChanged += OnRoomGeometryChanged;

        // Set up measurement view
        measurementView.Initialize(roomData);

        // Create initial node handles
        CreateNodeHandles();

        // Generate initial mesh
        OnRoomGeometryChanged();

        isInitialized = true;
    }

    /// <summary>
    /// Cleans up existing room state before re-initialization.
    /// </summary>
    private void CleanupExistingState()
    {
        // Unsubscribe from old room data events
        if (roomData != null)
        {
            roomData.OnGeometryChanged -= OnRoomGeometryChanged;
        }

        // Destroy existing node handles
        foreach (var handle in nodeHandles)
        {
            if (handle != null)
                Destroy(handle.gameObject);
        }
        nodeHandles.Clear();

        // Reset state
        selectedNode = null;
        isDraggingNode = false;
        isInitialized = false;
    }

    private void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// Handles all user input for the room editor.
    /// </summary>
    private void HandleInput()
    {
        // Left click - Select/Drag nodes or split edges
        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }

        if (Input.GetMouseButton(0) && isDraggingNode && selectedNode != null)
        {
            HandleNodeDrag();
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDraggingNode = false;
            if (selectedNode != null)
            {
                selectedNode.SetState(NodeHandle.NodeState.Normal);
                selectedNode = null;
            }
        }
    }

    /// <summary>
    /// Handles left mouse click for node selection or edge splitting.
    /// </summary>
    private void HandleLeftClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        // First, check if we clicked on a node
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            NodeHandle node = hit.collider.GetComponent<NodeHandle>();
            if (node != null && nodeHandles.Contains(node))
            {
                selectedNode = node;
                isDraggingNode = true;
                selectedNode.SetState(NodeHandle.NodeState.Selected);
                return;
            }
        }

        // If not a node, check if we clicked on an edge to split it
        CheckEdgeSplit(ray);
    }

    /// <summary>
    /// Handles dragging a selected node.
    /// </summary>
    private void HandleNodeDrag()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        float distance;
        if (groundPlane.Raycast(ray, out distance))
        {
            Vector3 worldPosition = ray.GetPoint(distance);
            int nodeIndex = nodeHandles.IndexOf(selectedNode);
            
            if (nodeIndex >= 0)
            {
                roomData.MoveCorner(nodeIndex, worldPosition);
                selectedNode.transform.position = roomData.Corners[nodeIndex];
            }
        }
    }

    /// <summary>
    /// Checks if the user clicked near an edge to split it.
    /// </summary>
    private void CheckEdgeSplit(Ray ray)
    {
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        
        if (!groundPlane.Raycast(ray, out distance)) return;
        
        Vector3 clickPoint = ray.GetPoint(distance);

        // Check each edge
        for (int i = 0; i < roomData.Corners.Count; i++)
        {
            var (start, end) = roomData.GetEdge(i);
            
            // Calculate closest point on line segment
            Vector3 lineDir = end - start;
            float lineLength = lineDir.magnitude;
            lineDir.Normalize();
            
            Vector3 toClick = clickPoint - start;
            float projection = Vector3.Dot(toClick, lineDir);
            
            // Check if projection is within segment bounds
            if (projection >= 0 && projection <= lineLength)
            {
                Vector3 closestPoint = start + lineDir * projection;
                float distanceToLine = Vector3.Distance(clickPoint, closestPoint);
                
                if (distanceToLine < edgeClickThreshold)
                {
                    // Split the edge at this point
                    int insertIndex = (i + 1) % (roomData.Corners.Count + 1);
                    roomData.InsertCorner(insertIndex, closestPoint);
                    
                    // Recreate node handles
                    CreateNodeHandles();
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Called when room geometry changes to regenerate meshes.
    /// </summary>
    private void OnRoomGeometryChanged()
    {
        // Regenerate mesh
        meshGenerator.GenerateMeshes(roomData);

        // Integrate with existing systems
        IntegrateWithExistingSystems();
    }

    /// <summary>
    /// Integrates the newly generated geometry with RoomThemeController and CameraController.
    /// </summary>
    private void IntegrateWithExistingSystems()
    {
        // Register geometry with RoomThemeController
        if (roomThemeController != null)
        {
            Renderer floorRenderer = meshGenerator.GetFloorRenderer();
            List<Renderer> wallRenderers = meshGenerator.GetWallRenderers();
            
            if (floorRenderer != null)
            {
                // Update RoomThemeController's references
                roomThemeController.floorRenderer = floorRenderer;
                roomThemeController.wallRenderers = wallRenderers.ToArray();
                
                // Apply the theme to the new geometry
                roomThemeController.ApplyTheme();
            }
        }

        // Update camera bounds
        if (cameraController != null)
        {
            // Update camera's floor renderer reference
            Renderer floorRenderer = meshGenerator.GetFloorRenderer();
            if (floorRenderer != null)
            {
                cameraController.floorRenderer = floorRenderer;
            }
            
            cameraController.RebuildBoundsFromFloor(false);
        }
    }

    /// <summary>
    /// Creates or recreates node handles based on current room data.
    /// </summary>
    private void CreateNodeHandles()
    {
        // Clear existing handles
        foreach (var handle in nodeHandles)
        {
            if (handle != null)
                Destroy(handle.gameObject);
        }
        nodeHandles.Clear();

        // Create new handles
        if (nodeHandlePrefab == null)
        {
            // Create default sphere handles if no prefab assigned
            CreateDefaultNodeHandles();
        }
        else
        {
            CreatePrefabNodeHandles();
        }

        // Set up node callbacks
        for (int i = 0; i < nodeHandles.Count; i++)
        {
            int index = i; // Capture for closure
            nodeHandles[i].OnNodeDeleted += () => OnNodeDeleted(index);
        }
    }

    /// <summary>
    /// Creates default sphere node handles.
    /// </summary>
    private void CreateDefaultNodeHandles()
    {
        for (int i = 0; i < roomData.Corners.Count; i++)
        {
            GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nodeObj.name = $"NodeHandle_{i}";
            nodeObj.transform.position = roomData.Corners[i];
            nodeObj.transform.localScale = Vector3.one * nodeHandleSize;
            nodeObj.transform.SetParent(transform);

            NodeHandle handle = nodeObj.AddComponent<NodeHandle>();
            handle.Initialize(i, nodeNormalColor, nodeHoverColor, nodeSelectedColor);
            nodeHandles.Add(handle);
        }
    }

    /// <summary>
    /// Creates node handles from prefab.
    /// </summary>
    private void CreatePrefabNodeHandles()
    {
        for (int i = 0; i < roomData.Corners.Count; i++)
        {
            GameObject nodeObj = Instantiate(nodeHandlePrefab, roomData.Corners[i], Quaternion.identity, transform);
            nodeObj.name = $"NodeHandle_{i}";

            NodeHandle handle = nodeObj.GetComponent<NodeHandle>();
            if (handle == null)
                handle = nodeObj.AddComponent<NodeHandle>();
            
            handle.Initialize(i, nodeNormalColor, nodeHoverColor, nodeSelectedColor);
            nodeHandles.Add(handle);
        }
    }

    /// <summary>
    /// Called when a node is deleted.
    /// </summary>
    private void OnNodeDeleted(int nodeIndex)
    {
        if (roomData.RemoveCorner(nodeIndex))
        {
            CreateNodeHandles();
        }
    }

    /// <summary>
    /// Public API: Get the room data.
    /// </summary>
    public RoomData GetRoomData()
    {
        return roomData;
    }

    private void OnDestroy()
    {
        if (roomData != null)
        {
            roomData.OnGeometryChanged -= OnRoomGeometryChanged;
        }
    }
}
