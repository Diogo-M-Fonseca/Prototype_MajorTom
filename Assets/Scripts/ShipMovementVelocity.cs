using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipMovementVelocity : MonoBehaviour
{
    [SerializeField] private MovementProfile _profile;

    private Rigidbody _rigidbody;

    public MovementProfile Profile => _profile;
    public Vector3 Velocity => _rigidbody.linearVelocity;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = false;
        _rigidbody.maxAngularVelocity = Mathf.Max(_rigidbody.maxAngularVelocity, _profile.MaxTurnRate * Mathf.Deg2Rad);
    }

    public void Steer(Vector3 desiredDirection, float throttle, float dt)
    {
        Quaternion rotation = _rigidbody.rotation;

        if (desiredDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion goal = Quaternion.LookRotation(desiredDirection, rotation * Vector3.up);
            _rigidbody.angularVelocity = CalculateAngularVelocity(rotation, goal, dt);
        }
        else
        {
            _rigidbody.angularVelocity = Vector3.zero;
        }

        Vector3 forward = rotation * Vector3.forward;
        Vector3 velocity = _rigidbody.linearVelocity;

        float forwardSpeed = Vector3.Dot(velocity, forward);
        Vector3 lateral = velocity - forward * forwardSpeed;

        float wantedSpeed = Mathf.Lerp(_profile.MinSpeed, _profile.MaxSpeed, Mathf.Clamp01(throttle));
        forwardSpeed = Mathf.MoveTowards(forwardSpeed, wantedSpeed, _profile.Acceleration * dt);
        lateral = Vector3.MoveTowards(lateral, Vector3.zero, _profile.LateralGrip * dt);

        _rigidbody.linearVelocity = forward * forwardSpeed + lateral;
    }

    private Vector3 CalculateAngularVelocity(Quaternion current, Quaternion goal, float dt)
    {
        Quaternion delta = goal * Quaternion.Inverse(current);
        delta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f) angle -= 360f;   // caminho mais curto
        if (Mathf.Abs(angle) < 0.01f) return Vector3.zero;

        float maxStep = _profile.MaxTurnRate * dt;
        float step = Mathf.Clamp(angle, -maxStep, maxStep);
        return axis * (step * Mathf.Deg2Rad / dt);
    }
}