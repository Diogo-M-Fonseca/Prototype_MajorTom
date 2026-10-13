using UnityEngine;

[CreateAssetMenu(menuName = "Ships/Steering/Alignment")]
public class AlignmentBehaviour : SteeringBehaviour
{
    public override Vector3 Calculate(in SteeringContext context) => context.Neighbourhood.Count == 0 ? Vector3.zero : context.Neighbourhood.AverageVelocity.normalized;
}
