using UnityEngine;

public class EvadeBehaviour : SteeringBehaviour
{
    [SerializeField, Min(1f)] private float _maxDistance = 300f;
    [SerializeField, Min(0f)] private float _weaveFrequency = 2.5f;
    [SerializeField, Range(0f, 1f)] private float _weaveAmount = 0.5f;

    public override Vector3 Calculate(in SteeringContext context)
    {
        if (!context.HasTarget) return Vector3.zero;

        Vector3 aim = context.TargetForward;
        if (aim.sqrMagnitude < 0.0001f) return Vector3.zero;
        aim.Normalize();

        Vector3 toSelf = context.Position - context.TargetPosition;
        float distance = toSelf.magnitude;
        if (distance < 0.01f || distance > _maxDistance) return Vector3.zero;

        Vector3 direction = toSelf / distance;

        Vector3 perpendicular = direction - aim * Vector3.Dot(direction, aim);
        if (perpendicular.sqrMagnitude < 0.0001f) perpendicular = Vector3.Cross(aim, Vector3.up);
        if (perpendicular.sqrMagnitude < 0.0001f) perpendicular = Vector3.Cross(aim, Vector3.right);
        perpendicular.Normalize();

        Vector3 side = Vector3.Cross(aim, perpendicular);

        float phase = Time.time * _weaveFrequency + context.Seed;
        Vector3 weave = perpendicular * Mathf.Cos(phase) + side * Mathf.Sin(phase);

        return (perpendicular * (1f - _weaveAmount) + weave * _weaveAmount).normalized;
    }
}
