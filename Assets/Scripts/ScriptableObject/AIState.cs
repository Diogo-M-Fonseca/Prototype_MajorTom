using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Ships/AI/State")]
public class AIState : ScriptableObject
{
    [SerializeField] private BehaviourProfile _profile;

    [SerializeField] private float _minDuration = 3f;

    [SerializeField] private float _maxDuration = 6f;

    [SerializeField, Min(0f)] private float _commitTime;

    [SerializeField] private StateTransition[] _transitions;

    public BehaviourProfile Profile => _profile;

    public float CommitTime => _commitTime;

    public StateTransition[] Transitions => _transitions;

    public float RollDuration()
    {
        return _maxDuration > _minDuration ? UnityEngine.Random.Range(_minDuration, _maxDuration) : _minDuration;
    }

}
