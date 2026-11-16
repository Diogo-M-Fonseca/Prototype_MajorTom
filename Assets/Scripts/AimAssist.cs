using SpaceFlight;
using UnityEngine;


[DefaultExecutionOrder(100)]
public class AimAssist : MonoBehaviour
{
    
    [SerializeField, Range(0f, 1f)]
    private float assistLevel = 0.5f;

    [SerializeField, Min(1f)]
    private float maxCaptureAngle = 20f;

    [SerializeField, Min(0.1f)]
    private float maxPullRate = 8f;

    [SerializeField, Min(1f)]
    private float maxDegreesPerSecond = 90f;

    private TargetLockSystem lockSystem;
    private SpaceshipController ship;

    public float AssistLevel
    {
        get => assistLevel;
        set => assistLevel = Mathf.Clamp01(value);
    }

    private void Awake()
    {
        lockSystem = GetComponent<TargetLockSystem>();
    }

    private void Update()
    {
        if (assistLevel <= 0f || Time.timeScale <= 0f)
            return;

        if (lockSystem == null)
            lockSystem = FindFirstObjectByType<TargetLockSystem>();
        if (ship == null)
            ship = FindFirstObjectByType<SpaceshipController>();
        if (lockSystem == null || ship == null)
            return;

        if (!lockSystem.TryGetLead(out Transform muzzle, out Vector3 leadDir))
            return;

        Vector3 current = muzzle.forward;
        float error = Vector3.Angle(current, leadDir);

        float capture = maxCaptureAngle * assistLevel;
        if (error < 0.02f || error > capture)
            return;

        float weight = Mathf.Clamp01((capture - error) / (capture * 0.4f));

        float rate = maxPullRate * assistLevel * weight;
        float fraction = 1f - Mathf.Exp(-rate * Time.deltaTime);
        fraction = Mathf.Min(fraction, maxDegreesPerSecond * assistLevel * Time.deltaTime / error);
        if (fraction <= 0f)
            return;

        Quaternion partial = Quaternion.Slerp(
            Quaternion.identity,
            Quaternion.FromToRotation(current, leadDir),
            fraction
        );

        Quaternion shipRot = ship.transform.rotation;
        Quaternion local = Quaternion.Inverse(shipRot) * partial * shipRot;
        local.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
            angle -= 360f;

        Vector3 degrees = axis * angle;
        ship.AddRotationDegrees(degrees.x, degrees.y); // x = pitch, y = yaw; roll não altera a direção do cano
    }
}
