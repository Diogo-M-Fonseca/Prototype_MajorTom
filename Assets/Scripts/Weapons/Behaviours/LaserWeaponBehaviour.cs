using UnityEngine;

[CreateAssetMenu(fileName = "NewLaserWeapon", menuName = "Weapons/Behaviours/Laser Weapon Behaviour")]
public class LaserWeaponBehaviour : WeaponBehaviour
{
    [Header("Charge Settings")]
    [SerializeField, Min(0.1f)] private float chargeTime = 1f;

    [Header("Movimento da nave")]
    [SerializeField, Range(0f, 1f)] private float chargeTurnScale = 0.7f;

    [SerializeField, Range(0f, 1f)] private float firingTurnScale = 0.6f;

    public override void StartFiring(Weapon weapon)
    {
        weapon.SetCharging(true);
        weapon.SetChargeProgress(0f);
        weapon.SetLaserActive(false);

        weapon.Context.SetMovementLimit(1f, 1f);

        weapon.Context.LaserBeam?.StopBeam();
    }

    public override void StopFiring(Weapon weapon)
    {
        weapon.SetCharging(false);
        weapon.SetChargeProgress(0f);
        weapon.SetLaserActive(false);

        weapon.Context.LaserBeam?.StopBeam();

        weapon.Context.SetMovementLimit(1f, 1f);
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

        float charge = Mathf.Clamp01(progress);
        weapon.Context.SetMovementLimit(1f - charge, Mathf.Lerp(1f, chargeTurnScale, charge));

        if (progress >= 1f)
        {
            weapon.SetCharging(false);
            weapon.SetLaserActive(true);

            weapon.Context.SetMovementLimit(0f, firingTurnScale);

            weapon.Context.LaserBeam?.StartBeam(
                weapon.Context.FiringPoint
            );
        }
    }
}
