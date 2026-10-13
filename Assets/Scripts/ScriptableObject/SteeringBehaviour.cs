using UnityEngine;

public abstract class SteeringBehaviour : ScriptableObject
{
    public abstract Vector3 Calculate(in SteeringContext context);
}
