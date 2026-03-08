# 05 Conventions

## Safety Rules & Best Practices

### 1. Core Generation Pipeline Preservation
*   **Do not break Ear Clipping**: The `RoomMeshGenerator.cs` relies on its triangulation algorithm to support complex, non-convex room shapes. Any updates to mesh generation must respect and maintain this capability.
*   **Isolate Data from Visualization**: `RoomData.cs` holds the geometric truth (corners). Modifiers (like `RoomEditorController.cs`) should update `RoomData`, which then triggers `RoomMeshGenerator` via events. Do not directly manipulate vertex data outside of the generator.

### 2. Dynamic Visibility
*   **Maintain WallManager Logic**: Wall visibility is managed centrally by `WallManager.cs` using camera dot-products. 
*   **No Per-Wall Scripts**: Do not attach visibility or interaction scripts to individual wall prefabs or dynamically generated wall meshes. Keep them lightweight.

### 3. UI decoupling
*   Keep interaction logic (raycasting, dragging, clicking) in interaction controllers (e.g., `RoomEditorController`) and avoid tightly coupling physical logic directly into the mesh generation scripts or UI button UnityEvents.
