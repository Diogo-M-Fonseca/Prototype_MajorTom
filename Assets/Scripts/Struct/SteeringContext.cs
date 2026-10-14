using UnityEngine;

public readonly struct SteeringContext
{
    public Vector3 Position { get; }

    public Vector3 Forward { get; }

    public float MaxSpeed { get; }

    public Neighbourhood Neighbourhood { get; }

    public bool HasTarget { get; }

    public Vector3 TargetPosition { get; }

    public Vector3 TargetVelocity { get; }

    public float Seed { get; }

    public SteeringContext(Vector3 position, Vector3 forward, float maxSpeed, Neighbourhood neighbourhood, bool hasTarget, Vector3 targetPosition, Vector3 targetVelocity, float seed)
    {
        Position = position;
        Forward = forward;
        MaxSpeed = maxSpeed;
        Neighbourhood = neighbourhood;
        HasTarget = hasTarget;
        TargetPosition = targetPosition;
        TargetVelocity = targetVelocity;
        Seed = seed;
    }

}
