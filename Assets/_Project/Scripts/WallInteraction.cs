using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Dedicated input/raycasting system for the room editor.
/// Detects clicks on NodeHandle vertices (wall corners) and wall edge BoxColliders.
/// Fires strongly-typed events that RoomEditorController subscribes to.
/// Contains zero mesh or data logic.
/// </summary>
[RequireComponent(typeof(RoomMeshGenerator))]
public class WallInteraction : MonoBehaviour
{
    [Header("Layer Masks")]
    [Tooltip("Layer containing NodeHandle colliders")]
    [SerializeField] private LayerMask nodeHandleLayer = ~0;
    [Tooltip("Layer containing wall BoxColliders")]
    [SerializeField] private LayerMask wallLayer = ~0;

    [Header("Raycast Distance")]
    [SerializeField] private float maxRayDistance = 200f;

    // ── Events ──────────────────────────────────────────────────────────────
    /// <summary>Fired when the mouse moves over a NodeHandle vertex.</summary>
    public event Action<int> OnVertexHovered;
    /// <summary>Fired on left-click on a NodeHandle vertex.</summary>
    public event Action<int> OnVertexClicked;
    /// <summary>Fired on right-click on a NodeHandle vertex.</summary>
    public event Action<int> OnVertexRightClicked;
    /// <summary>Fired on left-click on a wall BoxCollider. Carries edge index and world hit point.</summary>
    public event Action<int, Vector3> OnEdgeClicked;

    // ── Internal state ──────────────────────────────────────────────────────
    private RoomMeshGenerator meshGenerator;
    private List<NodeHandle> nodeHandles = new List<NodeHandle>();
    private int lastHoveredIndex = -1;
    private Camera mainCamera;

    private void Awake()
    {
        meshGenerator = GetComponent<RoomMeshGenerator>();
        mainCamera    = Camera.main;
    }

    // ── Called by RoomEditorController whenever the handle list changes ─────
    public void SetNodeHandles(List<NodeHandle> handles)
    {
        nodeHandles = handles;
    }

    private void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Guard: skip raycasting when the cursor is over a UI element
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ClearHover();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // ── 1. Try to hit a NodeHandle ──
        if (Physics.Raycast(ray, out RaycastHit nodeHit, maxRayDistance, nodeHandleLayer))
        {
            NodeHandle handle = nodeHit.collider.GetComponent<NodeHandle>();
            if (handle != null && nodeHandles.Contains(handle))
            {
                int idx = handle.GetNodeIndex();

                // Hover management
                if (lastHoveredIndex != idx)
                {
                    ClearHover();
                    lastHoveredIndex = idx;
                    handle.SetState(NodeHandle.NodeState.Hover);
                    OnVertexHovered?.Invoke(idx);
                }

                // Left-click
                if (Input.GetMouseButtonDown(0))
                    OnVertexClicked?.Invoke(idx);

                // Right-click
                if (Input.GetMouseButtonDown(1))
                    OnVertexRightClicked?.Invoke(idx);

                return; // Don't fall through to edge check
            }
        }

        // No NodeHandle hit — clear hover
        ClearHover();

        // ── 2. Try to hit a wall BoxCollider ──
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out RaycastHit wallHit, maxRayDistance, wallLayer))
            {
                BoxCollider bc = wallHit.collider as BoxCollider;
                if (bc != null && meshGenerator != null)
                {
                    int edgeIdx = meshGenerator.GetEdgeIndexForCollider(bc);
                    if (edgeIdx >= 0)
                        OnEdgeClicked?.Invoke(edgeIdx, wallHit.point);
                }
            }
        }
    }

    private void ClearHover()
    {
        if (lastHoveredIndex >= 0 && nodeHandles != null && lastHoveredIndex < nodeHandles.Count)
        {
            var prev = nodeHandles[lastHoveredIndex];
            if (prev != null && prev.GetNodeIndex() == lastHoveredIndex)
                prev.SetState(NodeHandle.NodeState.Normal);
        }
        lastHoveredIndex = -1;
    }
}
