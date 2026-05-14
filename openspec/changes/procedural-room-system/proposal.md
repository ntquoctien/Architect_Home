# Proposal: Procedural Room System

## What

Convert the current static prefab-based room to a fully dynamic, vertex-driven procedural room system. All geometry is generated at runtime from `RoomData`. Players can reshape the room arbitrarily and the mesh updates in real time.

## Why

Architect_Home's core loop is letting users design any room they can imagine. A prefab room hard-codes geometry and makes L-shapes, U-shapes, or any non-rectangular layout impossible without manual scene editing. A procedural system removes that constraint entirely.

The codebase already has partial foundations (`RoomData`, `RoomMeshGenerator`, `RoomEditorController`), but they are missing a robust interaction layer (radial menu, dedicated input system) and several APIs required for future furniture placement compatibility.

## Non-Goals

- Furniture placement logic is **not** implemented — only the data contract is defined.
- Parametric / configurable wall thickness is **not** in scope. Wall thickness is a **fixed constant** (0.15 m). No UI for it.
- Multi-room / room adjacency is **not** in scope.
- Undo/redo history is **not** in scope.
- Networked / multiplayer editing is **not** in scope.

## Terminology

- **Wall corner / vertex** — the same thing. A `NodeHandle` is the interactive handle placed at each polygon corner point. The terms are used interchangeably in these docs.
- **Add Wall** — splits an existing wall edge by inserting a new corner at the midpoint (or a chosen point) of that edge, turning one wall segment into two.

---

## Success Criteria

- Game starts with a default 5 m × 5 m × 3 m rectangular room — no prefab, no scene-placed mesh.
- Player can adjust **room width, length, and height at runtime** via editor UI controls (e.g., sliders or input fields); the mesh updates immediately.
- Player can drag any corner vertex (`NodeHandle`) in real time; mesh updates instantly.
- Player can left-click an edge to insert a new vertex, enabling non-rectangular shapes.
- Right-clicking a vertex opens a radial tool menu: **Add Wall** (split edge), **Extend Wall**, **Remove Corner**, **Create New Corner**.
- All geometry (floor + walls with visible thickness) is driven exclusively by `RoomData`.
- Wall corners have no visible gaps despite varying angles.
- Each wall segment displays its length via `RoomMeasurementView`.
- `RoomData` exposes `GetEdge()`, `GetArea()`, `GetPerimeter()`, `IsPointInsideRoom()` for future furniture placement.
- No heavy `MeshCollider` per wall — edge interaction uses lightweight `BoxCollider`s.

