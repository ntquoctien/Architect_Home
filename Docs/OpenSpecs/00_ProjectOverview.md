# 00 Project Overview

**Architect_Home** is an architectural procedural generation application built in Unity. 

The primary goal of the application is to empower users to dynamically generate, edit, and visualize 3D room layouts through procedural mesh generation, without being constrained by rigid grid-based tile systems.

## Core Capabilities
*   **Procedural Generation**: Dynamically creating floor and wall meshes from a set of corner nodes.
*   **Runtime Editing**: Allow users to interactively drag nodes and split edges to create complex, non-convex room shapes.
*   **Dynamic Visibility**: Automatically hiding walls that occlude the user's view of the room interior based on camera angles.

## Recent Architectural Shifts
The project recently moved away from a 2D tile/grid-based "Blueprint" system and an associated Furniture Placement raycasting system. These systems have been physically removed to refocus the codebase entirely on procedural mesh generation mathematically driven by Ear Clipping triangulation.
