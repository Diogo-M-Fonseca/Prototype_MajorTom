using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(ShipMovementVelocity))]
public class SteeringAgent : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("behaviour")] private BehaviourProfile _behaviour;
    [SerializeField, FormerlySerializedAs("target")] private Transform _target;
    [SerializeField, FormerlySerializedAs("flock")] private Flock _flock;   

    private readonly TargetTracker _tracker = new();
    private ShipMovementVelocity _movement;
    private Vector3 _desiredDirection;
    private float _throttle;

    public BehaviourProfile Behaviour => _behaviour;
    public Vector3 Position => transform.position;
    public Vector3 Velocity => _movement.Velocity;

    private void Awake() => _movement = GetComponent<ShipMovementVelocity>();

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
            neighbourhood, target.HasTarget, target.Position, target.Velocity);

        Vector3 sum = transform.forward;
        foreach (BehaviourProfile.Entry entry in _behaviour.Entries)
            sum += entry.Behaviour.Calculate(context) * entry.Weight;

        _desiredDirection = sum;
        _throttle = Vector3.Dot(transform.forward, sum.normalized);
    }

    public void Act(float dt) => _movement.Steer(_desiredDirection, _throttle, dt);
}
