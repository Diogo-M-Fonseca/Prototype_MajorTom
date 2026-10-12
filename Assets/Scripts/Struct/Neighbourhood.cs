using UnityEngine;

public readonly struct Neighbourhood
{
    public int Count { get; }

    public Vector3 AveragePosition { get; }

    public Vector3 AverageVelocity { get; }

    public Vector3 SeparationVector { get; }

    public Neighbourhood(int count, Vector3 averagePosition, Vector3 averageVelocity, Vector3 separation)
    {
        Count = count;
        AveragePosition = averagePosition;
        AverageVelocity = averageVelocity;
        SeparationVector = separation;
    }
}
