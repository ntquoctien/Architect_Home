# Design: Procedural Room System (v3)

## Architecture Overview

`RoomData` is the single source of truth. Every other system reads from it — nothing writes geometry directly to the scene.

```
Player Input
     │
     ▼
WallInteraction          ← raycasting, hover, click detection
     │ events
     ▼
RoomEditorController     ← drag, split edge, open radial menu
     │ mutates
     ▼
RoomData                 ← corners, edges, room height (source of truth)
     │ OnGeometryChanged
     ▼
RoomMeshGenerator        ← floor (ear-clip) + walls (extrusion + thickness)
     │
     ├─► RoomThemeController   ← applies materials + UV scaling
     └─► RoomMeasurementView  ← displays per-wall length labels
```

---

## Design Principles

- **Fixed wall thickness**: 0.15 m constant. No configurable system. Reduces complexity and keeps geometry stable.
- **No `MeshCollider` per wall**: Edge interaction uses lightweight per-edge `BoxCollider`s, sized and oriented at runtime. Avoids physics performance spikes.
- **Mesh pooling over destroy/instantiate**: Wall mesh objects are pre-created and activated/deactivated as corner count changes.
- **Selective mesh updates**: When one corner moves, only its two adjacent walls are rebuilt. The floor is always fully re-triangulated (cheap for N ≤ ~30 points).

---

## Component Designs

### 1. RoomData
**File**: `Assets/_Project/Scripts/RoomData.cs` *(extend existing)*

Single source of truth for room geometry. All other systems read from it; nothing writes geometry around it.

**Public API:**
```csharp
// Properties
public List<Vector3> Corners          // XZ-plane world-space corner positions
public float RoomHeight               // Default: 3 m
public event Action OnGeometryChanged

// Mutation (each validates polygon before firing event)
public void AddCorner(Vector3 pos)
public void InsertCorner(int index, Vector3 pos)
public bool RemoveCorner(int index)   // Enforces minimum 3 corners
public bool MoveCorner(int index, Vector3 pos)
public void SetRoomHeight(float h)

// Queries — used now and by future furniture placement
public (Vector3 start, Vector3 end) GetEdge(int index)
public float GetEdgeLength(int index)
public float GetArea()
public float GetPerimeter()
public bool IsPointInsideRoom(Vector3 worldPoint)   // 2D point-in-polygon on XZ plane

// Internal
internal int LastChangedCornerIndex   // -1 = full rebuild, ≥ 0 = selective update
private bool ValidatePolygon()        // Self-intersection check
```

**Default room**: `RoomData(width: 5, length: 5, height: 3, centerPosition)` creates a CCW rectangular polygon.

---

### 2. RoomMeshGenerator
**File**: `Assets/_Project/Scripts/RoomMeshGenerator.cs` *(extend existing)*

Listens to `RoomData.OnGeometryChanged` and rebuilds geometry. It is the **only** place that creates meshes.

**Floor generation:**
- Ear Clipping triangulation on the XZ polygon (supports non-convex shapes).
- Always fully rebuilt when geometry changes.

**Wall generation:**
- For each edge (i → i+1): extrude a quad upward by `RoomHeight`.
- **Wall thickness** = `const float WallThickness = 0.15f` — a constant in this class, not a `RoomData` field.
- At each corner, a miter fill quad (or angled cap mesh) closes the gap between two adjacent walls at any angle.
- **No `MeshCollider`**: After each wall is built, a `BoxCollider` child is positioned, sized (`(edgeLength, RoomHeight, WallThickness)`), and oriented to match the wall segment.

**Object Pooling:**
- At initialization, create `maxWalls` (e.g., 32) child GameObjects each with `MeshFilter` + `MeshRenderer` + `BoxCollider`.
- On geometry change: activate the first N children (where N = `roomData.Corners.Count`), deactivate the rest.
- Mesh data is reused via `Mesh.Clear()` + reassign — no `Instantiate/Destroy` during updates.

**Selective update:**
- If `LastChangedCornerIndex ≥ 0`: only rebuild wall `(i-1)` and wall `i` (two adjacent walls).
- If `LastChangedCornerIndex == -1`: full rebuild (on init or corner count change).

**Public API:**
```csharp
public Renderer GetFloorRenderer()
public List<Renderer> GetWallRenderers()
public void GenerateMeshes(RoomData data)
```

---

### 3. WallInteraction
**File**: `Assets/_Project/Scripts/WallInteraction.cs` *(new)*

Dedicated input system. Owns all raycasting. Fires typed events that `RoomEditorController` subscribes to. Contains zero mesh or data logic.

**Events:**
```csharp
public event Action<int>             OnVertexHovered         // corner index
public event Action<int>             OnVertexClicked         // corner index (left-click)
public event Action<int>             OnVertexRightClicked    // corner index (right-click)
public event Action<int, Vector3>    OnEdgeClicked           // edgeIndex + world hit point
```

**Each frame:**
1. Guard: if `EventSystem.current.IsPointerOverGameObject()` → skip.
2. Raycast from `Camera.main` through `Input.mousePosition` against the NodeHandle layer.
3. If a `NodeHandle` is hit: manage hover state; on click fire `OnVertexClicked`/`OnVertexRightClicked`.
4. If no `NodeHandle` hit: raycast against wall `BoxCollider` layer; identify nearest edge index; on click fire `OnEdgeClicked`.

---

### 4. RoomEditorController
**File**: `Assets/_Project/Scripts/RoomEditorController.cs` *(extend existing)*

Orchestrator. Connects `WallInteraction` events → `RoomData` mutations → `NodeHandle` sync.

**Serialized fields:**
```csharp
[SerializeField] float initialWidth  = 5f;
[SerializeField] float initialLength = 5f;
// RoomData.RoomHeight default = 3 m
[SerializeField] GameObject nodeHandlePrefab;
[SerializeField] RadialToolMenu radialToolMenu;
```

**Responsibilities:**
- `Awake`: Auto-find `WallInteraction`, subscribe to its events.
- `Start`: Call `InitializeRoom(initialWidth, initialLength)` if not already initialized.
- `OnVertexClicked(i)` → set `selectedNode`, enter drag mode.
- `Update` drag: call `RoomData.MoveCorner(index, groundPlaneHit)`, reposition `NodeHandle`.
- `OnVertexRightClicked(i)` → call `radialToolMenu.Show(cornerWorldPos, i)`.
- `OnEdgeClicked(edgeIndex, hitPoint)` → call `RoomData.InsertCorner(edgeIndex+1, hitPoint)` → `SyncNodeHandles()`.
- `SyncNodeHandles()`: creates / repositions / deactivates `NodeHandle` GameObjects to match `roomData.Corners` count — no full teardown.

**Runtime dimension controls:**  
A runtime UI panel (sliders or numeric input fields) binds to these public methods, letting the player resize the whole room at any time:
```csharp
public void SetWidth(float w)   // rebuilds RoomData corners to new rectangle, keeps center
public void SetLength(float l)
public void SetHeight(float h)  // delegates to RoomData.SetRoomHeight(h)
```
These controls apply only to the overall bounding box of the current polygon. After a resize, all corner positions are scaled proportionally from the room center, then `OnGeometryChanged` fires normally.

**Public API:**
```csharp
public RoomData GetRoomData()
public void InitializeFromExisting(float width, float length)
public void SetWidth(float w)
public void SetLength(float l)
public void SetHeight(float h)
```

---

### 5. NodeHandle
**File**: `Assets/_Project/Scripts/NodeHandle.cs` *(extend existing)*

A `NodeHandle` **is** the wall corner / vertex. Every corner point in `RoomData.Corners` has exactly one corresponding `NodeHandle` in the scene. The terms *vertex*, *corner*, and *NodeHandle* refer to the same concept.

- Has a `SphereCollider` (added in `Initialize()` if missing).
- States: `Normal`, `Hover`, `Selected` — drives `MeshRenderer` material color.
- Events:
  ```csharp
  public event Action<int> OnNodeDeleted       // right-click > Remove Corner
  public event Action<int> OnNodeRightClicked  // fired on right mouse button up
  ```
- `Initialize(int index, Color normal, Color hover, Color selected)`

---

### 6. RadialToolMenu
**File**: `Assets/_Project/Scripts/UI/RadialToolMenu.cs` *(new)*  
**Prefab**: `Assets/_Project/Prefabs/UI/RadialToolMenu.prefab`

Screen-space Canvas panel that appears near the right-clicked vertex.

**API:**
```csharp
public void Show(Vector3 worldPosition, int cornerIndex)
public void Hide()
public event Action OnActionPerformed   // fires after any button action → caller hides menu
```

**Button actions:**
| Button | Behaviour |
|---|---|
| **Add Wall** | **Splits an existing edge** by inserting a new corner at the midpoint of one of the two edges adjacent to this vertex. Player chooses which edge ("Left wall" / "Right wall" sub-prompt). This turns one wall segment into two. |
| **Extend Wall** | Moves this vertex 0.5 m outward along the bisector of its two adjacent walls |
| **Remove Corner** | Calls `RoomData.RemoveCorner(index)` — silently blocked if only 3 corners remain |
| **Create New Corner** | Enters "place mode": next ground-plane left-click calls `RoomData.AddCorner(hitPoint)` |

Auto-hides on outside click or `Escape`. Positioned via `Camera.main.WorldToScreenPoint()`.

---

### 7. RoomMeasurementView
**File**: `Assets/_Project/Scripts/RoomMeasurementView.cs` *(extend existing)*

Subscribes to `RoomData.OnGeometryChanged`. For each edge, displays a world-space label at the edge midpoint showing `GetEdgeLength(i)` formatted to two decimal places (e.g., `"3.50 m"`). Uses `TextMeshPro` world-space text objects (pooled).

---

### 8. FurniturePlacement (data contract only)
**File**: `Assets/_Project/Scripts/FurniturePlacement.cs` *(stub — no logic)*

Defines the interface future systems will use. Reads from `RoomData` directly.

```csharp
// All data available to furniture placement:
roomData.Corners              // polygon boundary
roomData.RoomHeight           // wall height
roomData.GetEdge(i)           // wall segment endpoints
roomData.GetArea()            // floor area
roomData.IsPointInsideRoom()  // placement validation

// Stub method:
public bool TryPlaceOnFloor(GameObject prefab, Vector3 worldPos) => false;
```

---

## Data Flow Detail

```
Player drags NodeHandle
        │
        ▼
RoomEditorController.HandleNodeDrag()
        │  MoveCorner(index, groundHit)
        ▼
RoomData                                ← validates polygon, sets LastChangedCornerIndex
        │  OnGeometryChanged
        ├──► RoomMeshGenerator          ← selective wall rebuild + floor rebuild
        │        └─► BoxCollider resize per wall
        ├──► RoomMeasurementView        ← updates length labels
        └──► RoomThemeController        ← re-applies materials if renderer references changed
```

---

## File Checklist

| File | Status | Action |
|---|---|---|
| `RoomData.cs` | Exists | Add `IsPointInsideRoom`, `LastChangedCornerIndex` |
| `RoomMeshGenerator.cs` | Exists | Add pooling, BoxCollider per wall, fixed thickness const |
| `RoomEditorController.cs` | Exists | Wire `WallInteraction` events, add `SyncNodeHandles()` |
| `NodeHandle.cs` | Exists | Add `OnNodeRightClicked` event |
| `WallInteraction.cs` | **New** | Create from scratch |
| `RadialToolMenu.cs` | **New** | Create from scratch + prefab |
| `RoomMeasurementView.cs` | Exists | Confirm pooled label update on event |
| `FurniturePlacement.cs` | **New** | Stub only |
