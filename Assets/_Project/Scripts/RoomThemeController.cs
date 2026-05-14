using System.Collections.Generic;
using UnityEngine;


// Áp dụng theme (materials/màu) cho phòng cơ bản.
public class RoomThemeController : MonoBehaviour
{
    [Header("Renderers (auto-find if empty)")]
    public Renderer floorRenderer;

    [Tooltip("Auto-filled from Room/Walls children named Wall_*")]
    public Renderer[] wallRenderers;

    [Header("Materials")]
    public Material floorMaterial;
    public Material wallMaterial;

    [Header("Colors (optional)")]
    public bool overrideColor = true;

    [Tooltip("Standard: _Color | URP Lit: _BaseColor")]
    public string colorProperty = "_Color";

    public Color floorColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color wallColor = Color.white;

    [Header("Per-room material instances")]
    public bool useMaterialInstances = true;

    private Material _floorMatInstance;
    private Material _wallMatInstance;

    // True once the procedural system has pushed runtime renderer references.
    // When true, AutoFindIfNeeded is bypassed so it can't overwrite runtime renderers
    // with a stale scene-hierarchy search.
    private bool _proceduralRenderersBound = false;

    void Awake()
    {
        AutoFindIfNeeded();
        ApplyTheme();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        AutoFindIfNeeded();
        ApplyTheme();
    }
#endif

    /// <summary>
    /// Called by RoomEditorController after every geometry rebuild.
    /// Binds the runtime-generated procedural renderers and applies the current theme.
    /// This is the ONLY correct way to refresh RoomThemeController in the procedural system.
    /// </summary>
    public void SetProceduralRenderers(Renderer floor, Renderer[] walls)
    {
        floorRenderer = floor;
        wallRenderers = walls;
        _proceduralRenderersBound = true;
        ApplyTheme();
    }

    [ContextMenu("Apply Theme")]
    public void ApplyTheme()
    {
        // Only run the static hierarchy search when renderers haven't been
        // set procedurally — prevents scene-name scan from overwriting runtime renderers.
        if (!_proceduralRenderersBound)
            AutoFindIfNeeded();

        Material floorMatToUse = PrepareMaterial(ref _floorMatInstance, floorMaterial);
        Material wallMatToUse  = PrepareMaterial(ref _wallMatInstance, wallMaterial);

        ApplyToRenderer(floorRenderer, floorMatToUse, floorColor);

        if (wallRenderers != null)
        {
            foreach (var r in wallRenderers)
                ApplyToRenderer(r, wallMatToUse, wallColor);
        }
    }

    private void AutoFindIfNeeded()
    {
        // Floor: Room/Floor
        if (floorRenderer == null)
        {
            var floor = transform.Find("Floor");
            if (floor != null) floorRenderer = floor.GetComponentInChildren<Renderer>(true);
        }

        // Walls: Room/Walls -> children Wall_*
        if (wallRenderers == null || wallRenderers.Length == 0)
        {
            var wallsRoot = transform.Find("Walls");
            if (wallsRoot == null)
                return;

            var list = new List<Renderer>();

            for (int i = 0; i < wallsRoot.childCount; i++)
            {
                Transform child = wallsRoot.GetChild(i);
                if (child == null) continue;

                // Quy ước: Wall_* (ví dụ Wall_01, Wall_2, Wall_12)
                if (!child.name.StartsWith("Wall_"))
                    continue;

                // Lấy renderer trên wall hoặc trong con
                var r = child.GetComponentInChildren<Renderer>(true);
                if (r != null) list.Add(r);
            }

            wallRenderers = list.ToArray();
        }
    }

    private Material PrepareMaterial(ref Material cacheInstance, Material source)
    {
        if (source == null) return null;

        if (!useMaterialInstances)
            return source;

        if (cacheInstance == null || cacheInstance.shader != source.shader)
        {
            cacheInstance = new Material(source);
            cacheInstance.name = source.name + "_Instance_" + gameObject.name;
        }

        return cacheInstance;
    }

    private void ApplyToRenderer(Renderer r, Material mat, Color color)
    {
        if (r == null) return;

        if (mat != null)
            r.sharedMaterial = mat;

        if (!overrideColor) return;

        var m = r.sharedMaterial;
        if (m != null && m.HasProperty(colorProperty))
            m.SetColor(colorProperty, color);
    }

    // Public helpers (đổi màu runtime)
    public void SetFloorColor(Color c) { floorColor = c; ApplyTheme(); }
    public void SetWallColor(Color c) { wallColor = c; ApplyTheme(); }

    // Force refresh walls list (static hierarchy mode only)
    public void RefreshWalls()
    {
        _proceduralRenderersBound = false;
        wallRenderers = null;
        AutoFindIfNeeded();
        ApplyTheme();
    }
}
