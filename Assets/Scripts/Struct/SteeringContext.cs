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

    public Vector3 TargetForward { get; }

    public Vector3 Velocity { get; }

    public float ProjectileSpeed { get; }

    public SteeringContext(Vector3 position, Vector3 forward, float maxSpeed, Neighbourhood neighbourhood, bool hasTarget, Vector3 targetPosition, Vector3 targetVelocity, float seed,
        Vector3 targetForward = default, Vector3 velocity = default, float projectileSpeed = 0f)
    {
        Position = position;
        Forward = forward;
        MaxSpeed = maxSpeed;
        Neighbourhood = neighbourhood;
        HasTarget = hasTarget;
        TargetPosition = targetPosition;
        TargetVelocity = targetVelocity;
        Seed = seed;
        TargetForward = targetForward;
        Velocity = velocity;
        ProjectileSpeed = projectileSpeed;
    }

}
