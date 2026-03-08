using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates procedural meshes for room floor and walls using Ear Clipping triangulation.
/// Supports non-convex polygons (L-shapes, etc.).
/// Includes corner post generation to fill gaps at wall joints.
/// </summary>
public class RoomMeshGenerator : MonoBehaviour
{
    [Header("Mesh Settings")]
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private float wallThickness = 0.1f;
    [SerializeField] private bool extrudeWallsInward = true;
    [SerializeField] private bool generateWallCaps = true;
    [SerializeField] private bool generateCornerPosts = true;

    [Header("UV Settings")]
    [SerializeField] private float uvScale = 1f; // Units per UV tile

    [Header("Default Materials")]
    [Tooltip("Fallback material for floor if none assigned")]
    [SerializeField] private Material defaultFloorMat;
    [Tooltip("Fallback material for walls if none assigned")]
    [SerializeField] private Material defaultWallMat;

    [Header("Mesh Objects")]
    [SerializeField] private GameObject floorObject;
    [SerializeField] private GameObject wallsObject;

    private MeshFilter floorMeshFilter;
    private MeshRenderer floorMeshRenderer;
    private MeshCollider floorMeshCollider;
    private MeshFilter wallsMeshFilter;
    private MeshRenderer wallsMeshRenderer;
    private MeshCollider wallsMeshCollider;

    private void Awake()
    {
        SetupMeshObjects();
    }

    /// <summary>
    /// Sets up the GameObject hierarchy for floor and walls with proper components.
    /// </summary>
    private void SetupMeshObjects()
    {
        // ===== FLOOR SETUP =====
        if (floorObject == null)
        {
            floorObject = new GameObject("Floor");
            floorObject.transform.SetParent(transform);
            floorObject.transform.localPosition = Vector3.zero;
        }
        
        // MeshFilter
        floorMeshFilter = floorObject.GetComponent<MeshFilter>();
        if (floorMeshFilter == null)
            floorMeshFilter = floorObject.AddComponent<MeshFilter>();
        
        // MeshRenderer
        floorMeshRenderer = floorObject.GetComponent<MeshRenderer>();
        if (floorMeshRenderer == null)
            floorMeshRenderer = floorObject.AddComponent<MeshRenderer>();

        // MeshCollider for raycasts
        floorMeshCollider = floorObject.GetComponent<MeshCollider>();
        if (floorMeshCollider == null)
            floorMeshCollider = floorObject.AddComponent<MeshCollider>();

        // Assign default floor material if none exists
        if (floorMeshRenderer.sharedMaterial == null)
        {
            if (defaultFloorMat != null)
            {
                floorMeshRenderer.sharedMaterial = defaultFloorMat;
            }
            else
            {
                // Create a simple default material
                Material fallbackFloor = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (fallbackFloor.shader == null)
                    fallbackFloor = new Material(Shader.Find("Standard"));
                fallbackFloor.name = "Default_Floor_Mat";
                fallbackFloor.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                floorMeshRenderer.sharedMaterial = fallbackFloor;
            }
        }

        // ===== WALLS SETUP =====
        if (wallsObject == null)
        {
            wallsObject = new GameObject("Walls");
            wallsObject.transform.SetParent(transform);
            wallsObject.transform.localPosition = Vector3.zero;
        }
        
        // MeshFilter
        wallsMeshFilter = wallsObject.GetComponent<MeshFilter>();
        if (wallsMeshFilter == null)
            wallsMeshFilter = wallsObject.AddComponent<MeshFilter>();
        
        // MeshRenderer
        wallsMeshRenderer = wallsObject.GetComponent<MeshRenderer>();
        if (wallsMeshRenderer == null)
            wallsMeshRenderer = wallsObject.AddComponent<MeshRenderer>();

        // MeshCollider for raycasts
        wallsMeshCollider = wallsObject.GetComponent<MeshCollider>();
        if (wallsMeshCollider == null)
            wallsMeshCollider = wallsObject.AddComponent<MeshCollider>();

        // Assign default wall material if none exists
        if (wallsMeshRenderer.sharedMaterial == null)
        {
            if (defaultWallMat != null)
            {
                wallsMeshRenderer.sharedMaterial = defaultWallMat;
            }
            else
            {
                // Create a simple default material
                Material fallbackWall = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (fallbackWall.shader == null)
                    fallbackWall = new Material(Shader.Find("Standard"));
                fallbackWall.name = "Default_Wall_Mat";
                fallbackWall.color = Color.white;
                wallsMeshRenderer.sharedMaterial = fallbackWall;
            }
        }
    }

    /// <summary>
    /// Main entry point: Generates both floor and wall meshes from room data.
    /// </summary>
    public void GenerateMeshes(RoomData roomData)
    {
        if (roomData == null || roomData.Corners.Count < 3)
        {
            Debug.LogWarning("Invalid room data for mesh generation");
            return;
        }

        // Ensure objects are set up
        if (floorMeshFilter == null || wallsMeshFilter == null)
        {
            SetupMeshObjects();
        }

        GenerateFloorMesh(roomData);
        GenerateWallMesh(roomData);
    }

    /// <summary>
    /// Generates the floor mesh using Ear Clipping triangulation.
    /// Also assigns mesh to MeshCollider for raycasting.
    /// </summary>
    private void GenerateFloorMesh(RoomData roomData)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Floor_Mesh";

        List<Vector3> corners = roomData.Corners;
        
        // Create vertices (corners on Y=0)
        Vector3[] vertices = new Vector3[corners.Count];
        for (int i = 0; i < corners.Count; i++)
        {
            vertices[i] = transform.InverseTransformPoint(corners[i]);
        }

        // Triangulate using Ear Clipping
        int[] triangles = TriangulatePolygon(corners);

        // Generate UVs based on world-space XZ coordinates
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / uvScale, vertices[i].z / uvScale);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        floorMeshFilter.mesh = mesh;

        // Assign mesh to collider for raycast detection
        if (floorMeshCollider != null)
        {
            floorMeshCollider.sharedMesh = null; // Clear first to force update
            floorMeshCollider.sharedMesh = mesh;
        }
    }

    /// <summary>
    /// Triangulates a polygon using the Ear Clipping algorithm.
    /// Supports non-convex polygons.
    /// </summary>
    private int[] TriangulatePolygon(List<Vector3> corners)
    {
        List<int> triangles = new List<int>();
        List<int> indices = new List<int>();
        
        // Initialize indices
        for (int i = 0; i < corners.Count; i++)
        {
            indices.Add(i);
        }

        int iterations = 0;
        int maxIterations = corners.Count * 3; // Safety check

        // Ear clipping algorithm
        while (indices.Count > 3 && iterations < maxIterations)
        {
            bool earFound = false;

            for (int i = 0; i < indices.Count; i++)
            {
                int prevIndex = indices[(i - 1 + indices.Count) % indices.Count];
                int currIndex = indices[i];
                int nextIndex = indices[(i + 1) % indices.Count];

                Vector3 prev = corners[prevIndex];
                Vector3 curr = corners[currIndex];
                Vector3 next = corners[nextIndex];

                // Check if this forms a valid ear
                if (IsEar(prev, curr, next, corners, indices))
                {
                    // Add triangle
                    triangles.Add(prevIndex);
                    triangles.Add(currIndex);
                    triangles.Add(nextIndex);

                    // Remove the ear vertex
                    indices.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }

            if (!earFound)
            {
                Debug.LogWarning("Ear clipping failed - no valid ear found. Using fallback triangulation.");
                break;
            }

            iterations++;
        }

        // Add remaining triangle
        if (indices.Count == 3)
        {
            triangles.Add(indices[0]);
            triangles.Add(indices[1]);
            triangles.Add(indices[2]);
        }

        return triangles.ToArray();
    }

    /// <summary>
    /// Checks if three consecutive vertices form a valid ear.
    /// </summary>
    private bool IsEar(Vector3 prev, Vector3 curr, Vector3 next, List<Vector3> allCorners, List<int> remainingIndices)
    {
        // Check if the angle is convex (using cross product in XZ plane)
        Vector2 v1 = new Vector2(prev.x - curr.x, prev.z - curr.z);
        Vector2 v2 = new Vector2(next.x - curr.x, next.z - curr.z);
        float cross = v1.x * v2.y - v1.y * v2.x;
        
        if (cross <= 0) return false; // Reflex angle

        // Check if any other vertex is inside this triangle
        for (int i = 0; i < remainingIndices.Count; i++)
        {
            int idx = remainingIndices[i];
            Vector3 point = allCorners[idx];
            
            if (point == prev || point == curr || point == next)
                continue;

            if (IsPointInTriangle(point, prev, curr, next))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a point is inside a triangle using barycentric coordinates.
    /// </summary>
    private bool IsPointInTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector2 p = new Vector2(point.x, point.z);
        Vector2 a2 = new Vector2(a.x, a.z);
        Vector2 b2 = new Vector2(b.x, b.z);
        Vector2 c2 = new Vector2(c.x, c.z);

        float denominator = ((b2.y - c2.y) * (a2.x - c2.x) + (c2.x - b2.x) * (a2.y - c2.y));
        if (Mathf.Abs(denominator) < 0.0001f) return false;

        float alpha = ((b2.y - c2.y) * (p.x - c2.x) + (c2.x - b2.x) * (p.y - c2.y)) / denominator;
        float beta = ((c2.y - a2.y) * (p.x - c2.x) + (a2.x - c2.x) * (p.y - c2.y)) / denominator;
        float gamma = 1.0f - alpha - beta;

        return alpha > 0.001f && beta > 0.001f && gamma > 0.001f;
    }

    /// <summary>
    /// Generates wall meshes with proper UVs, top caps, and corner posts.
    /// </summary>
    private void GenerateWallMesh(RoomData roomData)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Walls_Mesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        List<Vector3> corners = roomData.Corners;
        
        // Calculate wall offset direction (inward or outward)
        float offsetDirection = extrudeWallsInward ? -1f : 1f;

        // Generate each wall segment
        for (int i = 0; i < corners.Count; i++)
        {
            Vector3 start = corners[i];
            Vector3 end = corners[(i + 1) % corners.Count];
            
            Vector3 wallDir = (end - start).normalized;
            Vector3 wallNormal = new Vector3(-wallDir.z, 0, wallDir.x) * offsetDirection;
            
            float wallLength = Vector3.Distance(start, end);

            // Calculate wall corners
            Vector3 innerStart = start;
            Vector3 innerEnd = end;
            Vector3 outerStart = start + wallNormal * wallThickness;
            Vector3 outerEnd = end + wallNormal * wallThickness;

            // Convert to local space
            innerStart = transform.InverseTransformPoint(innerStart);
            innerEnd = transform.InverseTransformPoint(innerEnd);
            outerStart = transform.InverseTransformPoint(outerStart);
            outerEnd = transform.InverseTransformPoint(outerEnd);

            // Generate outer wall face
            GenerateWallQuad(vertices, triangles, uvs,
                outerStart, outerEnd,
                outerStart + Vector3.up * wallHeight,
                outerEnd + Vector3.up * wallHeight,
                wallLength);

            // Generate inner wall face
            GenerateWallQuad(vertices, triangles, uvs,
                innerEnd, innerStart,
                innerEnd + Vector3.up * wallHeight,
                innerStart + Vector3.up * wallHeight,
                wallLength);

            // Generate top cap if enabled
            if (generateWallCaps)
            {
                GenerateWallQuad(vertices, triangles, uvs,
                    innerStart + Vector3.up * wallHeight,
                    innerEnd + Vector3.up * wallHeight,
                    outerStart + Vector3.up * wallHeight,
                    outerEnd + Vector3.up * wallHeight,
                    wallLength);
            }
        }

        // Generate corner posts to fill gaps between wall segments
        if (generateCornerPosts)
        {
            GenerateCornerPosts(vertices, triangles, uvs, corners, offsetDirection);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        wallsMeshFilter.mesh = mesh;

        // Assign mesh to collider for raycast detection
        if (wallsMeshCollider != null)
        {
            wallsMeshCollider.sharedMesh = null; // Clear first to force update
            wallsMeshCollider.sharedMesh = mesh;
        }
    }

    /// <summary>
    /// Generates corner post pillars at each corner to fill gaps between wall segments.
    /// </summary>
    private void GenerateCornerPosts(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs,
        List<Vector3> corners, float offsetDirection)
    {
        for (int i = 0; i < corners.Count; i++)
        {
            Vector3 corner = corners[i];
            
            // Get adjacent wall directions
            int prevIdx = (i - 1 + corners.Count) % corners.Count;
            int nextIdx = (i + 1) % corners.Count;
            
            Vector3 prevCorner = corners[prevIdx];
            Vector3 nextCorner = corners[nextIdx];
            
            // Calculate normals for adjacent walls
            Vector3 dirToPrev = (prevCorner - corner).normalized;
            Vector3 dirToNext = (nextCorner - corner).normalized;
            
            Vector3 normalPrev = new Vector3(-dirToPrev.z, 0, dirToPrev.x) * offsetDirection;
            Vector3 normalNext = new Vector3(-dirToNext.z, 0, dirToNext.x) * offsetDirection;

            // Create corner post box vertices
            // The post fills the gap created by wall thickness at the corner
            Vector3 inner = corner;
            Vector3 outerA = corner + normalPrev * wallThickness;
            Vector3 outerB = corner + normalNext * wallThickness;
            Vector3 outerCorner = corner + (normalPrev + normalNext).normalized * wallThickness * 1.414f;

            // Generate corner post as a simple box
            GenerateCornerBox(vertices, triangles, uvs, 
                transform.InverseTransformPoint(corner),
                transform.InverseTransformPoint(outerA),
                transform.InverseTransformPoint(outerB),
                transform.InverseTransformPoint(outerCorner),
                wallHeight);
        }
    }

    /// <summary>
    /// Generates a corner box/pillar geometry to fill gaps at corners.
    /// </summary>
    private void GenerateCornerBox(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs,
        Vector3 inner, Vector3 outerA, Vector3 outerB, Vector3 outerCorner, float height)
    {
        // Bottom vertices (Y = 0)
        Vector3 b0 = inner;
        Vector3 b1 = outerA;
        Vector3 b2 = outerCorner;
        Vector3 b3 = outerB;

        // Top vertices (Y = height)
        Vector3 t0 = inner + Vector3.up * height;
        Vector3 t1 = outerA + Vector3.up * height;
        Vector3 t2 = outerCorner + Vector3.up * height;
        Vector3 t3 = outerB + Vector3.up * height;

        // Generate 4 side faces of the corner post
        // Face 1: inner to outerA
        GenerateQuadSimple(vertices, triangles, uvs, b0, b1, t0, t1);
        
        // Face 2: outerA to outerCorner
        GenerateQuadSimple(vertices, triangles, uvs, b1, b2, t1, t2);
        
        // Face 3: outerCorner to outerB
        GenerateQuadSimple(vertices, triangles, uvs, b2, b3, t2, t3);
        
        // Face 4: outerB to inner
        GenerateQuadSimple(vertices, triangles, uvs, b3, b0, t3, t0);

        // Top cap
        GenerateQuadSimple(vertices, triangles, uvs, t0, t1, t3, t2);
    }

    /// <summary>
    /// Generates a simple quad with basic UVs for corner posts.
    /// </summary>
    private void GenerateQuadSimple(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs,
        Vector3 bl, Vector3 br, Vector3 tl, Vector3 tr)
    {
        int startIndex = vertices.Count;

        vertices.Add(bl);
        vertices.Add(br);
        vertices.Add(tl);
        vertices.Add(tr);

        // Simple 0-1 UV mapping for corner posts
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1));

        // Two triangles for the quad
        triangles.Add(startIndex);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 1);

        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 3);
    }

    /// <summary>
    /// Helper method to generate a quad for wall segments with proper UVs.
    /// </summary>
    private void GenerateWallQuad(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs,
        Vector3 bottomLeft, Vector3 bottomRight, Vector3 topLeft, Vector3 topRight, float wallLength)
    {
        int startIndex = vertices.Count;

        vertices.Add(bottomLeft);
        vertices.Add(bottomRight);
        vertices.Add(topLeft);
        vertices.Add(topRight);

        // Generate UVs based on wall length and height
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(wallLength / uvScale, 0));
        uvs.Add(new Vector2(0, wallHeight / uvScale));
        uvs.Add(new Vector2(wallLength / uvScale, wallHeight / uvScale));

        // Two triangles for the quad
        triangles.Add(startIndex);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 1);

        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 3);
    }

    /// <summary>
    /// Returns the floor renderer for integration with RoomThemeController.
    /// </summary>
    public Renderer GetFloorRenderer()
    {
        return floorMeshRenderer;
    }

    /// <summary>
    /// Returns wall renderers list for integration with RoomThemeController.
    /// </summary>
    public List<Renderer> GetWallRenderers()
    {
        List<Renderer> wallRenderers = new List<Renderer>();
        if (wallsMeshRenderer != null)
        {
            wallRenderers.Add(wallsMeshRenderer);
        }
        return wallRenderers;
    }

    /// <summary>
    /// Returns the floor mesh for external use.
    /// </summary>
    public Mesh GetFloorMesh()
    {
        return floorMeshFilter != null ? floorMeshFilter.sharedMesh : null;
    }

    /// <summary>
    /// Returns the walls mesh for external use.
    /// </summary>
    public Mesh GetWallsMesh()
    {
        return wallsMeshFilter != null ? wallsMeshFilter.sharedMesh : null;
    }
}
