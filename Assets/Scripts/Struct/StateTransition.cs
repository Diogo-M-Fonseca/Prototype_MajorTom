using UnityEngine;

[System.Serializable]
public struct StateTransition
{
    [SerializeField] private StateCondition _condition;

    [SerializeField] private AIState _next;

    public StateCondition Condition => _condition;
    public AIState Next => _next;
}
