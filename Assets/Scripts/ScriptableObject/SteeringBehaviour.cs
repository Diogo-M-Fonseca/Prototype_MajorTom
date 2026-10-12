using UnityEngine;

[CreateAssetMenu(fileName = "SteeringBehaviour", menuName = "Scriptable Objects/SteeringBehaviour")]
public abstract class SteeringBehaviour : ScriptableObject
{
    public abstract Vector3 Calculate(in SteeringContext context);
}
