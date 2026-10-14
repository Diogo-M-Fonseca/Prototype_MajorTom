using UnityEngine;


public abstract class StateCondition : ScriptableObject
{
    public abstract bool Evaluate(in StateContext context);

    protected static bool Compare(float value, ComparisonMode mode, float threshold)
    {
        return mode == ComparisonMode.Below ? value < threshold : value > threshold;
    } 
}
