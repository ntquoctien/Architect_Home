using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates procedural meshes for room floor and walls from RoomData.
/// Supports non-convex polygons via Ear Clipping triangulation.
/// Walls are pooled per-segment with BoxColliders. Wall thickness is a fixed constant.
/// </summary>
public class RoomMeshGenerator : MonoBehaviour
{
    // ── Wall thickness is a fixed constant — not configurable (by design) ──
    public const float WallThickness = 0.15f;

    // Maximum pre-pooled wall segments. Increase if rooms ever need more corners.
    private const int MaxWalls = 32;

    [Header("UV Settings")]
    [SerializeField] private float uvScale = 1f;

    [Header("Default Materials")]
    [Tooltip("Fallback floor material")]
    [SerializeField] private Material defaultFloorMat;
    [Tooltip("Fallback wall material")]
    [SerializeField] private Material defaultWallMat;

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

    // Cached RoomData reference
    private RoomData roomData;

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

        for (int i = 0; i < MaxWalls; i++)
        {
            var go = new GameObject($"Wall_{i}");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.SetActive(false);

            wallObjects[i]       = go;
            wallMeshFilters[i]   = go.AddComponent<MeshFilter>();
            wallMeshRenderers[i] = go.AddComponent<MeshRenderer>();
            wallBoxColliders[i]  = go.AddComponent<BoxCollider>();

            AssignMaterial(wallMeshRenderers[i], defaultWallMat, "Default_Wall_Mat", Color.white);
        }
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
                wallObjects[i].SetActive(true);
                RebuildWall(i);
            }
            else
            {
                wallObjects[i].SetActive(false);
            }
        }
    }

    private void BuildFloorMesh()
    {
        var corners = roomData.Corners;
        var verts = new Vector3[corners.Count];
        var uvs   = new Vector2[corners.Count];

        for (int i = 0; i < corners.Count; i++)
        {
            verts[i] = transform.InverseTransformPoint(corners[i]);
            uvs[i]   = new Vector2(verts[i].x / uvScale, verts[i].z / uvScale);
        }

        var mesh = floorMeshFilter.sharedMesh;
        if (mesh == null) mesh = new Mesh { name = "Floor_Mesh" };
        mesh.Clear();
        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.triangles = TriangulatePolygon(corners);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        floorMeshFilter.sharedMesh = mesh;
        floorMeshCollider.sharedMesh = null;
        floorMeshCollider.sharedMesh = mesh;
    }

    private void RebuildWall(int edgeIndex)
    {
        if (edgeIndex < 0 || edgeIndex >= roomData.Corners.Count) return;

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

        Vector3 Ls(Vector3 w) => transform.InverseTransformPoint(w);

        // 12 vertices: outer face | inner face | top cap
        var verts = new Vector3[]
        {
            Ls(worldStart),                                                   // 0 outer bot L
            Ls(worldEnd),                                                     // 1 outer bot R
            Ls(worldStart + Vector3.up * height),                             // 2 outer top L
            Ls(worldEnd   + Vector3.up * height),                             // 3 outer top R

            Ls(worldEnd   + inward * WallThickness),                          // 4 inner bot R
            Ls(worldStart + inward * WallThickness),                          // 5 inner bot L
            Ls(worldEnd   + inward * WallThickness + Vector3.up * height),    // 6 inner top R
            Ls(worldStart + inward * WallThickness + Vector3.up * height),    // 7 inner top L

            Ls(worldStart + Vector3.up * height),                             // 8 top cap outer L (= 2)
            Ls(worldEnd   + Vector3.up * height),                             // 9 top cap outer R (= 3)
            Ls(worldStart + inward * WallThickness + Vector3.up * height),    // 10 top cap inner L (= 7)
            Ls(worldEnd   + inward * WallThickness + Vector3.up * height),    // 11 top cap inner R (= 6)
        };

        float wU = edgeLength / uvScale;
        float hU = height / uvScale;
        float tU = WallThickness / uvScale;
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
        Vector3 wallCenter = (worldStart + worldEnd) * 0.5f
                           + inward * (WallThickness * 0.5f)
                           + Vector3.up * (height * 0.5f);
        wallObjects[edgeIndex].transform.position = wallCenter;
        wallObjects[edgeIndex].transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        wallBoxColliders[edgeIndex].center = Vector3.zero;
        wallBoxColliders[edgeIndex].size   = new Vector3(edgeLength, height, WallThickness);
    }

    // ─────────────────────────────────────────────
    //  Ear Clipping
    // ─────────────────────────────────────────────

    private int[] TriangulatePolygon(List<Vector3> corners)
    {
        var tris    = new List<int>();
        var indices = new List<int>();
        for (int i = 0; i < corners.Count; i++) indices.Add(i);

        int safety = corners.Count * 3;
        int iter   = 0;
        while (indices.Count > 3 && iter++ < safety)
        {
            bool found = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int pi = indices[(i - 1 + indices.Count) % indices.Count];
                int ci = indices[i];
                int ni = indices[(i + 1) % indices.Count];
                if (IsEar(corners[pi], corners[ci], corners[ni], corners, indices))
                {
                    tris.Add(pi); tris.Add(ci); tris.Add(ni);
                    indices.RemoveAt(i);
                    found = true;
                    break;
                }
            }
            if (!found) { Debug.LogWarning("[RoomMeshGenerator] Ear clipping stalled."); break; }
        }
        if (indices.Count == 3) { tris.Add(indices[0]); tris.Add(indices[1]); tris.Add(indices[2]); }
        return tris.ToArray();
    }

    private bool IsEar(Vector3 prev, Vector3 curr, Vector3 next, List<Vector3> all, List<int> rem)
    {
        // Cross-product check: CCW ear has positive cross product
        float cross = (prev.x - curr.x) * (next.z - curr.z) - (prev.z - curr.z) * (next.x - curr.x);
        if (cross <= 0f) return false;
        foreach (int idx in rem)
        {
            Vector3 p = all[idx];
            if (p == prev || p == curr || p == next) continue;
            if (PointInTriangle(p, prev, curr, next)) return false;
        }
        return true;
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
