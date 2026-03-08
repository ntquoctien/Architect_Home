using UnityEngine;

/// <summary>
/// Handles hover highlight and double-click focus on objects.
/// Attach to a controller object and assign the focus controller + layer mask.
/// </summary>
public class ObjectHoverInteractor : MonoBehaviour
{
    public LayerMask interactLayer;
    public Color highlightColor = new Color(1f, 1f, 0.6f, 0.8f);
    public float doubleClickThreshold = 0.3f;
    public CameraFocusController focusController;

    private Camera mainCamera;
    private Renderer lastRenderer;
    private Color lastOriginalColor;
    private float lastClickTime = -10f;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (mainCamera == null) return;

        HandleHover();
        HandleClicks();
    }

    private void HandleHover()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactLayer))
        {
            Renderer r = hit.collider.GetComponentInChildren<Renderer>();
            if (r != null && r != lastRenderer)
            {
                ClearHighlight();
                lastRenderer = r;
                lastOriginalColor = r.material.color;
                r.material.color = highlightColor;
            }
        }
        else
        {
            ClearHighlight();
        }
    }

    private void HandleClicks()
    {
        if (!Input.GetMouseButtonDown(0) || lastRenderer == null) return;

        float now = Time.time;
        if (now - lastClickTime < doubleClickThreshold)
        {
            if (focusController != null)
            {
                focusController.FocusOnObject(lastRenderer.transform);
            }
        }
        lastClickTime = now;
    }

    private void ClearHighlight()
    {
        if (lastRenderer != null)
        {
            lastRenderer.material.color = lastOriginalColor;
            lastRenderer = null;
        }
    }
}
