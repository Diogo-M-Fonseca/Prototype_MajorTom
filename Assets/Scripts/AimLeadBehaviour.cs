using UnityEngine;

public class AimLeadBehaviour : SteeringBehaviour
{
    public override Vector3 Calculate(in SteeringContext context)
    {
        if (!context.HasTarget) return Vector3.zero;

        Vector3 direct = context.TargetPosition - context.Position;
        if (direct.sqrMagnitude < 0.0001f) return Vector3.zero;

        if (context.ProjectileSpeed > 0.01f &&
            LeadSolver.Solve(context.Position, context.TargetPosition, context.TargetVelocity,
                context.ProjectileSpeed, context.Velocity, true, out Vector3 intercept, out _))
        {
            Vector3 lead = intercept - context.Position;
            if (lead.sqrMagnitude > 0.0001f)
                return lead.normalized;
        }

        return direct.normalized;
    }
}
