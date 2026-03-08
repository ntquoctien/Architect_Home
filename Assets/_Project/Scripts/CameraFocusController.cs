using UnityEngine;

public class CameraFocusController : MonoBehaviour
{
    public float focusDuration = 1.0f;
    public float minFocusDistance = 2.0f;
    public float maxFocusDistance = 5.0f;
    public AnimationCurve smoothCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public bool IsFocusing { get; private set; }
    public Transform CurrentTarget { get; private set; }

    private Vector3 startPos;
    private Quaternion startRot;
    private Vector3 targetPos;
    private Quaternion targetRot;
    private float t;

    public void FocusOnObject(Transform target)
    {
        if (target == null) return;

        IsFocusing = true;
        CurrentTarget = target;
        t = 0f;

        startPos = transform.position;
        startRot = transform.rotation;

        // tính bounds để auto khoảng cách
        Bounds b;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            b = new Bounds(renderers[0].bounds.center, Vector3.zero);
            foreach (var r in renderers)
                b.Encapsulate(r.bounds);
        }
        else
        {
            b = new Bounds(target.position, Vector3.one);
        }

        float radius = b.extents.magnitude;
        float focusDist = Mathf.Clamp(radius * 2f, minFocusDistance, maxFocusDistance);

        Vector3 dir = (transform.position - b.center).normalized;
        if (dir.sqrMagnitude < 0.01f)
            dir = -target.forward; // phòng TH camera đứng đúng trên nó

        targetPos = b.center + dir * focusDist + Vector3.up * (radius * 0.2f);
        targetRot = Quaternion.LookRotation(b.center - targetPos);
    }

    public void ExitFocusMode()
    {
        IsFocusing = false;
        CurrentTarget = null;
    }

    void Update()
    {
        if (!IsFocusing) return;

        t += Time.deltaTime / focusDuration;
        float k = smoothCurve.Evaluate(t);

        transform.position = Vector3.Lerp(startPos, targetPos, k);
        transform.rotation = Quaternion.Slerp(startRot, targetRot, k);

        if (t >= 1f)
        {
            IsFocusing = false;
        }
    }
}
