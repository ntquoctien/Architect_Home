using UnityEngine;

public class AutoHideWall : MonoBehaviour
{
    [Header("Wall normal in WORLD space (points outward)")]
    public Vector3 worldNormal = Vector3.forward;

    [Tooltip("Nếu MeshRenderer nằm ở con thì kéo renderer con vào đây.")]
    public MeshRenderer rendererOverride;

    [Tooltip("Nếu wall đang bị ẩn bởi chế độ edit (ví dụ edit ceiling) thì giữ ẩn.")]
    public bool forceHidden;

    private MeshRenderer rend;

    void Awake()
    {
        rend = rendererOverride != null
            ? rendererOverride
            : GetComponentInChildren<MeshRenderer>(true);

        if (rend == null)
            Debug.LogWarning("AutoHideWall: không tìm thấy MeshRenderer", this);

        // Chuẩn hoá
        if (worldNormal.sqrMagnitude < 0.0001f)
            worldNormal = transform.forward;

        worldNormal.Normalize();
    }

    public void SetHidden(bool hidden)
    {
        if (rend == null) return;
        rend.enabled = !(hidden || forceHidden);
    }

    // helper: nếu bạn muốn đặt normal theo hướng transform (khuyến nghị)
    [ContextMenu("Set Normal = Transform Forward")]
    public void SetNormalFromForward()
    {
        worldNormal = transform.forward.normalized;
    }
}
