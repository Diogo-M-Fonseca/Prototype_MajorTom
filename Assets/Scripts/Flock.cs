using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Flock : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("target")] private Transform _target;

    private readonly List<SteeringAgent> _agents = new();
    private readonly TargetTracker _tracker = new();

    public void SetTarget(Transform newTarget) => _target = newTarget;

    public void Register(SteeringAgent agent)
    {
        if (!_agents.Contains(agent)) _agents.Add(agent);
    }

    public void Unregister(SteeringAgent agent) => _agents.Remove(agent);

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        TargetInfo flockTarget = _tracker.Sample(_target, dt);

        for (int i = 0; i < _agents.Count; i++)
            _agents[i].Think(BuildNeighbourhood(_agents[i]), flockTarget, dt);

        for (int i = 0; i < _agents.Count; i++)
            _agents[i].Act(dt);
    }

    private Neighbourhood BuildNeighbourhood(SteeringAgent self)
    {
        float neighbourRadius = self.Behaviour.NeighbourRadius;
        float separationRadius = self.Behaviour.SeparationRadius;
        float neighbourSqr = neighbourRadius * neighbourRadius;

        int count = 0;
        Vector3 positionSum = Vector3.zero;
        Vector3 velocitySum = Vector3.zero;
        Vector3 separation = Vector3.zero;

        for (int i = 0; i < _agents.Count; i++)
        {
            SteeringAgent other = _agents[i];
            if (other == self) continue;

            Vector3 offset = other.Position - self.Position;
            float sqr = offset.sqrMagnitude;
            if (sqr > neighbourSqr) continue;

            count++;
            positionSum += other.Position;
            velocitySum += other.Velocity;

            if (sqr < separationRadius * separationRadius)
            {
                float dist = Mathf.Max(Mathf.Sqrt(sqr), 0.01f);
                separation -= offset / dist * (1f - dist / separationRadius);
            }
        }

        if (count == 0) return default;
        return new Neighbourhood(count, positionSum / count, velocitySum / count, separation);
    }
}