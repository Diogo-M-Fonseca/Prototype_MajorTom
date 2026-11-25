using UnityEngine;

public class TargetAim
{
    private Transform _target;
    private SpaceshipController _ship;

    public Vector3 Get(Transform target)
    {
        if (target == null)
        {
            _target = null;
            _ship = null;
            return Vector3.zero;
        }

        if (target != _target)
        {
            _target = target;
            _ship = target.GetComponent<SpaceshipController>();
        }

        return _ship != null ? _ship.AimDirection : target.forward;
    }

}
