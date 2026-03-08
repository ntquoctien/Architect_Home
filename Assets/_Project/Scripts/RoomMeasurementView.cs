using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Displays real-time measurements between room corners using TextMeshPro.
/// Labels are positioned at eye level, facing outward perpendicular to walls.
/// Includes semi-transparent background for improved readability.
/// </summary>
public class RoomMeasurementView : MonoBehaviour
{
    [Header("Measurement Settings")]
    [SerializeField] private bool showMeasurements = true;
    [Tooltip("Height above ground for measurement labels (eye level)")]
    [SerializeField] private float heightOffset = 1.5f;
    [Tooltip("Offset from wall center (positive = outward from room center)")]
    [SerializeField] private float wallOffset = 0.3f;
    [SerializeField] private string measurementUnit = "m";
    [SerializeField] private int decimalPlaces = 1;

    [Header("Text Style")]
    [SerializeField] private float textSize = 2f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.75f);
    [SerializeField] private float backgroundPadding = 0.15f;

    [Header("Background Settings")]
    [SerializeField] private float backgroundWidth = 1.2f;
    [SerializeField] private float backgroundHeight = 0.4f;

    [Header("Prefab (Optional)")]
    [SerializeField] private GameObject textPrefab;

    private RoomData roomData;
    private List<GameObject> measurementTexts = new List<GameObject>();
    private Transform measurementContainer;
    private Material backgroundMaterial;

    private void Awake()
    {
        // Pre-create background material
        CreateBackgroundMaterial();
    }

    /// <summary>
    /// Creates the semi-transparent background material.
    /// </summary>
    private void CreateBackgroundMaterial()
    {
        // Try URP shader first, fallback to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        backgroundMaterial = new Material(shader);
        backgroundMaterial.name = "MeasurementBackground_Mat";
        backgroundMaterial.color = backgroundColor;

        // Enable transparency
        if (backgroundMaterial.HasProperty("_Surface"))
        {
            // URP
            backgroundMaterial.SetFloat("_Surface", 1); // Transparent
            backgroundMaterial.SetFloat("_Blend", 0); // Alpha
        }
        
        // Set rendering mode for transparency
        backgroundMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        backgroundMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        backgroundMaterial.SetInt("_ZWrite", 0);
        backgroundMaterial.DisableKeyword("_ALPHATEST_ON");
        backgroundMaterial.EnableKeyword("_ALPHABLEND_ON");
        backgroundMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        backgroundMaterial.renderQueue = 3000; // Transparent queue
    }

    /// <summary>
    /// Initializes the measurement view with room data.
    /// </summary>
    public void Initialize(RoomData data)
    {
        // Cleanup previous subscription if reinitializing
        if (roomData != null)
        {
            roomData.OnGeometryChanged -= UpdateMeasurements;
        }

        roomData = data;
        roomData.OnGeometryChanged += UpdateMeasurements;

        // Create container for measurements
        if (measurementContainer == null)
        {
            measurementContainer = new GameObject("MeasurementContainer").transform;
            measurementContainer.SetParent(transform);
            measurementContainer.localPosition = Vector3.zero;
        }

        UpdateMeasurements();
    }

    /// <summary>
    /// Updates all measurement displays when geometry changes.
    /// </summary>
    private void UpdateMeasurements()
    {
        if (!showMeasurements || roomData == null)
        {
            HideAllMeasurements();
            return;
        }

        int edgeCount = roomData.Corners.Count;

        // Pool management: Create or reuse text objects
        while (measurementTexts.Count < edgeCount)
        {
            measurementTexts.Add(CreateMeasurementText());
        }

        // Calculate room center for determining outward direction
        Vector3 roomCenter = CalculateRoomCenter();

        // Update each measurement
        for (int i = 0; i < edgeCount; i++)
        {
            var (start, end) = roomData.GetEdge(i);
            float length = Vector3.Distance(start, end);
            
            // Calculate midpoint of wall at eye level
            Vector3 midpoint = (start + end) * 0.5f;
            
            // Calculate wall direction and outward-facing normal
            Vector3 wallDirection = (end - start).normalized;
            Vector3 wallNormal = new Vector3(-wallDirection.z, 0, wallDirection.x);
            
            // Determine if normal points outward (away from room center)
            Vector3 toCenter = (roomCenter - midpoint).normalized;
            if (Vector3.Dot(wallNormal, toCenter) > 0)
            {
                // Normal points toward center, flip it
                wallNormal = -wallNormal;
            }
            
            // Position: at eye level, offset outward from wall
            Vector3 labelPosition = midpoint + Vector3.up * heightOffset + wallNormal * wallOffset;

            GameObject textObj = measurementTexts[i];
            textObj.SetActive(true);
            textObj.transform.position = labelPosition;

            // Rotation: Face outward, perpendicular to wall, standing upright
            // LookRotation(forward, up) where forward = direction the text faces
            Quaternion rotation = Quaternion.LookRotation(wallNormal, Vector3.up);
            textObj.transform.rotation = rotation;

            // Update text content
            TextMeshPro tmp = textObj.GetComponentInChildren<TextMeshPro>();
            if (tmp != null)
            {
                tmp.text = FormatMeasurement(length);
            }

            // Update background size based on text length
            UpdateBackgroundSize(textObj, length);
        }

        // Hide unused text objects
        for (int i = edgeCount; i < measurementTexts.Count; i++)
        {
            measurementTexts[i].SetActive(false);
        }
    }

    /// <summary>
    /// Calculates the centroid of the room for determining outward direction.
    /// </summary>
    private Vector3 CalculateRoomCenter()
    {
        if (roomData == null || roomData.Corners.Count == 0)
            return Vector3.zero;

        Vector3 center = Vector3.zero;
        foreach (var corner in roomData.Corners)
        {
            center += corner;
        }
        return center / roomData.Corners.Count;
    }

    /// <summary>
    /// Updates the background quad size based on measurement text length.
    /// </summary>
    private void UpdateBackgroundSize(GameObject textObj, float measurementValue)
    {
        Transform bgTransform = textObj.transform.Find("Background");
        if (bgTransform != null)
        {
            // Scale background based on number of digits
            string text = FormatMeasurement(measurementValue);
            float widthMultiplier = 0.15f * text.Length + 0.3f;
            bgTransform.localScale = new Vector3(
                Mathf.Max(backgroundWidth, widthMultiplier),
                backgroundHeight,
                1f
            );
        }
    }

    /// <summary>
    /// Creates a new measurement text object.
    /// </summary>
    private GameObject CreateMeasurementText()
    {
        GameObject textObj;

        if (textPrefab != null)
        {
            textObj = Instantiate(textPrefab, measurementContainer);
        }
        else
        {
            textObj = CreateDefaultMeasurementText();
        }

        textObj.name = $"Measurement_{measurementTexts.Count}";
        return textObj;
    }

    /// <summary>
    /// Creates a default TextMeshPro measurement object with background quad.
    /// </summary>
    private GameObject CreateDefaultMeasurementText()
    {
        // Parent container for label
        GameObject labelRoot = new GameObject("MeasurementLabel");
        labelRoot.transform.SetParent(measurementContainer);

        // ===== BACKGROUND QUAD =====
        // Create background first so it renders behind text
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "Background";
        background.transform.SetParent(labelRoot.transform);
        background.transform.localPosition = new Vector3(0, 0, 0.01f); // Slightly behind text
        background.transform.localRotation = Quaternion.identity;
        background.transform.localScale = new Vector3(backgroundWidth, backgroundHeight, 1f);

        // Apply background material
        Renderer bgRenderer = background.GetComponent<Renderer>();
        if (bgRenderer != null && backgroundMaterial != null)
        {
            bgRenderer.sharedMaterial = backgroundMaterial;
        }

        // Remove collider from background (prevent interference with raycasts)
        Collider bgCollider = background.GetComponent<Collider>();
        if (bgCollider != null)
            DestroyImmediate(bgCollider);

        // ===== TEXT MESH PRO =====
        // Create a child object for TextMeshPro
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(labelRoot.transform);
        textObject.transform.localPosition = Vector3.zero;
        textObject.transform.localRotation = Quaternion.identity;

        TextMeshPro tmp = textObject.AddComponent<TextMeshPro>();
        tmp.fontSize = textSize;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;
        
        // Text rendering settings
        tmp.sortingOrder = 1; // Render above background
        
        // RectTransform sizing
        RectTransform rectTransform = tmp.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(backgroundWidth * 100f, backgroundHeight * 100f);
        }

        return labelRoot;
    }

    /// <summary>
    /// Formats measurement value with unit.
    /// </summary>
    private string FormatMeasurement(float value)
    {
        return $"{value.ToString($"F{decimalPlaces}")}{measurementUnit}";
    }

    /// <summary>
    /// Hides all measurement displays.
    /// </summary>
    private void HideAllMeasurements()
    {
        foreach (var textObj in measurementTexts)
        {
            if (textObj != null)
                textObj.SetActive(false);
        }
    }

    /// <summary>
    /// Toggles measurement visibility.
    /// </summary>
    public void SetMeasurementsVisible(bool visible)
    {
        showMeasurements = visible;
        UpdateMeasurements();
    }

    /// <summary>
    /// Updates measurement style dynamically.
    /// </summary>
    public void UpdateStyle(float size, Color color)
    {
        textSize = size;
        textColor = color;

        foreach (var textObj in measurementTexts)
        {
            if (textObj == null) continue;
            
            TextMeshPro tmp = textObj.GetComponentInChildren<TextMeshPro>();
            if (tmp != null)
            {
                tmp.fontSize = textSize;
                tmp.color = textColor;
            }
        }
    }

    /// <summary>
    /// Updates background color dynamically.
    /// </summary>
    public void UpdateBackgroundColor(Color color)
    {
        backgroundColor = color;
        
        if (backgroundMaterial != null)
        {
            backgroundMaterial.color = backgroundColor;
        }
    }

    /// <summary>
    /// Sets the height offset for measurement labels.
    /// </summary>
    public void SetHeightOffset(float height)
    {
        heightOffset = height;
        UpdateMeasurements();
    }

    private void OnDestroy()
    {
        if (roomData != null)
        {
            roomData.OnGeometryChanged -= UpdateMeasurements;
        }

        // Clean up pooled objects
        foreach (var textObj in measurementTexts)
        {
            if (textObj != null)
                Destroy(textObj);
        }
        measurementTexts.Clear();

        // Clean up material
        if (backgroundMaterial != null)
        {
            Destroy(backgroundMaterial);
        }
    }

    private void OnValidate()
    {
        // Update measurements when values change in inspector
        if (Application.isPlaying && roomData != null)
        {
            // Update background material color
            if (backgroundMaterial != null)
            {
                backgroundMaterial.color = backgroundColor;
            }
            
            UpdateMeasurements();
        }
    }
}
