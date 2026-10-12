using UnityEngine;

public readonly struct TargetInfo
{
    public bool HasTarget { get; }
    public Vector3 Position { get; }

    public Vector3 Velocity { get; }

    public TargetInfo(bool hasTarget, Vector3 position, Vector3 velocity)
    {
        HasTarget = hasTarget;
        Position = position;
        Velocity = velocity;
    }
}
