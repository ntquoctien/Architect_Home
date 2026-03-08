using UnityEngine;

namespace ArchitectHome.Blueprint
{
    /// <summary>
    /// Singleton manager that handles grid logic, data storage, and coordinate conversion.
    /// This class does NOT handle rendering - it's purely for mathematical grid operations.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        #region Singleton
        private static GridManager _instance;
        public static GridManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GridManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GridManager");
                        _instance = go.AddComponent<GridManager>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Grid Configuration
        [Header("Grid Dimensions")]
        [Tooltip("Number of cells along the X axis")]
        [SerializeField] private int width = 20;

        [Tooltip("Number of cells along the Z axis")]
        [SerializeField] private int height = 20;

        [Tooltip("Size of each cell in world units")]
        [SerializeField] private float cellSize = 1f;

        [Header("Grid Position")]
        [Tooltip("World position of the grid origin (bottom-left corner)")]
        [SerializeField] private Vector3 originPosition = Vector3.zero;

        // Public read-only accessors
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public Vector3 OriginPosition => originPosition;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Enforce singleton pattern
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnValidate()
        {
            // Ensure valid values in the inspector
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            cellSize = Mathf.Max(0.1f, cellSize);
        }
        #endregion

        #region Coordinate Conversion
        /// <summary>
        /// Converts a world position to grid coordinates.
        /// </summary>
        /// <param name="worldPos">Position in world space</param>
        /// <returns>Grid coordinates (X, Z) as Vector2Int</returns>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            // Calculate relative position from origin
            Vector3 relativePos = worldPos - originPosition;

            // Convert to grid coordinates
            int x = Mathf.FloorToInt(relativePos.x / cellSize);
            int z = Mathf.FloorToInt(relativePos.z / cellSize);

            return new Vector2Int(x, z);
        }

        /// <summary>
        /// Converts grid coordinates to world position at the CENTER of the cell.
        /// </summary>
        /// <param name="x">Grid X coordinate</param>
        /// <param name="y">Grid Z coordinate</param>
        /// <returns>World position at cell center</returns>
        public Vector3 GridToWorldCenter(int x, int y)
        {
            float worldX = originPosition.x + (x * cellSize) + (cellSize * 0.5f);
            float worldZ = originPosition.z + (y * cellSize) + (cellSize * 0.5f);
            return new Vector3(worldX, originPosition.y, worldZ);
        }

        /// <summary>
        /// Converts grid coordinates to world position at the CORNER (intersection point).
        /// This is the bottom-left corner of the cell.
        /// </summary>
        /// <param name="x">Grid X coordinate</param>
        /// <param name="y">Grid Z coordinate</param>
        /// <returns>World position at cell corner</returns>
        public Vector3 GridToWorldCorner(int x, int y)
        {
            float worldX = originPosition.x + (x * cellSize);
            float worldZ = originPosition.z + (y * cellSize);
            return new Vector3(worldX, originPosition.y, worldZ);
        }

        /// <summary>
        /// Snaps a world position to the nearest grid cell center.
        /// </summary>
        /// <param name="worldPos">Position to snap</param>
        /// <returns>Snapped world position</returns>
        public Vector3 SnapToGridCenter(Vector3 worldPos)
        {
            Vector2Int gridPos = WorldToGrid(worldPos);
            gridPos = ClampToGrid(gridPos); // Ensure it's within bounds
            return GridToWorldCenter(gridPos.x, gridPos.y);
        }
        #endregion

        #region Validation & Utilities
        /// <summary>
        /// Checks if the given grid coordinates are within valid bounds.
        /// </summary>
        /// <param name="x">Grid X coordinate</param>
        /// <param name="y">Grid Z coordinate</param>
        /// <returns>True if position is valid, false otherwise</returns>
        public bool IsValidGridPosition(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        /// <summary>
        /// Checks if the given grid position is within valid bounds.
        /// </summary>
        /// <param name="gridPos">Grid position to check</param>
        /// <returns>True if position is valid, false otherwise</returns>
        public bool IsValidGridPosition(Vector2Int gridPos)
        {
            return IsValidGridPosition(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// Clamps grid coordinates to valid bounds.
        /// </summary>
        /// <param name="gridPos">Grid position to clamp</param>
        /// <returns>Clamped grid position</returns>
        public Vector2Int ClampToGrid(Vector2Int gridPos)
        {
            int x = Mathf.Clamp(gridPos.x, 0, width - 1);
            int y = Mathf.Clamp(gridPos.y, 0, height - 1);
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Gets the total world size of the grid.
        /// </summary>
        /// <returns>World size as Vector2 (width, height)</returns>
        public Vector2 GetGridWorldSize()
        {
            return new Vector2(width * cellSize, height * cellSize);
        }
        #endregion

        #region Debug Visualization
        private void OnDrawGizmos()
        {
            // Draw grid bounds in editor
            Gizmos.color = Color.yellow;
            Vector2 worldSize = new Vector2(width * cellSize, height * cellSize);
            Vector3 center = originPosition + new Vector3(worldSize.x * 0.5f, 0, worldSize.y * 0.5f);
            Gizmos.DrawWireCube(center, new Vector3(worldSize.x, 0.1f, worldSize.y));

            // Draw origin point
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(originPosition, 0.2f);
        }
        #endregion
    }
}
