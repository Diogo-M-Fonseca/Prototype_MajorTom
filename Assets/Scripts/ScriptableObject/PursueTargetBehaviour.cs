using UnityEngine;

[CreateAssetMenu(menuName = "Ships/Steering/Pursue Target")]
public class PursueTargetBehaviour : SteeringBehaviour
{
    [SerializeField] private float _leadFactor = 1f;

    public override Vector3 Calculate(in SteeringContext context)
    {
        if (!context.HasTarget) return Vector3.zero;

        Vector3 toTarget = context.TargetPosition - context.Position;
        float time = toTarget.magnitude / Mathf.Max(context.MaxSpeed, 0.01f) * _leadFactor;
        Vector3 predicted = context.TargetPosition + context.TargetVelocity * time;
        return (predicted - context.Position).normalized;
    }
    
}
