# Architecture Decisions Log

## 1. Procedural Meshes over Fixed Grids
*   **Context**: Building dynamic, customizable rooms.
*   **Decision**: Use dynamic vertex generation and Ear Clipping triangulation (`RoomMeshGenerator`) instead of a rigid 2D grid/tile-based approach.
*   **Reasoning**: Allows for non-convex rooms, angled walls, and precise real-world measurements that a fixed grid restricts.
*   **Status**: Accepted and implemented as the core feature of the architect app.

## 2. Centralized Wall Occlusion
*   **Context**: Hiding walls that block the camera view of the room interior.
*   **Decision**: Use a single manager (`WallManager`) that calculates dot products between the camera vector and the room center.
*   **Reasoning**: Greatly improves performance and simplicity by not requiring rigidbodies, colliders, or update loops on every single wall segment.
*   **Status**: Accepted and active.
