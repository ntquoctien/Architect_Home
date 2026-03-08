using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class IconGenerator : MonoBehaviour
{
    [Header("Prefab để chụp icon")]
    public GameObject prefab;

    [Header("Thiết lập lưu file")]
    public string folderPath = "Assets/Icons";
    public string fileName = "bed_icon";
    public int resolution = 512;

    [Header("Camera Settings")]
    public Color backgroundColor = Color.clear;
    public Vector3 viewRotation = new Vector3(90, 0, 0);
    public float padding = 1.2f;

    private const string ICON_LAYER = "IconCapture";

    [ContextMenu("Capture Icon")]
    public void CaptureIcon()
    {
        if (prefab == null)
        {
            Debug.LogError("Chưa gán prefab!");
            return;
        }

        // Tạo layer ẢNH ICON nếu chưa có
        CreateIconLayer();

        // Tạo instance vật thể cần chụp
        GameObject obj = Instantiate(prefab);
        obj.transform.position = new Vector3(0, 0, 0);
        obj.layer = LayerMask.NameToLayer(ICON_LAYER);
        SetLayerRecursively(obj, LayerMask.NameToLayer(ICON_LAYER));

        // Tính bounds và kích thước object
        Bounds bounds = GetObjectBounds(obj);
        Vector3 center = bounds.center;
        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float dist = size * padding;

        // Tạo camera tạm
        GameObject camGO = new GameObject("IconCamera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
        cam.orthographic = true;
        cam.orthographicSize = size * 0.6f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;

        // Camera CHỈ Render layer IconCapture
        cam.cullingMask = 1 << LayerMask.NameToLayer(ICON_LAYER);

        // Xoay camera và đặt vị trí
        cam.transform.rotation = Quaternion.Euler(viewRotation);
        cam.transform.position = center - cam.transform.forward * dist;

        // RenderTexture
        RenderTexture rt = new RenderTexture(resolution, resolution, 24);
        cam.targetTexture = rt;
        cam.Render();

        // Chụp Texture
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        byte[] png = tex.EncodeToPNG();
        string fullPath = Path.Combine(folderPath, fileName + ".png");
        Directory.CreateDirectory(folderPath);
        File.WriteAllBytes(fullPath, png);

        Debug.Log("Đã lưu icon tại: " + fullPath);

        // Cleanup
        DestroyImmediate(obj);
        DestroyImmediate(camGO);
        DestroyImmediate(rt);
        DestroyImmediate(tex);

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    private void CreateIconLayer()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
        );
        SerializedProperty layers = tagManager.FindProperty("layers");

        // Tìm layer trống
        bool exists = false;
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty sp = layers.GetArrayElementAtIndex(i);

            if (sp.stringValue == ICON_LAYER)
                exists = true;

            if (sp.stringValue == "" && !exists)
            {
                sp.stringValue = ICON_LAYER;
                tagManager.ApplyModifiedProperties();
                exists = true;
                break;
            }
        }
    }

    private Bounds GetObjectBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(renderers[0].bounds.center, Vector3.zero);

        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        return bounds;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
