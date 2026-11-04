using UnityEngine;

[CreateAssetMenu(fileName = "NewLaserWeapon", menuName = "Weapons/Behaviours/Laser Weapon Behaviour")]
public class LaserWeaponBehaviour : WeaponBehaviour
{
    [Header("Charge Settings")]
    [SerializeField, Min(0.1f)] private float chargeTime = 1f;

    public override void StartFiring(Weapon weapon)
    {
        weapon.SetCharging(true);
        weapon.SetChargeProgress(0f);
        weapon.SetLaserActive(false);

        weapon.Context.SetMovementLocked(true);

        weapon.Context.LaserBeam?.StopBeam();
    }

    public override void StopFiring(Weapon weapon)
    {
        weapon.SetCharging(false);
        weapon.SetChargeProgress(0f);
        weapon.SetLaserActive(false);

        weapon.Context.LaserBeam?.StopBeam();

        weapon.Context.SetMovementLocked(false);
    }

    public override void Tick(Weapon weapon, float deltaTime)
    {
        if (!weapon.IsFiring)
            return;

        if (!weapon.IsCharging)
            return;

        float progress =
            weapon.ChargeProgress + deltaTime / chargeTime;

        weapon.SetChargeProgress(progress);

        if (progress >= 1f)
        {
            weapon.SetCharging(false);
            weapon.SetLaserActive(true);

            weapon.Context.LaserBeam?.StartBeam(
                weapon.Context.FiringPoint
            );
        }
    }
}
