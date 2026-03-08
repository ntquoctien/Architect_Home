# 02 System Boundaries

Strict boundaries are maintained between data models, user input, and visual generation to ensure a robust architecture.

## Separation of Concerns

### 1. Geometric Data Structure
*   **Boundary**: `RoomData.cs`
*   **Responsibility**: Holds mathematical and coordinate truth. It does not know about meshes, renderers, or cameras. It only manages lists of Vector3 coordinates and fires events (`OnGeometryChanged`) when they are modified.

### 2. User Input & Modification
*   **Boundary**: `RoomEditorController.cs`, `RoomModeSwitcher.cs`
*   **Responsibility**: Raycasting, detecting mouse clicks, moving handles, and interpreting user intent. It modifies `RoomData` but does *not* generate meshes itself.

### 3. Procedural Visualization
*   **Boundary**: `RoomMeshGenerator.cs`, `WallManager.cs`, `RoomThemeController.cs`
*   **Responsibility**: Listening to data events and building the visual representation (Meshes, Materials, visibility toggling). It should never modify the underlying geometric data.

## Transition Boundaries
*   Moving between the static fallback (`RoomController`) and the dynamic editor (`RoomEditorController`) is exclusively marshaled by `RoomModeSwitcher` to prevent state conflicts and dual-generation.
