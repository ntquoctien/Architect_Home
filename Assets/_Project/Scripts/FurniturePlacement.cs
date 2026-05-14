using UnityEngine;

/// <summary>
/// Skeleton for future furniture placement system.
/// Reads geometry from RoomData — no placement logic is implemented yet.
///
/// When implementing, the following RoomData APIs will be the primary data sources:
///   roomData.Corners             — polygon boundary for floor extent
///   roomData.RoomHeight          — wall height (for wall-mount height constraints)
///   roomData.GetEdge(int index)  — world-space wall segment (start, end)
///   roomData.GetArea()           — total floor area (for placement validation)
///   roomData.IsPointInsideRoom() — checks whether a proposed placement point is inside the room
/// </summary>
[RequireComponent(typeof(RoomEditorController))]
public class FurniturePlacement : MonoBehaviour
{
    private RoomData roomData;

    private void Start()
    {
        roomData = GetComponent<RoomEditorController>().GetRoomData();
    }

    /// <summary>
    /// Attempts to place a furniture prefab on the floor at the given world position.
    /// NOT YET IMPLEMENTED — returns false always.
    ///
    /// Future implementation should:
    ///   1. Call roomData.IsPointInsideRoom(worldPos) to check validity.
    ///   2. Raycast downward from worldPos to find the floor surface.
    ///   3. Check clearance (no overlap with existing furniture).
    ///   4. Instantiate the prefab and register it with a FurnitureManager.
    /// </summary>
    public bool TryPlaceOnFloor(GameObject prefab, Vector3 worldPos)
    {
        Debug.Log("[FurniturePlacement] TryPlaceOnFloor is not yet implemented.");
        return false;
    }

    /// <summary>
    /// Attempts to attach a furniture/décor object to a wall at the given world position.
    /// NOT YET IMPLEMENTED — returns false always.
    ///
    /// Future implementation should:
    ///   1. Find the nearest edge via roomData.GetEdge(i).
    ///   2. Project worldPos onto the edge to find the attachment point.
    ///   3. Orient the object so it faces inward (toward room centre).
    ///   4. Instantiate the prefab against the wall surface.
    /// </summary>
    public bool TryAttachToWall(GameObject prefab, Vector3 worldPos)
    {
        Debug.Log("[FurniturePlacement] TryAttachToWall is not yet implemented.");
        return false;
    }
}
