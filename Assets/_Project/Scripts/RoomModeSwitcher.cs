using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the transition between the static RoomController system
/// and the dynamic RoomEditorController system.
/// Assign SwitchToEditMode() to Btn_customroom's OnClick event.
/// </summary>
public class RoomModeSwitcher : MonoBehaviour
{
    [Header("Old Static Room System")]
    [Tooltip("The RoomController script controlling the static room")]
    [SerializeField] private RoomController oldRoom;
    
    [Tooltip("The root GameObject of the static room visuals")]
    [SerializeField] private GameObject oldRoomObject;

    [Header("New Dynamic Room System")]
    [Tooltip("The RoomEditorController for the new dynamic editing system")]
    [SerializeField] private RoomEditorController newRoomEditor;
    
    [Tooltip("The root GameObject of the new room editor system")]
    [SerializeField] private GameObject newRoomObject;

    [Header("Optional UI References")]
    [Tooltip("Button to switch to edit mode (auto-assigns listener if set)")]
    [SerializeField] private Button btnCustomRoom;

    [Header("Debug")]
    [SerializeField] private bool logTransitions = true;

    /// <summary>
    /// Current editing mode state.
    /// </summary>
    public bool IsInEditMode { get; private set; } = false;

    private void Start()
    {
        // Ensure initial state: Old system active, New system inactive
        SetOldSystemActive(true);
        SetNewSystemActive(false);
        IsInEditMode = false;

        // Auto-assign button listener if reference is set
        if (btnCustomRoom != null)
        {
            btnCustomRoom.onClick.AddListener(SwitchToEditMode);
        }

        if (logTransitions)
        {
            Debug.Log("[RoomModeSwitcher] Initialized. Static room active, Editor inactive.");
        }
    }

    /// <summary>
    /// Switches from the static room system to the dynamic edit mode.
    /// Assign this method to Btn_customroom's OnClick event.
    /// </summary>
    public void SwitchToEditMode()
    {
        if (IsInEditMode)
        {
            Debug.LogWarning("[RoomModeSwitcher] Already in edit mode.");
            return;
        }

        if (oldRoom == null)
        {
            Debug.LogError("[RoomModeSwitcher] Old room reference is not set!");
            return;
        }

        if (newRoomEditor == null || newRoomObject == null)
        {
            Debug.LogError("[RoomModeSwitcher] New room editor references are not set!");
            return;
        }

        // Step 1: Get dimensions from the old room
        float width = oldRoom.width;
        float length = oldRoom.length;

        if (logTransitions)
        {
            Debug.Log($"[RoomModeSwitcher] Transitioning to Edit Mode. Room size: {width}m x {length}m");
        }

        // Step 2: Activate the new room system
        SetNewSystemActive(true);

        // Step 3: Initialize the new room editor with existing dimensions
        // This ensures the new room matches the old room's shape perfectly
        newRoomEditor.InitializeFromExisting(width, length);

        // Step 4: Deactivate the old room system to prevent conflicts
        SetOldSystemActive(false);

        IsInEditMode = true;

        if (logTransitions)
        {
            Debug.Log("[RoomModeSwitcher] Successfully switched to Edit Mode.");
        }
    }

    /// <summary>
    /// Switches back from edit mode to the static room system.
    /// Optionally updates the static room with the edited dimensions.
    /// </summary>
    /// <param name="applyChanges">If true, applies the edited dimensions to the old room</param>
    public void SwitchToStaticMode(bool applyChanges = true)
    {
        if (!IsInEditMode)
        {
            Debug.LogWarning("[RoomModeSwitcher] Already in static mode.");
            return;
        }

        if (applyChanges && newRoomEditor != null && oldRoom != null)
        {
            // Get the bounding box of the edited room
            RoomData roomData = newRoomEditor.GetRoomData();
            if (roomData != null && roomData.Corners.Count >= 3)
            {
                // Calculate bounding dimensions from corners
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 max = new Vector2(float.MinValue, float.MinValue);

                foreach (var corner in roomData.Corners)
                {
                    min.x = Mathf.Min(min.x, corner.x);
                    min.y = Mathf.Min(min.y, corner.z);
                    max.x = Mathf.Max(max.x, corner.x);
                    max.y = Mathf.Max(max.y, corner.z);
                }

                float newWidth = max.x - min.x;
                float newLength = max.y - min.y;

                // Update old room dimensions
                oldRoom.width = newWidth;
                oldRoom.length = newLength;

                if (logTransitions)
                {
                    Debug.Log($"[RoomModeSwitcher] Applied edited dimensions: {newWidth}m x {newLength}m");
                }
            }
        }

        // Activate old system
        SetOldSystemActive(true);

        // Apply layout to reflect any dimension changes
        if (oldRoom != null)
        {
            oldRoom.ApplyLayout();
        }

        // Deactivate new system
        SetNewSystemActive(false);

        IsInEditMode = false;

        if (logTransitions)
        {
            Debug.Log("[RoomModeSwitcher] Successfully switched to Static Mode.");
        }
    }

    /// <summary>
    /// Toggles between edit and static modes.
    /// </summary>
    public void ToggleMode()
    {
        if (IsInEditMode)
        {
            SwitchToStaticMode(true);
        }
        else
        {
            SwitchToEditMode();
        }
    }

    /// <summary>
    /// Sets the old room system active/inactive.
    /// </summary>
    private void SetOldSystemActive(bool active)
    {
        if (oldRoomObject != null)
        {
            oldRoomObject.SetActive(active);
        }

        if (oldRoom != null)
        {
            oldRoom.enabled = active;
        }
    }

    /// <summary>
    /// Sets the new room editor system active/inactive.
    /// </summary>
    private void SetNewSystemActive(bool active)
    {
        if (newRoomObject != null)
        {
            newRoomObject.SetActive(active);
        }

        if (newRoomEditor != null)
        {
            newRoomEditor.enabled = active;
        }
    }

    private void OnDestroy()
    {
        // Clean up button listener
        if (btnCustomRoom != null)
        {
            btnCustomRoom.onClick.RemoveListener(SwitchToEditMode);
        }
    }
}
