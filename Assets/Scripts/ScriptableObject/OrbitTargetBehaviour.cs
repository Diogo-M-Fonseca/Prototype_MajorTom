using UnityEngine;

[CreateAssetMenu(menuName = "Ships/Steering/Orbit Target")]
public class OrbitTargetBehaviour : SteeringBehaviour
{
    [SerializeField] private float _desiredDistance = 60f;

    public override Vector3 Calculate(in SteeringContext context)
    {
        if (!context.HasTarget) return Vector3.zero;

        Vector3 toTarget = context.TargetPosition - context.Position;
        float distance = toTarget.magnitude;
        if (distance < 0.01f) return Vector3.zero;

        Vector3 direction = toTarget / distance;

        Vector3 tangent = Vector3.Cross(Vector3.up, direction);
        if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.Cross(Vector3.right, direction);
        tangent.Normalize();

        if (Vector3.Dot(tangent, context.Forward) < 0f) tangent = -tangent;

        float desired = Mathf.Max(_desiredDistance, 0.01f);
        float radial = Mathf.Clamp((distance - desired) / desired, -1f, 1f);

        return (tangent + direction * radial).normalized;
    }

}
