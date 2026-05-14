using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen-space radial tool menu that appears when the player right-clicks a wall corner (NodeHandle/vertex).
/// Attach this component to a Canvas GameObject and assign the four action buttons in the Inspector.
///
/// Actions:
///   Add Wall       — splits one of the two adjacent edges by inserting a new corner at its midpoint.
///   Extend Wall    — nudges the vertex 0.5 m outward along the bisector of its two adjacent walls.
///   Remove Corner  — deletes the vertex (blocked silently if only 3 remain).
///   Create New Corner — enters place-mode; the next ground-plane click adds a new vertex.
/// </summary>
public class RadialToolMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button addWallButton;
    [SerializeField] private Button extendWallButton;
    [SerializeField] private Button removeCornerButton;
    [SerializeField] private Button createNewCornerButton;

    [Header("Sub-prompt for Add Wall")]
    [Tooltip("Small panel shown when choosing Left / Right edge for Add Wall")]
    [SerializeField] private GameObject edgeChoicePanel;
    [SerializeField] private Button leftEdgeButton;
    [SerializeField] private Button rightEdgeButton;

    [Header("Panel root")]
    [SerializeField] private RectTransform panelRoot;

    // ── Events ──────────────────────────────────────────────────────────
    /// <summary>Fired after any menu action completes so the caller can close the menu and resync.</summary>
    public event Action OnActionPerformed;

    // ── State ────────────────────────────────────────────────────────────
    private RoomData roomData;
    private int      cornerIndex   = -1;
    private bool     isVisible     = false;
    private bool     placeModeActive = false;

    private Camera mainCamera;

    // ─────────────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        mainCamera = Camera.main;

        // Wire buttons
        if (addWallButton)         addWallButton.onClick.AddListener(OnAddWall);
        if (extendWallButton)      extendWallButton.onClick.AddListener(OnExtendWall);
        if (removeCornerButton)    removeCornerButton.onClick.AddListener(OnRemoveCorner);
        if (createNewCornerButton) createNewCornerButton.onClick.AddListener(OnCreateNewCorner);
        if (leftEdgeButton)        leftEdgeButton.onClick.AddListener(() => OnAddWallEdgeChosen(isLeft: true));
        if (rightEdgeButton)       rightEdgeButton.onClick.AddListener(() => OnAddWallEdgeChosen(isLeft: false));

        HideAll();
    }

    private void Update()
    {
        // Place-mode: listen for a ground-plane click to add a new vertex
        if (placeModeActive)
        {
            HandlePlaceMode();
            return;
        }

        // Auto-hide on Escape
        if (isVisible && Input.GetKeyDown(KeyCode.Escape))
            Hide();

        // Auto-hide on click outside (left or right button, not on a UI element)
        if (isVisible && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
        {
            // A click on a button is handled by the Button component above;
            // any click that reaches here means we clicked outside the panel.
            Hide();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Shows the radial menu anchored near the given world position for the specified corner.</summary>
    public void Show(Vector3 worldPosition, int index, RoomData data)
    {
        roomData    = data;
        cornerIndex = index;
        isVisible   = true;

        // Position panel at screen-space projection of the vertex
        if (mainCamera == null) mainCamera = Camera.main;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
        if (panelRoot != null)
            panelRoot.position = screenPos;

        if (panelRoot != null) panelRoot.gameObject.SetActive(true);
        if (edgeChoicePanel)   edgeChoicePanel.SetActive(false);
    }

    /// <summary>Shows the radial menu (overload without RoomData — uses the previously cached reference).</summary>
    public void Show(Vector3 worldPosition, int index)
    {
        Show(worldPosition, index, roomData);
    }

    public void Hide()
    {
        HideAll();
        placeModeActive = false;
        isVisible       = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Button callbacks
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Add Wall — shows the sub-prompt to choose which adjacent edge to split.</summary>
    private void OnAddWall()
    {
        if (edgeChoicePanel)
            edgeChoicePanel.SetActive(true);
        // Keep main menu visible until the user picks an edge
    }

    private void OnAddWallEdgeChosen(bool isLeft)
    {
        if (roomData == null || cornerIndex < 0) return;

        int n = roomData.Corners.Count;
        // Left edge = the edge ENTERING this corner (prevCorner → cornerIndex)
        // Right edge = the edge LEAVING this corner (cornerIndex → nextCorner)
        int edgeIndex = isLeft
            ? (cornerIndex - 1 + n) % n   // edge i-1 (prevCorner → this)
            : cornerIndex;                 // edge i (this → nextCorner)

        var (start, end) = roomData.GetEdge(edgeIndex);
        Vector3 midpoint = (start + end) * 0.5f;
        roomData.InsertCorner(edgeIndex + 1, midpoint);

        OnActionPerformed?.Invoke();
        Hide();
    }

    /// <summary>Extend Wall — nudges the vertex 0.5 m outward along the bisector of its two walls.</summary>
    private void OnExtendWall()
    {
        if (roomData == null || cornerIndex < 0) return;

        int n    = roomData.Corners.Count;
        int prev = (cornerIndex - 1 + n) % n;
        int next = (cornerIndex + 1) % n;

        Vector3 curr    = roomData.Corners[cornerIndex];
        Vector3 toPrev  = (roomData.Corners[prev] - curr).normalized;
        Vector3 toNext  = (roomData.Corners[next] - curr).normalized;
        Vector3 bisector = -(toPrev + toNext).normalized; // outward = opposite of average inward direction

        roomData.MoveCorner(cornerIndex, curr + bisector * 0.5f);

        OnActionPerformed?.Invoke();
        Hide();
    }

    /// <summary>Remove Corner — silently blocked if only 3 corners remain.</summary>
    private void OnRemoveCorner()
    {
        if (roomData == null || cornerIndex < 0) return;
        roomData.RemoveCorner(cornerIndex); // Warning logged internally if blocked

        OnActionPerformed?.Invoke();
        Hide();
    }

    /// <summary>Create New Corner — enter place-mode; next ground-plane click adds a vertex.</summary>
    private void OnCreateNewCorner()
    {
        placeModeActive = true;
        // Hide the panel, but keep roomData reference
        if (panelRoot != null) panelRoot.gameObject.SetActive(false);
        isVisible = false;
    }

    private void HandlePlaceMode()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (mainCamera == null) mainCamera = Camera.main;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float dist))
            {
                Vector3 hitPoint = ray.GetPoint(dist);
                hitPoint.y = 0f;
                roomData?.AddCorner(hitPoint);
            }

            placeModeActive = false;
            OnActionPerformed?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            placeModeActive = false;
            OnActionPerformed?.Invoke();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────

    private void HideAll()
    {
        if (panelRoot != null)    panelRoot.gameObject.SetActive(false);
        if (edgeChoicePanel)      edgeChoicePanel.SetActive(false);
    }
}
