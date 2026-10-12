using UnityEngine;

[CreateAssetMenu(menuName = "Ships/Steering/Avoid Obstacles")]
public class AvoidObstaclesBehaviour : SteeringBehaviour
{
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private float _distance = 30f;
    [SerializeField] private float _radius = 2f;

    public override Vector3 Calculate(in SteeringContext context)
    {
        if (!Physics.SphereCast(context.Position, _radius, context.Forward, out RaycastHit hit, _distance, _obstacleMask))
            return Vector3.zero;

        float urgency = 1f - hit.distance / _distance;
        return hit.normal * urgency;
    }
}
