using UnityEngine;

public class AimedAtCondition : StateCondition
{
    [SerializeField, Range(1f, 90f)] private float _angle = 15f;

    [SerializeField, Min(1f)] private float _maxDistance = 250f;

    [SerializeField, Min(0f)] private float _chancePerSecond = 0.8f;

    public override bool Evaluate(in StateContext context)
    {
        if (!context.HasTarget || context.DistanceToTarget > _maxDistance || context.AimedAtAngle > _angle)
            return false;

        return _chancePerSecond <= 0f || context.Roll < _chancePerSecond * context.DeltaTime;
    }
}
