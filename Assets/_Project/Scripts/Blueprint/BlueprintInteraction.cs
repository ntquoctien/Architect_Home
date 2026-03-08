using UnityEngine;

namespace ArchitectHome.Blueprint
{
    /// <summary>
    /// Handles mouse input and snapping logic for the blueprint grid system.
    /// Uses mathematical Plane.Raycast instead of Physics for optimal performance.
    /// Provides smooth snapping with lerping for a polished "cozy" feel.
    /// </summary>
    public class BlueprintInteraction : MonoBehaviour
    {
        #region Configuration
        [Header("Input Settings")]
        [Tooltip("Camera used for raycasting (usually Main Camera)")]
        [SerializeField] private Camera raycastCamera;

        [Header("Ghost Cursor")]
        [Tooltip("Visual indicator that snaps to grid cells")]
        [SerializeField] private Transform ghostCursor;

        [Tooltip("Speed of cursor snapping (higher = faster)")]
        [SerializeField] private float snapSpeed = 15f;

        [Tooltip("Vertical offset for the ghost cursor above the grid")]
        [SerializeField] private float cursorHeightOffset = 0.05f;

        [Header("Visual Feedback")]
        [Tooltip("Show ghost cursor only when hovering over valid grid cells")]
        [SerializeField] private bool hideWhenOutOfBounds = true;

        [Tooltip("Color when hovering over valid position")]
        [SerializeField] private Color validColor = Color.green;

        [Tooltip("Color when hovering over invalid position")]
        [SerializeField] private Color invalidColor = Color.red;
        #endregion

        #region State
        private Plane gridPlane;
        private Vector2Int currentGridPosition;
        private Vector2Int lastValidGridPosition;
        private bool isOverValidCell;
        private Vector3 targetWorldPosition;
        private Renderer ghostRenderer;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // Auto-assign main camera if not set
            if (raycastCamera == null)
            {
                raycastCamera = Camera.main;
                if (raycastCamera == null)
                {
                    Debug.LogError("No camera assigned and Camera.main is null!");
                }
            }

            // Initialize grid plane at Y=0 (or GridManager origin Y)
            float planeY = GridManager.Instance != null ? GridManager.Instance.OriginPosition.y : 0f;
            gridPlane = new Plane(Vector3.up, new Vector3(0, planeY, 0));

            // Setup ghost cursor
            if (ghostCursor != null)
            {
                ghostRenderer = ghostCursor.GetComponent<Renderer>();
                targetWorldPosition = ghostCursor.position;
            }
            else
            {
                Debug.LogWarning("Ghost cursor not assigned to BlueprintInteraction!");
            }
        }

        private void Update()
        {
            if (GridManager.Instance == null || raycastCamera == null)
                return;

            HandleMouseInput();
            UpdateGhostCursor();
        }
        #endregion

        #region Input Handling
        /// <summary>
        /// Handles mouse input: raycasting and grid snapping.
        /// </summary>
        private void HandleMouseInput()
        {
            // Create ray from camera through mouse position
            Ray ray = raycastCamera.ScreenPointToRay(Input.mousePosition);

            // Raycast against mathematical plane (no physics overhead!)
            if (gridPlane.Raycast(ray, out float enter))
            {
                // Get world position where ray hits the plane
                Vector3 hitPoint = ray.GetPoint(enter);

                // Convert to grid coordinates
                currentGridPosition = GridManager.Instance.WorldToGrid(hitPoint);

                // Check if position is valid
                isOverValidCell = GridManager.Instance.IsValidGridPosition(currentGridPosition);

                if (isOverValidCell)
                {
                    // Store last valid position
                    lastValidGridPosition = currentGridPosition;

                    // Calculate target world position (center of cell)
                    targetWorldPosition = GridManager.Instance.GridToWorldCenter(
                        currentGridPosition.x, 
                        currentGridPosition.y
                    );
                    targetWorldPosition.y += cursorHeightOffset;

                    // Handle mouse click
                    if (Input.GetMouseButtonDown(0))
                    {
                        OnGridCellClicked(currentGridPosition);
                    }
                }
            }
            else
            {
                isOverValidCell = false;
            }
        }

        /// <summary>
        /// Called when a grid cell is clicked.
        /// Override this or use events for custom behavior.
        /// </summary>
        /// <param name="gridPos">The clicked grid position</param>
        private void OnGridCellClicked(Vector2Int gridPos)
        {
            Debug.Log($"Grid cell clicked: [{gridPos.x}, {gridPos.y}] at world position: " +
                     $"{GridManager.Instance.GridToWorldCenter(gridPos.x, gridPos.y)}");

            // TODO: Implement your custom logic here
            // Examples:
            // - Place furniture
            // - Select cell
            // - Start building
            // - Show context menu
        }
        #endregion

        #region Ghost Cursor
        /// <summary>
        /// Updates the ghost cursor position with smooth lerping.
        /// Provides visual feedback based on validity.
        /// </summary>
        private void UpdateGhostCursor()
        {
            if (ghostCursor == null)
                return;

            // Smooth movement to target position
            ghostCursor.position = Vector3.Lerp(
                ghostCursor.position,
                targetWorldPosition,
                Time.deltaTime * snapSpeed
            );

            // Handle visibility
            if (hideWhenOutOfBounds)
            {
                ghostCursor.gameObject.SetActive(isOverValidCell);
            }

            // Update color based on validity
            UpdateGhostCursorColor();
        }

        /// <summary>
        /// Updates ghost cursor color based on cell validity.
        /// </summary>
        private void UpdateGhostCursorColor()
        {
            if (ghostRenderer == null)
                return;

            Color targetColor = isOverValidCell ? validColor : invalidColor;

            // Apply color if material supports it
            if (ghostRenderer.material.HasProperty("_Color"))
            {
                ghostRenderer.material.color = targetColor;
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Gets the current grid position under the mouse cursor.
        /// </summary>
        /// <returns>Current grid position</returns>
        public Vector2Int GetCurrentGridPosition()
        {
            return currentGridPosition;
        }

        /// <summary>
        /// Gets the last valid grid position (useful when mouse is out of bounds).
        /// </summary>
        /// <returns>Last valid grid position</returns>
        public Vector2Int GetLastValidGridPosition()
        {
            return lastValidGridPosition;
        }

        /// <summary>
        /// Checks if the cursor is currently over a valid grid cell.
        /// </summary>
        /// <returns>True if over valid cell, false otherwise</returns>
        public bool IsOverValidCell()
        {
            return isOverValidCell;
        }

        /// <summary>
        /// Manually sets the ghost cursor position to a specific grid cell.
        /// </summary>
        /// <param name="gridPos">Target grid position</param>
        public void SetCursorToGridPosition(Vector2Int gridPos)
        {
            if (GridManager.Instance.IsValidGridPosition(gridPos))
            {
                currentGridPosition = gridPos;
                lastValidGridPosition = gridPos;
                targetWorldPosition = GridManager.Instance.GridToWorldCenter(gridPos.x, gridPos.y);
                targetWorldPosition.y += cursorHeightOffset;
                isOverValidCell = true;
            }
        }

        /// <summary>
        /// Enables or disables the ghost cursor.
        /// </summary>
        /// <param name="enabled">Enable state</param>
        public void SetGhostCursorEnabled(bool enabled)
        {
            if (ghostCursor != null)
            {
                ghostCursor.gameObject.SetActive(enabled);
            }
        }
        #endregion

        #region Debug Visualization
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !isOverValidCell)
                return;

            // Draw current grid cell outline
            Gizmos.color = isOverValidCell ? Color.green : Color.red;
            Vector3 cellCenter = GridManager.Instance.GridToWorldCenter(
                currentGridPosition.x, 
                currentGridPosition.y
            );
            float cellSize = GridManager.Instance.CellSize;
            Gizmos.DrawWireCube(cellCenter, new Vector3(cellSize, 0.1f, cellSize));
        }
        #endregion
    }
}
