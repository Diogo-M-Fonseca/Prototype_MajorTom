using UnityEngine;

public class RecentlyDamagedCondition : StateCondition
{
    [SerializeField, Min(0f)] private float _seconds = 0.3f;

    public override bool Evaluate(in StateContext context)
    {
        return context.SecondsSinceDamaged <= _seconds;
    }
}
