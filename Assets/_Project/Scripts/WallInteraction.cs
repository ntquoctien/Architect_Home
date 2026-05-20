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
    /// <summary>Fired on Shift + left-click on a NodeHandle vertex to create a new wall by dragging.</summary>
    public event Action<int> OnVertexShiftClicked;
    /// <summary>Fired on right-click on a NodeHandle vertex.</summary>
    public event Action<int> OnVertexRightClicked;

    // ── Internal state ──────────────────────────────────────────────────────
    private List<NodeHandle> nodeHandles = new List<NodeHandle>();
    private int lastHoveredIndex = -1;
    private Camera mainCamera;

    private void Awake()
    {
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
        if (TryRaycastNodeHandle(ray, out NodeHandle handle))
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

            if (Input.GetMouseButtonDown(0))
            {
                if (IsShiftHeld())
                    OnVertexShiftClicked?.Invoke(idx);
                else
                    OnVertexClicked?.Invoke(idx);
            }

            // Right-click
            if (Input.GetMouseButtonDown(1))
                OnVertexRightClicked?.Invoke(idx);

            return; // Don't fall through to edge check
        }

        // No NodeHandle hit — clear hover
        ClearHover();

        // ── 2. Try to hit a wall BoxCollider ──
        // Wall creation is handled by Shift + dragging from a NodeHandle.
    }

    private bool TryRaycastNodeHandle(Ray ray, out NodeHandle handle)
    {
        handle = null;
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, nodeHandleLayer);
        if (hits == null || hits.Length == 0) return false;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            NodeHandle candidate = hits[i].collider.GetComponent<NodeHandle>();
            if (candidate == null)
                candidate = hits[i].collider.GetComponentInParent<NodeHandle>();
            if (candidate == null || nodeHandles == null || !nodeHandles.Contains(candidate)) continue;

            handle = candidate;
            return true;
        }

        return false;
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

    private static bool IsShiftHeld()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }
}
