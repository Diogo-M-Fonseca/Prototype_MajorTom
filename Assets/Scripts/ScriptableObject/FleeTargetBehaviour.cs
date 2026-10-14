using UnityEngine;

[CreateAssetMenu(menuName = "Ships/Steering/Flee Target")]
public class FleeTargetBehaviour : SteeringBehaviour
{
    public override Vector3 Calculate(in SteeringContext context)
    {
        if (!context.HasTarget) return Vector3.zero;

        Vector3 away = context.Position - context.TargetPosition;
        return away.sqrMagnitude < 0.0001f ? context.Forward : away.normalized;
    }
}
