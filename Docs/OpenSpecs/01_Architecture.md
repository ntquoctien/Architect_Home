# 01 Architecture

The architecture of Architect_Home centers around isolated modules that handle different stages of the room generation lifecycle.

## Static Room Generation
*   **`RoomController`**: Generates a standard, parameter-driven rectangular room. It uses user-defined `width`, `length`, `height`, and `thickness` to quickly bootstrap a basic layout.

## Dynamic Mesh Generation Pipeline
This is the core pipeline of the application:
1.  **Data Structure (`RoomData`)**: Holds the geometric truth of the room, storing the list of corners (nodes) and calculated edges.
2.  **Interaction (`RoomEditorController`)**: Handles user input (clicking, dragging, splitting) and updates the `RoomData`.
3.  **Mesh Generation (`RoomMeshGenerator`)**: Listens for changes in `RoomData` and physically builds the meshes.
    *   **Floors**: Generated using Ear Clipping triangulation to support non-convex shapes safely.
    *   **Walls**: Extruded dynamically along the edges defined by the corners, featuring calculated corner posts to fill gaps at joints.

## Visibility Management
*   **`WallManager`**: A centralized manager that controls the visibility of all walls. It calculates the dot product between the camera's forward vector and the room's center to determine if a wall is occluding the view, hiding it if necessary without requiring scripts on every wall object.
