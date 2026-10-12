using UnityEngine;

[CreateAssetMenu(menuName = "Ships/Steering/Separation")]
public class SeparationBehaviour : SteeringBehaviour
{
    public override Vector3 Calculate(in SteeringContext context) => Vector3.ClampMagnitude(context.Neighbourhood.SeparationVector, 1f);
}
