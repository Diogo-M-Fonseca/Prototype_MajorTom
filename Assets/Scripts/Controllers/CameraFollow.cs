using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    Transform target;

    [SerializeField, Min(0f)]
    float lookAheadTime = 0.5f;

    [SerializeField, Min(0f)]
    float maxLookAhead = 6f;

    [SerializeField, Min(0.01f)]
    float smoothTime = 0.25f;

    const float VelocitySmoothing = 10f;

    Vector3 offset,
        lastPos,
        velocity,
        camVelocity;

    void Start()
    {
        if (target == null)
        {
            enabled = false;
            return;
        }
        offset = transform.position - target.position;
        lastPos = target.position;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        Vector3 pos = target.position;
        Vector3 raw = (pos - lastPos) / dt;
        raw.y = 0f;
        lastPos = pos;
        velocity = Vector3.Lerp(velocity, raw, 1f - Mathf.Exp(-VelocitySmoothing * dt));

        Vector3 lead = Vector3.ClampMagnitude(velocity * lookAheadTime, maxLookAhead);
        transform.position = Vector3.SmoothDamp(
            transform.position,
            pos + lead + offset,
            ref camVelocity,
            smoothTime
        );
    }
}
