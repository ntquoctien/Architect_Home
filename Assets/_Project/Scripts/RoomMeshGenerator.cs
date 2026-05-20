using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates procedural meshes for room floor and walls from RoomData.
/// Supports non-convex polygons via Ear Clipping triangulation.
/// Walls are pooled per-segment with BoxColliders.
/// </summary>
public class RoomMeshGenerator : MonoBehaviour
{
    // ── Wall thickness is a fixed constant — not configurable (by design) ──
    public const float DefaultWallThickness = 0.15f;
    public const float DefaultFloorThickness = 0.12f;

    // Maximum pre-pooled wall segments. Increase if rooms ever need more corners.
    private const int MaxWalls = 32;

    [Header("UV Settings")]
    [SerializeField] private float uvScale = 1f;

    [Header("Default Materials")]
    [Tooltip("Fallback floor material")]
    [SerializeField] private Material defaultFloorMat;
    [Tooltip("Fallback wall material")]
    [SerializeField] private Material defaultWallMat;

    [Header("Wall Border")]
    [SerializeField] private bool showWallBorders = true;
    [SerializeField] private float wallBorderWidth = 0.035f;
    [SerializeField] private float wallBorderOutset = 0.004f;
    [SerializeField] private Color wallBorderColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Camera Wall Visibility")]
    [SerializeField] private bool autoHideFrontWalls = true;
    [SerializeField] private Camera visibilityCamera;
    [Range(0f, 0.9f)]
    [SerializeField] private float frontWallHideDotThreshold = 0.15f;
    [SerializeField] private bool debugWallVisibility;

    // ── Floor ──
    private GameObject floorObject;
    private MeshFilter floorMeshFilter;
    private MeshRenderer floorMeshRenderer;
    private MeshCollider floorMeshCollider;

    // ── Per-wall pools ──
    private GameObject[] wallObjects;
    private MeshFilter[] wallMeshFilters;
    private MeshRenderer[] wallMeshRenderers;
    private BoxCollider[] wallBoxColliders;
    private GameObject[] wallTopBorders;
    private GameObject[] wallStartBorders;
    private GameObject[] wallEndBorders;
    private MeshRenderer[] wallTopBorderRenderers;
    private MeshRenderer[] wallStartBorderRenderers;
    private MeshRenderer[] wallEndBorderRenderers;
    private Material wallBorderMaterial;

    // Cached RoomData reference
    private RoomData roomData;

    private void LateUpdate()
    {
        UpdateCameraWallVisibility();
    }

    private void Awake()
    {
        SetupFloor();
        SetupWallPool();
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Subscribes to RoomData events and triggers an initial full mesh build.
    /// Call this once after the RoomData instance is created.
    /// </summary>
    public void Initialize(RoomData data)
    {
        if (roomData != null)
            roomData.OnGeometryChanged -= OnGeometryChanged;

        roomData = data;
        roomData.OnGeometryChanged += OnGeometryChanged;
        FullRebuild();
    }

    /// <summary>
    /// Direct entry point used by RoomEditorController's existing call pattern.
    /// Stores the reference and does a full rebuild.
    /// </summary>
    public void GenerateMeshes(RoomData data)
    {
        if (data == null || data.Corners.Count < 3) return;
        if (roomData != data)
        {
            if (roomData != null) roomData.OnGeometryChanged -= OnGeometryChanged;
            roomData = data;
            roomData.OnGeometryChanged += OnGeometryChanged;
        }
        FullRebuild();
    }

    public Renderer GetFloorRenderer() => floorMeshRenderer;

    public List<Renderer> GetWallRenderers()
    {
        var list = new List<Renderer>();
        if (roomData == null) return list;
        int count = Mathf.Min(roomData.Corners.Count, MaxWalls);
        for (int i = 0; i < count; i++)
            if (wallMeshRenderers[i] != null)
                list.Add(wallMeshRenderers[i]);
        return list;
    }

    /// <summary>Returns the BoxCollider for a given edge index (used by WallInteraction).</summary>
    public BoxCollider GetWallCollider(int edgeIndex)
    {
        if (edgeIndex < 0 || edgeIndex >= MaxWalls) return null;
        return wallBoxColliders[edgeIndex];
    }

    /// <summary>Returns the edge index for a BoxCollider (used by WallInteraction hit lookup).</summary>
    public int GetEdgeIndexForCollider(BoxCollider col)
    {
        for (int i = 0; i < MaxWalls; i++)
            if (wallBoxColliders[i] == col) return i;
        return -1;
    }

    public bool IsWallVisible(int edgeIndex)
    {
        if (edgeIndex < 0 || edgeIndex >= MaxWalls) return false;
        return wallMeshRenderers[edgeIndex] != null && wallMeshRenderers[edgeIndex].enabled;
    }

    // ─────────────────────────────────────────────
    //  Event handler
    // ─────────────────────────────────────────────

    private void OnGeometryChanged()
    {
        if (roomData == null || roomData.Corners.Count < 3) return;

        int changed = roomData.LastChangedCornerIndex;
        if (changed >= 0)
        {
            // Selective: rebuild only the two walls adjacent to the moved corner
            int n = roomData.Corners.Count;
            int prev = (changed - 1 + n) % n;
            RebuildWall(prev);
            RebuildWall(changed);
            BuildFloorMesh();
        }
        else
        {
            FullRebuild();
        }
    }

    // ─────────────────────────────────────────────
    //  Setup helpers
    // ─────────────────────────────────────────────

    private void SetupFloor()
    {
        floorObject = new GameObject("Floor");
        floorObject.transform.SetParent(transform);
        floorObject.transform.localPosition = Vector3.zero;

        floorMeshFilter   = floorObject.AddComponent<MeshFilter>();
        floorMeshRenderer = floorObject.AddComponent<MeshRenderer>();
        floorMeshCollider = floorObject.AddComponent<MeshCollider>();

        AssignMaterial(floorMeshRenderer, defaultFloorMat, "Default_Floor_Mat", new Color(0.7f, 0.7f, 0.7f));
    }

    private void SetupWallPool()
    {
        wallObjects       = new GameObject[MaxWalls];
        wallMeshFilters   = new MeshFilter[MaxWalls];
        wallMeshRenderers = new MeshRenderer[MaxWalls];
        wallBoxColliders  = new BoxCollider[MaxWalls];
        wallTopBorders = new GameObject[MaxWalls];
        wallStartBorders = new GameObject[MaxWalls];
        wallEndBorders = new GameObject[MaxWalls];
        wallTopBorderRenderers = new MeshRenderer[MaxWalls];
        wallStartBorderRenderers = new MeshRenderer[MaxWalls];
        wallEndBorderRenderers = new MeshRenderer[MaxWalls];
    }

    private void EnsureWallObject(int index)
    {
        if (wallObjects[index] != null) return;

        var go = new GameObject($"Wall_{index}");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        wallObjects[index]       = go;
        wallMeshFilters[index]   = go.AddComponent<MeshFilter>();
        wallMeshRenderers[index] = go.AddComponent<MeshRenderer>();
        wallBoxColliders[index]  = go.AddComponent<BoxCollider>();

        AssignMaterial(wallMeshRenderers[index], defaultWallMat, "Default_Wall_Mat", Color.white);
        CreateWallBorderObjects(index, go.transform);
    }

    private void AssignMaterial(MeshRenderer target, Material preferred, string fallbackName, Color fallbackColor)
    {
        if (preferred != null) { target.sharedMaterial = preferred; return; }
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
        {
            name  = fallbackName,
            color = fallbackColor
        };
        target.sharedMaterial = mat;
    }

    private void CreateWallBorderObjects(int index, Transform parent)
    {
        wallTopBorders[index] = CreateBorderCube(parent, "TopBorder", out wallTopBorderRenderers[index]);
        wallStartBorders[index] = CreateBorderCube(parent, "StartBorder", out wallStartBorderRenderers[index]);
        wallEndBorders[index] = CreateBorderCube(parent, "EndBorder", out wallEndBorderRenderers[index]);
    }

    private GameObject CreateBorderCube(Transform parent, string name, out MeshRenderer meshRenderer)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }

        meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = GetWallBorderMaterial();
        return go;
    }

    private Material GetWallBorderMaterial()
    {
        if (wallBorderMaterial != null)
            return wallBorderMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        wallBorderMaterial = new Material(shader)
        {
            name = "Generated_Wall_Border_Mat",
            color = wallBorderColor
        };
        if (wallBorderMaterial.HasProperty("_BaseColor"))
            wallBorderMaterial.SetColor("_BaseColor", wallBorderColor);
        if (wallBorderMaterial.HasProperty("_Color"))
            wallBorderMaterial.SetColor("_Color", wallBorderColor);
        return wallBorderMaterial;
    }

    // ─────────────────────────────────────────────
    //  Build logic
    // ─────────────────────────────────────────────

    private void FullRebuild()
    {
        BuildFloorMesh();
        int n = roomData.Corners.Count;
        for (int i = 0; i < MaxWalls; i++)
        {
            if (i < n)
            {
                EnsureWallObject(i);
                wallObjects[i].SetActive(true);
                RebuildWall(i);
            }
            else
            {
                if (wallObjects[i] != null)
                    wallObjects[i].SetActive(false);
            }
        }
        UpdateCameraWallVisibility();
    }

    private void BuildFloorMesh()
    {
        var corners = roomData.Corners;
        int n = corners.Count;
        float thickness = Mathf.Max(0.01f, roomData.FloorThickness);
        var verts = new Vector3[n * 2];
        var uvs   = new Vector2[n * 2];

        for (int i = 0; i < n; i++)
        {
            Vector3 top = transform.InverseTransformPoint(corners[i]);
            Vector3 bottom = top + Vector3.down * thickness;
            verts[i] = top;
            verts[i + n] = bottom;
            uvs[i]   = new Vector2(verts[i].x / uvScale, verts[i].z / uvScale);
            uvs[i + n] = uvs[i];
        }

        int[] topTris = TriangulatePolygon(corners);
        var tris = new List<int>(topTris.Length * 2 + n * 6);
        tris.AddRange(topTris);

        for (int i = 0; i < topTris.Length; i += 3)
        {
            tris.Add(topTris[i] + n);
            tris.Add(topTris[i + 2] + n);
            tris.Add(topTris[i + 1] + n);
        }

        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            int topA = i;
            int topB = next;
            int bottomA = i + n;
            int bottomB = next + n;

            tris.Add(topA);
            tris.Add(bottomA);
            tris.Add(topB);
            tris.Add(topB);
            tris.Add(bottomA);
            tris.Add(bottomB);
        }

        var mesh = floorMeshFilter.sharedMesh;
        if (mesh == null) mesh = new Mesh { name = "Floor_Mesh" };
        mesh.Clear();
        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        floorMeshFilter.sharedMesh = mesh;
        floorMeshCollider.sharedMesh = null;
        floorMeshCollider.sharedMesh = mesh;
    }

    private void RebuildWall(int edgeIndex)
    {
        if (edgeIndex < 0 || edgeIndex >= roomData.Corners.Count) return;
        EnsureWallObject(edgeIndex);

        var corners = roomData.Corners;
        int n       = corners.Count;
        int nextIdx = (edgeIndex + 1) % n;

        Vector3 worldStart = corners[edgeIndex];
        Vector3 worldEnd   = corners[nextIdx];
        Vector3 dir        = worldEnd - worldStart;
        float edgeLength   = dir.magnitude;
        if (edgeLength < 0.001f) return;
        dir /= edgeLength;

        // Inward normal (CCW polygon: interior is to the left of the edge direction)
        Vector3 inward = new Vector3(-dir.z, 0f, dir.x);
        float height   = roomData.RoomHeight;

        float wallThickness = Mathf.Max(0.01f, roomData.WallThickness);

        Vector3 wallCenter = (worldStart + worldEnd) * 0.5f
                           + inward * (wallThickness * 0.5f)
                           + Vector3.up * (height * 0.5f);

        Transform wallTransform = wallObjects[edgeIndex].transform;
        wallTransform.position = wallCenter;
        wallTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        wallTransform.localScale = Vector3.one;

        // 12 vertices: outer face | inner face | top cap.
        // Local axes: X = thickness, Y = height, Z = edge length.
        float halfThickness = wallThickness * 0.5f;
        float halfHeight = height * 0.5f;
        float halfLength = edgeLength * 0.5f;
        var verts = new Vector3[]
        {
            new Vector3( halfThickness, -halfHeight, -halfLength), // 0 outer bot L
            new Vector3( halfThickness, -halfHeight,  halfLength), // 1 outer bot R
            new Vector3( halfThickness,  halfHeight, -halfLength), // 2 outer top L
            new Vector3( halfThickness,  halfHeight,  halfLength), // 3 outer top R

            new Vector3(-halfThickness, -halfHeight,  halfLength), // 4 inner bot R
            new Vector3(-halfThickness, -halfHeight, -halfLength), // 5 inner bot L
            new Vector3(-halfThickness,  halfHeight,  halfLength), // 6 inner top R
            new Vector3(-halfThickness,  halfHeight, -halfLength), // 7 inner top L

            new Vector3( halfThickness,  halfHeight, -halfLength), // 8 top cap outer L (= 2)
            new Vector3( halfThickness,  halfHeight,  halfLength), // 9 top cap outer R (= 3)
            new Vector3(-halfThickness,  halfHeight, -halfLength), // 10 top cap inner L (= 7)
            new Vector3(-halfThickness,  halfHeight,  halfLength), // 11 top cap inner R (= 6)
        };

        float wU = edgeLength / uvScale;
        float hU = height / uvScale;
        float tU = wallThickness / uvScale;
        var uvs = new Vector2[]
        {
            new(0,0), new(wU,0), new(0,hU), new(wU,hU),   // outer
            new(0,0), new(wU,0), new(0,hU), new(wU,hU),   // inner
            new(0,0), new(wU,0), new(0,tU), new(wU,tU),   // top cap
        };

        var tris = new int[]
        {
            0,2,1,  1,2,3,     // outer face
            4,6,5,  5,6,7,     // inner face
            8,10,9, 9,10,11,   // top cap
        };

        var mesh = wallMeshFilters[edgeIndex].sharedMesh;
        if (mesh == null) mesh = new Mesh { name = $"Wall_{edgeIndex}_Mesh" };
        mesh.Clear();
        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        wallMeshFilters[edgeIndex].sharedMesh = mesh;

        // ── BoxCollider: orient the wall GO to face the edge, then size it ──
        wallBoxColliders[edgeIndex].center = Vector3.zero;
        wallBoxColliders[edgeIndex].size   = new Vector3(wallThickness, height, edgeLength);
        UpdateWallBorders(edgeIndex, wallThickness, height, edgeLength);
    }

    private void UpdateWallBorders(int edgeIndex, float wallThickness, float height, float edgeLength)
    {
        float borderWidth = Mathf.Max(0.005f, wallBorderWidth);
        float halfHeight = height * 0.5f;
        float halfLength = edgeLength * 0.5f;
        float visualThickness = wallThickness + wallBorderOutset * 2f;

        ConfigureBorder(wallTopBorders[edgeIndex], wallTopBorderRenderers[edgeIndex],
            new Vector3(0f, halfHeight + borderWidth * 0.5f + wallBorderOutset, 0f),
            new Vector3(visualThickness, borderWidth, edgeLength + borderWidth));

        ConfigureBorder(wallStartBorders[edgeIndex], wallStartBorderRenderers[edgeIndex],
            new Vector3(0f, 0f, -halfLength),
            new Vector3(visualThickness, height + borderWidth, borderWidth));

        ConfigureBorder(wallEndBorders[edgeIndex], wallEndBorderRenderers[edgeIndex],
            new Vector3(0f, 0f, halfLength),
            new Vector3(visualThickness, height + borderWidth, borderWidth));
    }

    private void ConfigureBorder(GameObject border, MeshRenderer renderer, Vector3 localPosition, Vector3 localScale)
    {
        if (border == null) return;

        border.SetActive(showWallBorders);
        border.transform.localPosition = localPosition;
        border.transform.localRotation = Quaternion.identity;
        border.transform.localScale = localScale;

        if (renderer != null)
        {
            renderer.sharedMaterial = GetWallBorderMaterial();
            renderer.enabled = showWallBorders;
        }
    }

    private void UpdateCameraWallVisibility()
    {
        if (!autoHideFrontWalls || roomData == null || roomData.Corners.Count < 3)
        {
            SetAllWallsVisible();
            return;
        }

        if (visibilityCamera == null)
            visibilityCamera = Camera.main;
        if (visibilityCamera == null) return;

        Vector3 center = GetRoomCenter();
        Vector3 centerToCamera = visibilityCamera.transform.position - center;
        centerToCamera.y = 0f;
        if (centerToCamera.sqrMagnitude < 0.0001f) return;
        centerToCamera.Normalize();

        int count = Mathf.Min(roomData.Corners.Count, MaxWalls);
        for (int i = 0; i < count; i++)
        {
            MeshRenderer wallRenderer = wallMeshRenderers[i];
            if (wallRenderer == null) continue;

            Transform wallTransform = wallObjects[i] != null ? wallObjects[i].transform : null;
            if (wallTransform == null) continue;

            Vector3 centerToWall = wallTransform.position - center;
            centerToWall.y = 0f;
            if (centerToWall.sqrMagnitude < 0.0001f)
            {
                wallRenderer.enabled = true;
                continue;
            }
            centerToWall.Normalize();

            bool hide = Vector3.Dot(centerToCamera, centerToWall) > frontWallHideDotThreshold;
            SetWallVisualVisible(i, !hide);

            if (debugWallVisibility)
                Debug.DrawRay(wallTransform.position, centerToWall * 1.2f, hide ? Color.red : Color.cyan);
        }
    }

    private void SetAllWallsVisible()
    {
        if (wallMeshRenderers == null) return;
        for (int i = 0; i < wallMeshRenderers.Length; i++)
            SetWallVisualVisible(i, true);
    }

    private void SetWallVisualVisible(int index, bool visible)
    {
        if (index < 0 || index >= MaxWalls) return;

        if (wallMeshRenderers[index] != null)
            wallMeshRenderers[index].enabled = visible;

        bool borderVisible = visible && showWallBorders;
        SetRendererVisible(wallTopBorderRenderers, index, borderVisible);
        SetRendererVisible(wallStartBorderRenderers, index, borderVisible);
        SetRendererVisible(wallEndBorderRenderers, index, borderVisible);
    }

    private void SetRendererVisible(MeshRenderer[] renderers, int index, bool visible)
    {
        if (renderers == null || index < 0 || index >= renderers.Length) return;
        if (renderers[index] != null)
            renderers[index].enabled = visible;
    }

    private Vector3 GetRoomCenter()
    {
        Vector3 min = roomData.Corners[0];
        Vector3 max = roomData.Corners[0];
        for (int i = 1; i < roomData.Corners.Count; i++)
        {
            min = Vector3.Min(min, roomData.Corners[i]);
            max = Vector3.Max(max, roomData.Corners[i]);
        }
        return (min + max) * 0.5f;
    }

    // ─────────────────────────────────────────────
    //  Ear Clipping
    // ─────────────────────────────────────────────

    private int[] TriangulatePolygon(List<Vector3> corners)
    {
        var tris = new List<int>();
        var indices = new List<int>();
        for (int i = 0; i < corners.Count; i++) indices.Add(i);

        bool isClockwise = SignedAreaXZ(corners) < 0f;
        int safety = corners.Count * corners.Count;
        int iter = 0;
        while (indices.Count > 3 && iter++ < safety)
        {
            bool found = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int pi = indices[(i - 1 + indices.Count) % indices.Count];
                int ci = indices[i];
                int ni = indices[(i + 1) % indices.Count];
                if (IsEar(pi, ci, ni, corners, indices, isClockwise))
                {
                    AddUpwardTriangle(tris, pi, ci, ni);
                    indices.RemoveAt(i);
                    found = true;
                    break;
                }
            }
            if (!found) { Debug.LogWarning("[RoomMeshGenerator] Ear clipping stalled."); break; }
        }
        if (indices.Count == 3)
            AddUpwardTriangle(tris, indices[0], indices[1], indices[2]);
        return tris.ToArray();
    }

    private float SignedAreaXZ(List<Vector3> points)
    {
        float area = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[(i + 1) % points.Count];
            area += a.x * b.z - b.x * a.z;
        }
        return area * 0.5f;
    }

    private bool IsEar(int prevIndex, int currIndex, int nextIndex, List<Vector3> all, List<int> rem, bool polygonClockwise)
    {
        Vector3 prev = all[prevIndex];
        Vector3 curr = all[currIndex];
        Vector3 next = all[nextIndex];
        float cross = CrossXZ(prev, curr, next);
        if (polygonClockwise ? cross >= -0.0001f : cross <= 0.0001f) return false;

        foreach (int idx in rem)
        {
            if (idx == prevIndex || idx == currIndex || idx == nextIndex) continue;
            Vector3 p = all[idx];
            if (PointInTriangle(p, prev, curr, next)) return false;
        }
        return true;
    }

    private float CrossXZ(Vector3 a, Vector3 b, Vector3 c)
    {
        return (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
    }

    private void AddUpwardTriangle(List<int> tris, int a, int b, int c)
    {
        tris.Add(a);
        tris.Add(c);
        tris.Add(b);
    }

    private bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        float denom = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
        if (Mathf.Abs(denom) < 0.0001f) return false;
        float al = ((b.z - c.z) * (p.x - c.x) + (c.x - b.x) * (p.z - c.z)) / denom;
        float be = ((c.z - a.z) * (p.x - c.x) + (a.x - c.x) * (p.z - c.z)) / denom;
        float ga = 1f - al - be;
        return al > 0.001f && be > 0.001f && ga > 0.001f;
    }

    private void OnDestroy()
    {
        if (roomData != null)
            roomData.OnGeometryChanged -= OnGeometryChanged;
    }
}
