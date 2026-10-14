using UnityEngine;

[CreateAssetMenu(menuName = "Ships/AI/Conditions/Target Angle")]
public class TargetAngleCondition : StateCondition
{
    [SerializeField] private ComparisonMode _mode = ComparisonMode.Above;

    [SerializeField, Range(0f, 180f)] private float _angle = 110f;

    public override bool Evaluate(in StateContext context)
    {
        return context.HasTarget && Compare(context.AngleToTarget, _mode, _angle);
    }
    
}