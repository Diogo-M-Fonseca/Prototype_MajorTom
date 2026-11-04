using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile Weapon", menuName = "Weapons/Behaviours/Projectile Weapon Behaviour")]
public class ProjectileWeaponBehaviour : WeaponBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField, Min(0.1f)] private float fireRate = 10f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 100f;
    [SerializeField, Min(1f)] private float range = 500f;
    [SerializeField, Min(0f)] private float damage = 10f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip shootClip;

    public override void StartFiring(Weapon weapon)
    {
        // O disparo é tratado continuamente no Tick, então não há necessidade de lógica adicional aqui.
        throw new NotImplementedException();
    }

    public override void StopFiring(Weapon weapon)
    {
        // Não é necessário interromper um projétil já disparado.
        throw new NotImplementedException();
    }

    public override void Tick(Weapon weapon, float deltaTime)
    {
        if (!weapon.IsFiring || !weapon.CanFire)
            return;
        
        FireProjectile(weapon);
        weapon.StartFireCooldown(1f / fireRate);
    }

    private void FireProjectile(Weapon weapon)
    {
        WeaponContext context = weapon.Context;

        if (context.ProjectilePool == null || context.FiringPoint == null)
            return;
        
        context.ProjectilePool.GetProjectile(context.FiringPoint.position, context.FiringPoint.rotation, projectileSpeed, range, damage);

        if (shootClip != null && context.AudioSource != null)
        {
            context.AudioSource.PlayOneShot(shootClip);
        }
    }
}
