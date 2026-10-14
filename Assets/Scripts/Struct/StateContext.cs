using UnityEngine;

public class StateContext
{
    public float TimeInState { get; }
    
    public float StateDuration { get; }

    public float DeltaTime { get; }

    public float Roll {  get; }

    public bool HasTarget {  get; }

    public float DistanceToTarget { get; }

    public float AngleToTarget { get; }

    public float HealthFraction { get; }

    public StateContext(float timeInState, float stateDuration, float deltaTime, float roll, bool hasTarget, float distanceToTarget, float angleToTarget, float healthFraction)
    {
        TimeInState = timeInState;
        StateDuration = stateDuration;
        DeltaTime = deltaTime;
        Roll = roll;
        HasTarget = hasTarget;
        DistanceToTarget = distanceToTarget;
        AngleToTarget = angleToTarget;
        HealthFraction = healthFraction;
    }
}
