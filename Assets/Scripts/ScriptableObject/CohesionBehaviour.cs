using UnityEngine;

[CreateAssetMenu(menuName = "Ships/Steering/Cohesion")]
public class CohesionBehaviour : SteeringBehaviour
{
    public override Vector3 Calculate(in SteeringContext context)
    {
       return context.Neighbourhood.Count == 0 ? Vector3.zero : (context.Neighbourhood.AveragePosition - context.Position).normalized;
    }
}
