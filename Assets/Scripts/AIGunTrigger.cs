using UnityEngine;

public class AIGunTrigger : MonoBehaviour
{
    [SerializeField] private GunController _gun;
    [SerializeField] private Transform _target;
    [SerializeField, Range(0f, 90f)] private float _coneAngle = 10f;
    [SerializeField, Range(0f, 1f)] private float _rangeFraction = 0.9f;

    public void SetTarget(Transform newTarget) => _target = newTarget;

    private void Awake()
    {
        if (_gun != null)
            _gun.SetPlayerInput(false);
    }

    private void Update()
    {
        if (_gun == null)
            return;

        _gun.SetFiring(ShouldFire());
    }

    private void OnDisable()
    {
        if (_gun != null)
            _gun.SetFiring(false);
    }

    private bool ShouldFire()
    {
        if (_target == null)
            return false;

        Vector3 toTarget = _target.position - transform.position;

        if (toTarget.magnitude > _gun.Range * _rangeFraction)
            return false;

        return Vector3.Angle(transform.forward, toTarget) <= _coneAngle;
    }
}
