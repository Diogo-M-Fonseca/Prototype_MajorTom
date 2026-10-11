using UnityEngine;
using UnityEngine.InputSystem;

public class GunController : MonoBehaviour
{
    [Header("Gun Configuration")]
    [SerializeField] private GunData gunData;
    [SerializeField] private ProjectilePool projectilePool;

    [Header("Input")]
    [SerializeField] private InputActionReference fireAction;

    [Header("Firing Point")]
    [SerializeField] private Transform firingPoint;

    private float nextFireTime;

    private void OnEnable()
    {
        if (fireAction != null && fireAction.action != null)
        {
            fireAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (fireAction != null && fireAction.action != null)
        {
            fireAction.action.Disable();
        }
    }

    private void Update()
    {
        if (fireAction == null || fireAction.action == null)
            return;

        if (gunData == null || projectilePool == null || firingPoint == null)
            return;

        if (fireAction.action.IsPressed())
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime)
            return;

        if (gunData.FireRate <= 0f)
            return;

        float fireInterval = 1f / gunData.FireRate;

        nextFireTime = Time.time + fireInterval;

        projectilePool.GetProjectile(
            firingPoint.position,
            firingPoint.rotation,
            gunData.ProjectileSpeed,
            gunData.Range
        );
    }
}
