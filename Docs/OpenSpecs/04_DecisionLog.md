# 04 Decision Log

## Architecture Decisions

### 1. Procedural Meshes over Fixed Grids
*   **Context**: Building dynamic, customizable rooms.
*   **Decision**: Use dynamic vertex generation and Ear Clipping triangulation (`RoomMeshGenerator`) instead of a rigid 2D grid/tile-based approach.
*   **Reasoning**: Allows for non-convex rooms, angled walls, and precise real-world measurements that a fixed grid restricts.

### 2. Centralized Wall Occlusion
*   **Context**: Hiding walls that block the camera view of the room interior.
*   **Decision**: Use a single manager (`WallManager`) that calculates dot products between the camera vector and the room center.
*   **Reasoning**: Greatly improves performance and simplicity by not requiring rigidbodies, colliders, or repetitive Unity update loops on every single wall segment.

### 3. Removal of "Blueprint" and "Furniture" Systems
*   **Context**: The application contained dual paradigms: procedural node-based meshes and a grid-based tile placement system.
*   **Decision**: The grid-based Blueprint system and raycast Furniture Placer have been removed.
*   **Reasoning**: Consolidating around the superior Procedural Mesh pipeline simplifies the codebase, removes UI/interaction conflicts, and focuses the application entirely on high-fidelity architectural layout.
