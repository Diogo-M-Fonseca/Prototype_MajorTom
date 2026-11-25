using UnityEngine;

public readonly struct TargetInfo
{
    public bool HasTarget { get; }
    public Vector3 Position { get; }

    public Vector3 Velocity { get; }

    public Vector3 Forward { get; }

    public TargetInfo(bool hasTarget, Vector3 position, Vector3 velocity, Vector3 forward = default)
    {
        HasTarget = hasTarget;
        Position = position;
        Velocity = velocity;
        Forward = forward;
    }
}
