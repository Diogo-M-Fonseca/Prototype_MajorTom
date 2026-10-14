using UnityEngine;

[CreateAssetMenu(menuName = "Ships/AI/Conditions/Target Distance")]
public class TargetDistanceCondition : StateCondition
{
    [SerializeField] private ComparisonMode _mode = ComparisonMode.Below;

    [SerializeField] private float _distance = 25f;

    public override bool Evaluate(in StateContext context)
    {
        return context.HasTarget && Compare(context.DistanceToTarget, _mode, _distance);
    }


}
