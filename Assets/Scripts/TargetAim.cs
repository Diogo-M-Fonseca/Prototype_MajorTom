using UnityEngine;

public class TargetAim
{
    private Transform _target;
    private SpaceshipController _ship;
    private Transform _muzzle;

    public Vector3 Get(Transform target)
    {
        if (target == null)
        {
            _target = null;
            _ship = null;
            _muzzle = null;
            return Vector3.zero;
        }

        if (target != _target)
        {
            _target = target;
            _ship = target.GetComponent<SpaceshipController>();
            _muzzle = null;

            if (_ship != null)
            {
                WeaponManager weapons = _ship.GetComponentInChildren<WeaponManager>();
                if (weapons != null)
                    _muzzle = weapons.FiringPoint;

                if (_muzzle == null)
                {
                    GunController gun = _ship.GetComponentInChildren<GunController>();
                    if (gun != null)
                        _muzzle = gun.FiringPoint;
                }
            }
        }

        // Direção para onde o alvo realmente dispara (cano), não o reticle livre.
        if (_muzzle != null)
            return _muzzle.forward;

        return _ship != null ? _ship.Nose : target.forward;
    }
}
