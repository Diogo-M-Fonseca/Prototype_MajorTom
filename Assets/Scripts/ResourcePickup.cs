using System.Collections.Generic;
using UnityEngine;

public class ResourcePickup : MonoBehaviour
{
    public static readonly List<ResourcePickup> All = new();

    [Header("Floating")]
    [SerializeField, Min(0f)] private float _drag = 1.5f;
    [SerializeField] private Vector3 _spinDegreesPerSecond = new Vector3(20f, 60f, 10f);

    [Header("Duration")]
    [SerializeField, Min(0f)] private float _lifetime = 0f;

    private AsteroidPool _pool;
    private float _age;

    private ResourcePlanet _targetPlanet;
    private float _flySpeed;
    private float _flyAcceleration;
    private float _flyDelay;

    public bool IsCarried { get; private set; }

    public Vector3 Velocity { get; set; }

    private void OnEnable()
    {
        IsCarried = false;
        Velocity = Vector3.zero;
        _age = 0f;
        _targetPlanet = null;

        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        transform.Rotate(_spinDegreesPerSecond * dt, Space.Self);

        if (_targetPlanet != null)
        {
            FlyToPlanet(dt);
            return;
        }

        if (IsCarried)
            return;

        Velocity *= Mathf.Exp(-_drag * dt);
        transform.position += Velocity * dt;

        if (_lifetime > 0f)
        {
            _age += dt;

            if (_age >= _lifetime)
                Despawn();
        }
    }

    public void SetPool(AsteroidPool pool)
    {
        _pool = pool;
    }

    public void Launch(Vector3 velocity)
    {
        Velocity = velocity;
    }

    public void MarkCollected()
    {
        IsCarried = true;
        All.Remove(this);
    }

    public void Consume()
    {
        Despawn();
    }

    public void DeliverTo(ResourcePlanet planet, float speed, float acceleration, float delay)
    {
        _targetPlanet = planet;
        _flySpeed = speed;
        _flyAcceleration = acceleration;
        _flyDelay = delay;
    }

    private void FlyToPlanet(float dt)
    {
        if (_targetPlanet == null || !_targetPlanet.isActiveAndEnabled)
        {
            _targetPlanet = null;
            Despawn();
            return;
        }

        if (_flyDelay > 0f)
        {
            _flyDelay -= dt;
            Velocity *= Mathf.Exp(-_drag * dt);
            transform.position += Velocity * dt;
            return;
        }

        Vector3 toTarget = _targetPlanet.transform.position - transform.position;
        float distance = toTarget.magnitude;

        float enterDistance = Mathf.Max(0.5f, _targetPlanet.SurfaceRadius * 0.9f);

        if (distance <= enterDistance)
        {
            ResourcePlanet planet = _targetPlanet;
            _targetPlanet = null;
            planet.Receive();
            Despawn();
            return;
        }

        Vector3 desired = toTarget / distance * _flySpeed;
        Velocity = Vector3.MoveTowards(Velocity, desired, _flyAcceleration * dt);

        Vector3 step = Velocity * dt;

        if (step.sqrMagnitude > toTarget.sqrMagnitude)
            step = toTarget;

        transform.position += step;
    }

    private void Despawn()
    {
        if (_pool != null)
            _pool.ReturnResource(this);
        else
            Destroy(gameObject);
    }
}