using UnityEngine;
using UnityEngine.InputSystem;

public class GunController : MonoBehaviour
{
    [Header("Gun Configuration")]
    [SerializeField] private GunData gunData;
    [SerializeField] private ProjectilePool projectilePool;

    [Header("Firing Point")]
    [SerializeField] private Transform firingPoint;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootClip;

    private float nextFireTime;
    private bool isFiring;

    public float Range => gunData != null ? gunData.Range : 0f;

    public void SetFiring(bool firing)
    {
        isFiring = firing;
    }

    private void OnDisable()
    {
        isFiring = false;
    }

    private void Update()
    {
        if (isFiring)
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        if (gunData == null || projectilePool == null || firingPoint == null)
            return;

        if (Time.time < nextFireTime)
            return;

        if (gunData.FireRate <= 0f)
            return;

        float fireInterval = 1f / gunData.FireRate;

        nextFireTime = Time.time + fireInterval;

        if (audioSource != null && shootClip != null)
        {
            audioSource.PlayOneShot(shootClip);
        }

        projectilePool.GetProjectile(
            firingPoint.position,
            firingPoint.rotation,
            gunData.ProjectileSpeed,
            gunData.Range,
            gunData.Damage
        );
    }
}
