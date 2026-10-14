using UnityEngine;

[CreateAssetMenu(menuName = "Ships/AI/Conditions/Health")]
public class HealthCondition : StateCondition
{
    [SerializeField] private ComparisonMode _mode = ComparisonMode.Below;

    [SerializeField, Range(0f, 1f)] private float _fraction = 0.25f;

    public override bool Evaluate(in StateContext context)
    {
        return Compare(context.HealthFraction, _mode, _fraction);
    }
    
}
