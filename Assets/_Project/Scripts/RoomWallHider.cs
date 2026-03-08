using UnityEngine;

public class RoomWallHider : MonoBehaviour
{
    public Transform roomCenter;     // Room/Pivot hoặc Floor center
    public Camera cam;               // Main Camera
    public AutoHideWall[] walls;     // các wall có AutoHideWall
    public bool debugDraw = false;

    [Header("Tuning")]
    [Tooltip("Ngưỡng dot để bắt đầu hide. Tăng lên nếu hide quá nhiều.")]
    [Range(0f, 0.9f)]
    public float hideDotThreshold = 0.15f;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null || roomCenter == null || walls == null || walls.Length == 0)
            return;

        Vector3 center = roomCenter.position;

        // hướng từ center -> camera (camera đang ở phía nào của phòng)
        Vector3 toCam = (cam.transform.position - center);
        toCam.y = 0f; // chỉ xét mặt phẳng sàn để ổn định
        if (toCam.sqrMagnitude < 0.0001f) return;
        toCam.Normalize();

        foreach (var wall in walls)
        {
            if (wall == null) continue;

            Vector3 n = wall.worldNormal;
            n.y = 0f;
            if (n.sqrMagnitude < 0.0001f) continue;
            n.Normalize();

            // Nếu camera nằm về phía normal của wall => wall này là "mặt trước" chắn tầm nhìn
            float d = Vector3.Dot(toCam, n);

            bool hide = d > hideDotThreshold;
            wall.SetHidden(hide);

            if (debugDraw)
            {
                Debug.DrawRay(center, toCam * 2f, Color.green);
                Debug.DrawRay(wall.transform.position, n * 1.2f, hide ? Color.red : Color.cyan);
            }
        }
    }
}
