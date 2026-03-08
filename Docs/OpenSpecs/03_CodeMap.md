# 03 Code Map

All primary scripts are located within `Assets/_Project/Scripts/`.

## Core Pipeline
- `RoomData.cs`: Core data model for the procedural room polygon.
- `RoomEditorController.cs`: Runtime node manipulation and edge-splitting logic.
- `RoomMeshGenerator.cs`: Ear Clipping triangulation and 3D wall extrusion.
- `NodeHandle.cs`: Visual and interaction state for draggable room corners.

## Visibility & Camera
- `WallManager.cs`: Centralized dot-product based wall occlusion manager.
- `CameraController.cs`: Orbit camera system with collision detection and automatic bounding box adaptation.
- `RoomWallHider.cs` & `AutoHideWall.cs`: *(Legacy/Fallback visibility scripts, largely superseded by WallManager)*.

## State & UI
- `RoomController.cs`: Static, parameter-based rectangular room generator.
- `RoomModeSwitcher.cs`: State machine transitioning between `RoomController` and `RoomEditorController`.
- `RoomThemeController.cs`: Applies visual materials to procedural meshes.
- `RoomMeasurementView.cs`: Visualizes lengths and labels.
