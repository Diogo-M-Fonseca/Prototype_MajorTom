using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(ShipMovementVelocity))]
public class SteeringAgent : MonoBehaviour
{
    [SerializeField] private BehaviourProfile _behaviour;
    [SerializeField] private Transform _target;
    [SerializeField] private Flock _flock;

    private readonly TargetTracker _tracker = new();
    private ShipMovementVelocity _movement;
    private Vector3 _desiredDirection;
    private float _throttle;
    private float _seed;

    public BehaviourProfile Behaviour => _behaviour;
    public Vector3 Position => transform.position;
    public Vector3 Velocity => _movement.Velocity;

    public Transform TargetTransform => _target != null ? _target : (_flock != null ? _flock.Target : null);

    private void Awake()
    {
        _movement = GetComponent<ShipMovementVelocity>();
        _seed = Random.value * 100f;
    }

    private void OnEnable()
    {
        if (_flock != null) _flock.Register(this);
    }

    private void OnDisable()
    {
        if (_flock != null) _flock.Unregister(this);
    }

    private void FixedUpdate()
    {
        if (_flock != null) return;

        float dt = Time.fixedDeltaTime;
        Think(default, default, dt);
        Act(dt);
    }

    public void SetTarget(Transform newTarget) => _target = newTarget;

    public void SetBehaviour(BehaviourProfile profile) => _behaviour = profile;

    public void JoinFlock(Flock newFlock)
    {
        if (_flock != null) _flock.Unregister(this);
        _flock = newFlock;
        _flock.Register(this);
    }

    public void LeaveFlock()
    {
        if (_flock == null) return;
        _flock.Unregister(this);
        _flock = null;
    }

    public void Think(Neighbourhood neighbourhood, TargetInfo flockTarget, float dt)
    {
        TargetInfo own = _tracker.Sample(_target, dt);
        TargetInfo target = own.HasTarget ? own : flockTarget;

        SteeringContext context = new SteeringContext(Position, transform.forward, _movement.Profile.MaxSpeed,
            neighbourhood, target.HasTarget, target.Position, target.Velocity, _seed);

        Vector3 sum = transform.forward;
        foreach (BehaviourProfile.Entry entry in _behaviour.Entries)
            sum += entry.Behaviour.Calculate(context) * entry.Weight;

        _desiredDirection = sum;
        _throttle = Vector3.Dot(transform.forward, sum.normalized);
    }

    public void Act(float dt) => _movement.Steer(_desiredDirection, _throttle, dt);
}