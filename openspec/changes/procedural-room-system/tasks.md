# Tasks: Procedural Room System (v3)

---

## Group A — RoomData

- [x] **A1** — Confirm public API: `Corners`, `RoomHeight`, `OnGeometryChanged`, mutation methods (`AddCorner`, `InsertCorner`, `RemoveCorner` min-3 enforced, `MoveCorner`, `SetRoomHeight`).
- [x] **A2** — Add `IsPointInsideRoom(Vector3 worldPoint)` — 2D point-in-polygon test on the XZ plane (ray-casting algorithm).
- [x] **A3** — Add `internal int LastChangedCornerIndex` (`-1` = full rebuild, `≥ 0` = selective). Set it in `MoveCorner`; reset to `-1` in `InsertCorner`, `RemoveCorner`, `AddCorner`.
- [x] **A4** — Confirm constructor `RoomData(float width, float length, float height, Vector3 centerPosition)` creates a CCW rectangle. Default values: `width=5`, `length=5`, `height=3`.
- [x] **A5** — Verify all mutation methods call `ValidatePolygon()` (self-intersection check) before firing `OnGeometryChanged`. Revert and warn on invalid result.

---

## Group B — RoomMeshGenerator

- [x] **B1** — Add `const float WallThickness = 0.15f` at the top of the class. Remove any serialized/parametric thickness field.
- [x] **B2** — Implement wall object pooling: on `Awake`, pre-create `MaxWalls = 32` child GameObjects each with `MeshFilter`, `MeshRenderer`, `BoxCollider`. Activate/deactivate as count changes — no `Instantiate/Destroy` during updates.
- [x] **B3** — Wall mesh extrusion: for each edge (i → i+1), generate a quad extruded to `RoomHeight` with depth `WallThickness`. Fill corner gaps with a miter cap quad at each joint.
- [x] **B4** — Replace any `MeshCollider` per wall with a `BoxCollider`. After building each wall quad, resize and orient its pooled `BoxCollider` to `(edgeLength, RoomHeight, WallThickness)`.
- [x] **B5** — Selective update: if `LastChangedCornerIndex ≥ 0`, only call `RebuildWall(i-1)` and `RebuildWall(i)`. Else call `FullRebuild()`. Floor always uses `Mesh.Clear()` + full re-triangulate.
- [x] **B6** — Expose `GetFloorRenderer()` and `GetWallRenderers()` (used by `RoomThemeController`).
- [x] **B7** — Subscribe to `RoomData.OnGeometryChanged` in `Initialize(RoomData data)`. Store the `RoomData` reference; call `GenerateMeshes()` on each event.

---

## Group C — WallInteraction (new script)

- [x] **C1** — Create `Assets/_Project/Scripts/WallInteraction.cs`.
- [x] **C2** — Declare events: `OnVertexHovered(int)`, `OnVertexClicked(int)`, `OnVertexRightClicked(int)`, `OnEdgeClicked(int edgeIndex, Vector3 hitPoint)`.
- [x] **C3** — In `Update()`: guard with `EventSystem.current.IsPointerOverGameObject()`. If over UI, skip.
- [x] **C4** — Raycast against NodeHandle layer first. On hit: manage hover state (`NodeHandle.SetState`), fire click/right-click events.
- [x] **C5** — If no NodeHandle hit: raycast against wall `BoxCollider` layer. Identify which edge index was hit (tag or component reference). On left-click, fire `OnEdgeClicked(edgeIndex, hit.point)`.
- [x] **C6** — Clear hover when mouse leaves a NodeHandle (track `lastHoveredIndex`, set `Normal` on exit).

---

## Group D — NodeHandle

- [x] **D1** — A `NodeHandle` IS the wall corner vertex. Ensure `Initialize(int index, Color normal, Color hover, Color selected)` adds a `SphereCollider` if none exists. One `NodeHandle` must exist for every entry in `RoomData.Corners`; their indices must stay in sync.
- [x] **D2** — Add `public event Action<int> OnNodeRightClicked`. Fire on right mouse button up while the collider is hovered.
- [x] **D3** — Confirm `SetState(NodeState)` correctly swaps `MeshRenderer` material color for `Normal`, `Hover`, `Selected`.

---

## Group E — RoomEditorController

- [x] **E1** — In `Awake()`: get `WallInteraction` component (or `FindAnyObjectByType`). Subscribe: `OnVertexClicked`, `OnVertexRightClicked`, `OnEdgeClicked`.
- [x] **E2** — `OnVertexClicked(i)` → set `selectedNode = nodeHandles[i]`, `isDraggingNode = true`, call `selectedNode.SetState(Selected)`.
- [x] **E3** — `Update()` drag: ground-plane raycast → call `RoomData.MoveCorner(index, hitPos)` → reposition `selectedNode.transform.position`.
- [x] **E4** — `OnVertexRightClicked(i)` → call `radialToolMenu.Show(roomData.Corners[i], i)`.
- [x] **E5** — `OnEdgeClicked(edgeIndex, hitPoint)` → `RoomData.InsertCorner(edgeIndex+1, hitPoint)` → `SyncNodeHandles()`.
- [x] **E6** — `SyncNodeHandles()`: loop corners; if handle exists reposition it; if new corner create a handle; deactivate excess handles. No full teardown.
- [x] **E7** — `InitializeRoom(width, length)` called from `Start()` using serialized defaults. Passes `centerPosition: transform.position`.
- [x] **E8** — Add `SetWidth(float w)` and `SetLength(float l)`: scale all `Corners` proportionally from the room center, then call `OnGeometryChanged`. Hook these to runtime UI sliders/fields.
- [x] **E9** — `SetHeight(float h)`: delegates to `RoomData.SetRoomHeight(h)`. Hook to runtime UI slider/field. No corner repositioning needed.

---

## Group F — RadialToolMenu (new script + prefab)

- [x] **F1** — Create `Assets/_Project/Scripts/UI/RadialToolMenu.cs`.
- [ ] **F2** — Create Canvas prefab `Assets/_Project/Prefabs/UI/RadialToolMenu.prefab` with four `Button` children in a radial layout (N/E/S/W).
- [x] **F3** — `Show(Vector3 worldPos, int cornerIndex)`: anchor panel at `Camera.main.WorldToScreenPoint(worldPos)`.
- [x] **F4** — `Hide()`: deactivate panel. Auto-hide on click-outside or `Escape`.
- [x] **F5** — Wire buttons:
  - **Add Wall** → **splits an existing edge**: inserts a new corner at the midpoint of one of the two edges adjacent to this vertex ("Left wall" / "Right wall" sub-prompt). Calls `RoomData.InsertCorner(targetEdgeIndex + 1, midpoint)`. One wall becomes two.
  - **Extend Wall** → move vertex 0.5 m along bisector of its two adjacent walls.
  - **Remove Corner** → `RoomData.RemoveCorner(index)` (silently blocked if only 3 corners remain).
  - **Create New Corner** → enter place-mode; next ground-plane left-click calls `RoomData.AddCorner(hitPoint)`.
- [x] **F6** — Fire `OnActionPerformed` event after any button action so `RoomEditorController` closes the menu and resyncs handles.

---

## Group G — RoomMeasurementView

- [x] **G1** — Confirm `RoomMeasurementView` subscribes to `RoomData.OnGeometryChanged`.
- [x] **G2** — Pool `TextMeshPro` world-space labels (one per edge). On update: reposition label to edge midpoint, set text to `$"{roomData.GetEdgeLength(i):F2} m"`.
- [x] **G3** — Activate/deactivate labels to match edge count — no destroy/instantiate.

---

## Group H — FurniturePlacement Skeleton

- [x] **H1** — Create `Assets/_Project/Scripts/FurniturePlacement.cs`.
- [x] **H2** — `[RequireComponent(typeof(RoomEditorController))]`. In `Start()` get `RoomData` via `GetComponent<RoomEditorController>().GetRoomData()`.
- [x] **H3** — Add stub: `public bool TryPlaceOnFloor(GameObject prefab, Vector3 worldPos) => false;`. Document in XML comments which `RoomData` APIs the real implementation will use.

---

## Group I — Scene Setup & Verification

- [ ] **I1** — Create `RoomSystem` GameObject in scene. Add `RoomEditorController`, `RoomMeshGenerator`, `RoomMeasurementView`, `WallInteraction` components.
- [ ] **I2** — Assign `RadialToolMenu` prefab in `RoomEditorController` Inspector. Assign `nodeHandlePrefab`.
- [ ] **I3** — Configure `LayerMask` fields in `WallInteraction` for NodeHandle and wall `BoxCollider` layers.
- [ ] **I4** — Enter Play mode. Verify: rectangular room appears with no prefab, corners draggable, edge click inserts vertex, right-click opens radial menu.
- [ ] **I5** — Test rapid vertex dragging: confirm no null refs, no garbage allocation spikes, mesh stays valid.
- [ ] **I6** — Test corner removal: remove down to 3 corners; confirm the 3rd removal attempt is silently blocked.
- [ ] **I7** — Test L-shape: insert two vertices, drag them outward. Confirm walls have no gaps and `BoxCollider`s update correctly.
- [ ] **I8** — Verify `RoomThemeController` re-applies materials after a full rebuild.
- [ ] **I9** — Verify `RoomMeasurementView` labels update on every vertex drag.
