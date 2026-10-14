using UnityEngine;

[CreateAssetMenu(menuName = "Ships/AI/Conditions/Random Chance")]
public class RandomChanceCondition : StateCondition
{

    [SerializeField] private float _chancePerSecond = 0.15f;

    public override bool Evaluate(in StateContext context)
    {
        return context.Roll < _chancePerSecond * context.DeltaTime;
    }
    
}
