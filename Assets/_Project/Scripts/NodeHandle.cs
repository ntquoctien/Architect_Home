using System;
using UnityEngine;

/// <summary>
/// Represents a draggable corner node in the room editor.
/// Handles visual feedback and user interactions (hover, drag, delete).
/// </summary>
[RequireComponent(typeof(Collider))]
public class NodeHandle : MonoBehaviour
{
    public enum NodeState
    {
        Normal,
        Hover,
        Selected
    }

    public event Action OnNodeDeleted;
    /// <summary>
    /// Fired when the player right-clicks this NodeHandle (= wall corner vertex).
    /// RoomEditorController subscribes to open the RadialToolMenu.
    /// </summary>
    public event Action<int> OnNodeRightClicked;

    private int nodeIndex;
    private NodeState currentState = NodeState.Normal;
    
    private Color normalColor;
    private Color hoverColor;
    private Color selectedColor;
    
    private Renderer nodeRenderer;
    private MaterialPropertyBlock propertyBlock;

    /// <summary>
    /// Initializes the node handle with colors and index.
    /// </summary>
    public void Initialize(int index, Color normal, Color hover, Color selected)
    {
        nodeIndex = index;
        normalColor = normal;
        hoverColor = hover;
        selectedColor = selected;

        // Ensure there is a SphereCollider for WallInteraction raycasting.
        // Radius 0.5 matches Unity's default sphere mesh, so transform scale controls handle size.
        var sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.radius = 0.5f;
        sphereCollider.center = Vector3.zero;

        nodeRenderer = GetComponent<Renderer>();
        if (nodeRenderer == null)
            nodeRenderer = GetComponentInChildren<Renderer>();

        propertyBlock = new MaterialPropertyBlock();
        SetState(NodeState.Normal);
    }

    /// <summary>
    /// Sets the visual state of the node.
    /// </summary>
    public void SetState(NodeState state)
    {
        currentState = state;
        UpdateVisual();
    }

    /// <summary>
    /// Updates the visual appearance based on current state.
    /// </summary>
    private void UpdateVisual()
    {
        if (nodeRenderer == null) return;

        Color targetColor;
        switch (currentState)
        {
            case NodeState.Hover:
                targetColor = hoverColor;
                break;
            case NodeState.Selected:
                targetColor = selectedColor;
                break;
            default:
                targetColor = normalColor;
                break;
        }

        // Use MaterialPropertyBlock to avoid creating material instances
        if (nodeRenderer != null)
        {
            propertyBlock.SetColor("_Color", targetColor);
            nodeRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void OnMouseEnter()
    {
        if (currentState != NodeState.Selected)
        {
            SetState(NodeState.Hover);
        }
    }

    private void OnMouseExit()
    {
        if (currentState != NodeState.Selected)
        {
            SetState(NodeState.Normal);
        }
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1))
        {
            // Right-click: open RadialToolMenu (not immediate delete)
            OnNodeRightClicked?.Invoke(nodeIndex);
        }
    }

    /// <summary>
    /// Gets the node index.
    /// </summary>
    public int GetNodeIndex()
    {
        return nodeIndex;
    }

    /// <summary>
    /// Updates the node index (used after insertions/deletions).
    /// </summary>
    public void SetNodeIndex(int index)
    {
        nodeIndex = index;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw a small sphere at the node position for easier selection in editor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
#endif
}
