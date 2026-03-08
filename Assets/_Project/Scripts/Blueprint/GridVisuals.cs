using UnityEngine;

namespace ArchitectHome.Blueprint
{
    /// <summary>
    /// High-performance grid visualization using procedural mesh generation.
    /// Generates a SINGLE mesh with all grid lines as thin quads for optimal performance.
    /// No LineRenderer overhead - perfect for large grids.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class GridVisuals : MonoBehaviour
    {
        #region Configuration
        [Header("Visual Settings")]
        [Tooltip("Width of grid lines in world units")]
        [SerializeField] private float lineWidth = 0.02f;

        [Tooltip("Color of grid lines (requires material support)")]
        [SerializeField] private Color gridColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);

        [Tooltip("If true, regenerates grid on Start()")]
        [SerializeField] private bool generateOnStart = true;

        [Header("References")]
        [Tooltip("Material for grid rendering (Unlit/Color or Custom shader recommended)")]
        [SerializeField] private Material gridMaterial;
        #endregion

        #region Components
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh gridMesh;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            // Apply material if set
            if (gridMaterial != null)
            {
                meshRenderer.material = gridMaterial;
            }
        }

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateGridMesh();
            }
        }

        private void OnValidate()
        {
            lineWidth = Mathf.Max(0.001f, lineWidth);
        }
        #endregion

        #region Public API
        /// <summary>
        /// Generates or regenerates the complete grid mesh.
        /// Call this whenever grid dimensions change.
        /// </summary>
        public void GenerateGridMesh()
        {
            if (GridManager.Instance == null)
            {
                Debug.LogError("GridManager instance not found! Cannot generate grid visuals.");
                return;
            }

            // Clear existing mesh
            if (gridMesh != null)
            {
                gridMesh.Clear();
            }
            else
            {
                gridMesh = new Mesh();
                gridMesh.name = "ProceduralGridMesh";
            }

            // Get grid data from manager
            int width = GridManager.Instance.Width;
            int height = GridManager.Instance.Height;
            float cellSize = GridManager.Instance.CellSize;
            Vector3 origin = GridManager.Instance.OriginPosition;

            // Calculate total number of lines
            int verticalLines = width + 1;      // Lines along X axis
            int horizontalLines = height + 1;   // Lines along Z axis
            int totalLines = verticalLines + horizontalLines;

            // Pre-allocate arrays for mesh data
            // Each line is a quad (4 vertices, 6 indices)
            int vertexCount = totalLines * 4;
            int triangleCount = totalLines * 6;

            Vector3[] vertices = new Vector3[vertexCount];
            int[] triangles = new int[triangleCount];
            Vector2[] uvs = new Vector2[vertexCount];

            int vertIndex = 0;
            int triIndex = 0;

            float halfLineWidth = lineWidth * 0.5f;

            // Generate VERTICAL LINES (parallel to Z axis)
            for (int x = 0; x <= width; x++)
            {
                float worldX = origin.x + (x * cellSize);
                float startZ = origin.z;
                float endZ = origin.z + (height * cellSize);

                // Create quad vertices for this line
                vertices[vertIndex + 0] = new Vector3(worldX - halfLineWidth, origin.y, startZ);
                vertices[vertIndex + 1] = new Vector3(worldX + halfLineWidth, origin.y, startZ);
                vertices[vertIndex + 2] = new Vector3(worldX + halfLineWidth, origin.y, endZ);
                vertices[vertIndex + 3] = new Vector3(worldX - halfLineWidth, origin.y, endZ);

                // UVs for proper texture mapping
                uvs[vertIndex + 0] = new Vector2(0, 0);
                uvs[vertIndex + 1] = new Vector2(1, 0);
                uvs[vertIndex + 2] = new Vector2(1, 1);
                uvs[vertIndex + 3] = new Vector2(0, 1);

                // Triangle indices (two triangles per quad)
                triangles[triIndex + 0] = vertIndex + 0;
                triangles[triIndex + 1] = vertIndex + 2;
                triangles[triIndex + 2] = vertIndex + 1;

                triangles[triIndex + 3] = vertIndex + 0;
                triangles[triIndex + 4] = vertIndex + 3;
                triangles[triIndex + 5] = vertIndex + 2;

                vertIndex += 4;
                triIndex += 6;
            }

            // Generate HORIZONTAL LINES (parallel to X axis)
            for (int z = 0; z <= height; z++)
            {
                float worldZ = origin.z + (z * cellSize);
                float startX = origin.x;
                float endX = origin.x + (width * cellSize);

                // Create quad vertices for this line
                vertices[vertIndex + 0] = new Vector3(startX, origin.y, worldZ - halfLineWidth);
                vertices[vertIndex + 1] = new Vector3(startX, origin.y, worldZ + halfLineWidth);
                vertices[vertIndex + 2] = new Vector3(endX, origin.y, worldZ + halfLineWidth);
                vertices[vertIndex + 3] = new Vector3(endX, origin.y, worldZ - halfLineWidth);

                // UVs
                uvs[vertIndex + 0] = new Vector2(0, 0);
                uvs[vertIndex + 1] = new Vector2(1, 0);
                uvs[vertIndex + 2] = new Vector2(1, 1);
                uvs[vertIndex + 3] = new Vector2(0, 1);

                // Triangle indices
                triangles[triIndex + 0] = vertIndex + 0;
                triangles[triIndex + 1] = vertIndex + 2;
                triangles[triIndex + 2] = vertIndex + 1;

                triangles[triIndex + 3] = vertIndex + 0;
                triangles[triIndex + 4] = vertIndex + 3;
                triangles[triIndex + 5] = vertIndex + 2;

                vertIndex += 4;
                triIndex += 6;
            }

            // Assign mesh data
            gridMesh.vertices = vertices;
            gridMesh.triangles = triangles;
            gridMesh.uv = uvs;

            // Optimize mesh
            gridMesh.RecalculateNormals();
            gridMesh.RecalculateBounds();
            gridMesh.Optimize();

            // Assign to mesh filter
            meshFilter.mesh = gridMesh;

            Debug.Log($"Grid mesh generated: {vertexCount} vertices, {triangleCount / 3} triangles");
        }

        /// <summary>
        /// Updates the grid color if material supports it.
        /// </summary>
        /// <param name="color">New grid color</param>
        public void SetGridColor(Color color)
        {
            gridColor = color;
            if (meshRenderer.material.HasProperty("_Color"))
            {
                meshRenderer.material.color = gridColor;
            }
        }

        /// <summary>
        /// Updates the line width and regenerates the mesh.
        /// </summary>
        /// <param name="width">New line width</param>
        public void SetLineWidth(float width)
        {
            lineWidth = Mathf.Max(0.001f, width);
            GenerateGridMesh();
        }
        #endregion

        #region Editor Utilities
#if UNITY_EDITOR
        [ContextMenu("Force Regenerate Grid")]
        private void ForceRegenerate()
        {
            GenerateGridMesh();
        }
#endif
        #endregion
    }
}
