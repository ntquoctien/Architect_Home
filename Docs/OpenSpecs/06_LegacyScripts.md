# 06 Legacy Scripts

## Deprecated & Removed Systems
The following systems were analyzed and physically deleted from the codebase (`Assets/_Project/Scripts/`) to simplify the architecture:

### 1. The Blueprint System
A grid-based 2D tile system that mathematically and conceptually conflicted with the non-convex procedural mesh generation approach.
*   `BlueprintModeController.cs`
*   `BlueprintInteraction.cs`
*   `GridManager.cs`
*   `GridVisuals.cs`

### 2. The Furniture System
A basic raycast-to-mesh placement script and associated ScriptableObjects that were considered experimental clutter outside the core architectural scope.
*   `FurniturePlacer.cs`
*   `FurnitureData.cs`
*   `FurnitureButton.cs`
*   `ObjectHoverInteractor.cs`

*(Note: While the scripts were removed from disk, dormant `GameObject` components referencing these scripts may still exist in `Room.unity` or `SampleScene.unity` as "Missing Scripts" until manually cleaned).*
