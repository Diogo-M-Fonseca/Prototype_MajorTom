using UnityEngine;

[CreateAssetMenu(menuName = "Ships/AI/Conditions/State Time Up")]
public class StateTimeUpCondition : StateCondition
{
    public override bool Evaluate(in StateContext context)
    {
        return context.TimeInState >= context.StateDuration;
    }
    
}
