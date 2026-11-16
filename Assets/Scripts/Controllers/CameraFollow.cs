using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    Transform target;

    [SerializeField]
    bool invertForward = true;

    [SerializeField, Min(0f)]
    float distance = 10f;

    [SerializeField]
    float height = 3f;

    [SerializeField, Min(0f)]
    float lookAheadDistance = 15f;

    [SerializeField, Min(0.01f)]
    float positionSmoothTime = 0.08f;

    [SerializeField, Min(0.1f)]
    float rotationSharpness = 8f;

    [Header("Target Focus")]
    [Tooltip(
        "Quanto a nave do player pode afastar-se do centro do ecrã ao focar um alvo, "
            + "como fração do meio-FOV vertical. 0.7 = a nave nunca passa dos 70% até à borda."
    )]
    [SerializeField, Range(0.1f, 0.95f)]
    float shipSafeZone = 0.7f;

    Vector3 posVelocity;
    Camera cam;

    public float ExtraDistance { get; set; }

    /// <summary>Alvo para onde a câmara roda (parcialmente). Null = comportamento normal.</summary>
    public Transform FocusTarget { get; set; }

    /// <summary>0 = ignora o alvo, 1 = olha diretamente para o alvo.</summary>
    public float FocusBlend { get; set; } = 0.5f;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target == null)
        {
            enabled = false;
            return;
        }
        SnapToTarget();
    }

    Vector3 Forward => target.forward * (invertForward ? -1f : 1f);

    Vector3 DesiredPosition => target.position - Forward * (distance + ExtraDistance) + target.up * height;

    Quaternion DesiredRotation()
    {
        Vector3 lookPoint = target.position + Forward * lookAheadDistance;
        Vector3 dir = lookPoint - transform.position;

        if (FocusTarget != null)
        {
            Vector3 toFocus = FocusTarget.position - transform.position;
            if (toFocus.sqrMagnitude > 0.01f)
            {
                Vector3 baseDir = dir.normalized;
                dir = Vector3.Slerp(baseDir, toFocus.normalized, FocusBlend);
                dir = KeepShipInView(dir, baseDir);
            }
        }

        return Quaternion.LookRotation(dir, target.up);
    }

    Vector3 KeepShipInView(Vector3 dir, Vector3 baseDir)
    {
        Vector3 toShip = target.position - transform.position;
        if (toShip.sqrMagnitude < 0.0001f)
            return dir;
        toShip.Normalize();

        float halfFov = (cam != null ? cam.fieldOfView : 60f) * 0.5f;
        float baseline = Vector3.Angle(baseDir, toShip);
        float limit = Mathf.Max(halfFov * shipSafeZone, baseline);

        if (Vector3.Angle(dir, toShip) <= limit)
            return dir;

        return Vector3.RotateTowards(toShip, dir.normalized, limit * Mathf.Deg2Rad, 0f);
    }

    void SnapToTarget()
    {
        transform.position = DesiredPosition;
        transform.rotation = DesiredRotation();
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            DesiredPosition,
            ref posVelocity,
            positionSmoothTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            DesiredRotation(),
            1f - Mathf.Exp(-rotationSharpness * dt)
        );

        if (FocusTarget != null)
        {
            Vector3 baseDir = (target.position + Forward * lookAheadDistance - transform.position).normalized;
            Vector3 limited = KeepShipInView(transform.forward, baseDir);
            if (limited != transform.forward)
                transform.rotation = Quaternion.LookRotation(limited, target.up);
        }
    }
}