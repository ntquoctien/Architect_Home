# 08 Active Systems

**Architect_Home** currently relies on the following active, supported systems.

## The Mesh Generation Suite
The core engine that translates user input into 3D architecture geometry.
*   **`RoomData`**: The repository of truth for corner vectors.
*   **`RoomEditorController`**: Handles the math and logic for dragging corners and splitting edges.
*   **`RoomMeshGenerator`**: A complex, critical script that reads `RoomData` arrays and calculates non-convex Ear Clipping triangulation. Calculates proper wall extrusion vectors and generates UVs based on world-scale parameters.

## Visualization and Scene Polish
Systems that make the generated mathematics look correct in the engine.
*   **`WallManager`**: Calculates relative angle to camera dot-products to hide occluding walls dynamically.
*   **`RoomThemeController`**: Re-assigns materials and adjusts UV scaling logic to match the dimensions of dynamically generated meshes.
*   **`CameraController`**: Provides Orbit controls (WASD/Mouse) while performing constant sphere-casts to prevent the camera clip-plane from intercepting walls. Includes a safe-distance padding calculation to prevent spawning inside room bounds.
