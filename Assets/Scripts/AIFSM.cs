using UnityEngine;

[RequireComponent(typeof(SteeringAgent))]
public class AIFSM : MonoBehaviour
{
    [SerializeField] private AIState _initialState;

    [SerializeField] private EnemyHealth _health;

    [SerializeField, Min(1f)] private float _maxTimeAwayFromInitial = 15f;


    private SteeringAgent _agent;

    private BehaviourProfile _baseProfile;

    private AIState _current;

    private float _elapsed;

    private float _duration;

    private float _timeAway;

    private float _lastDamageTime = float.NegativeInfinity;

    private readonly TargetAim _targetAim = new();


    public AIState Current => _current;

    private void Awake()
    {
        _agent = GetComponent<SteeringAgent>();

        _baseProfile = _agent.Behaviour;

        if (_health == null)
        {
            _health = GetComponent<EnemyHealth>();
        }
    }

    private void OnEnable()
    {
        if (_health != null)
            _health.Damaged += OnDamaged;

        Enter(_initialState);
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.Damaged -= OnDamaged;
    }

    private void OnDamaged()
    {
        _lastDamageTime = Time.time;
    }

    private void Update()
    {
        if (_current == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        _elapsed += dt;

        if (_current != _initialState)
        {
            _timeAway += dt;

            if (_initialState != null && _timeAway > _maxTimeAwayFromInitial)
            {
                Enter(_initialState);
                return;
            }
        }

        if (_elapsed < _current.CommitTime)
        {
            return;
        }

        StateTransition[] transitions = _current.Transitions;

        if (transitions == null || transitions.Length == 0)
        {
            return;
        }

        StateContext context = BuildContext(dt);

        for (int i = 0; i < transitions.Length; i++)
        {
            StateTransition transition = transitions[i];

            if (transition.Next == null || transition.Condition == null)
                continue;

            if (!transition.Condition.Evaluate(context))
                continue;

            Enter(transition.Next);
            return;
        }
    }

    private void Enter(AIState state)
    {
        _current = state;
        _elapsed = 0f;

        if (state == _initialState)
            _timeAway = 0f;

        if (state == null)
            return;

        _duration = state.RollDuration();

        BehaviourProfile profile = state.Profile != null ? state.Profile : _baseProfile;
        if (profile != null)
            _agent.SetBehaviour(profile);
    }

    private StateContext BuildContext(float dt)
    {
        Transform target = _agent.TargetTransform;

        bool hasTarget = target != null;
        float distance = 0f;
        float angle = 0f;
        float aimedAt = 180f;

        if (hasTarget)
        {
            Vector3 toTarget = target.position - transform.position;
            distance = toTarget.magnitude;

            if (distance > 0.0001f)
            {
                angle = Vector3.Angle(transform.forward, toTarget);

                Vector3 aim = _targetAim.Get(target);
                if (aim.sqrMagnitude > 0.0001f)
                    aimedAt = Vector3.Angle(aim, -toTarget);
            }
        }

        float health = _health != null ? _health.HealthFraction : 1f;
        float sinceDamaged = Time.time - _lastDamageTime;

        return new StateContext(_elapsed, _duration, dt, Random.value, hasTarget, distance, angle, health,
            aimedAt, sinceDamaged);
    }
}