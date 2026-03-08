# 07 Migration Plan

## Sunsetting Grid Systems
The project has officially migrated off of grid-based layouts to mathematical, node-based procedural mesh generation. 

### Completed Migration Steps
- [x] Analyze architecture to identify conflicting paradigms.
- [x] Isolate and decouple dependencies (e.g., `CameraController` no longer blocks input based on `FurniturePlacer`).
- [x] Physically delete legacy `Blueprint` and `Furniture` scripts from the git repository.

### Pending Manual Cleanups (Unity Editor)
Developers must perform these manual steps in the Unity Editor to complete the migration:
- **`Room.unity`**: Locate and delete the `GridManager`, `GridVisuals`, `BlueprintInteraction`, and `BlueprintModeController` GameObjects. Find any Canvas UI buttons trying to trigger `ToggleBlueprintMode()` and remove the broken UnityEvent.
- **`SampleScene.unity`**: Locate and delete test buttons, GameObjects, and missing scripts associated with `FurniturePlacer`.

### Future Migrations
*   No other migrations are currently planned. Next steps involve building new features (doors, windows, multi-room generation) strictly upon the `RoomMeshGenerator` pipeline.
