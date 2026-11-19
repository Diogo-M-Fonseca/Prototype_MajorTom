using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Hit Detection")]
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private float hitRadius = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip hitClip;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private ProjectilePool projectilePool;

    private float speed;
    private Vector3 inheritedVelocity;
    private float remainingRange;
    private float damage;

    public void SetPool(ProjectilePool pool)
    {
        projectilePool = pool;
    }

    public void Initialize(float projectileSpeed, float range, float projectileDamage, Vector3 shooterVelocity)
    {
        speed = projectileSpeed;
        inheritedVelocity = shooterVelocity;
        remainingRange = range;
        damage = projectileDamage;
    }

    public void Initialize(float projectileSpeed, float range, float projectileDamage, float shooterSpeed = 0f)
    {
        Initialize(projectileSpeed, range, projectileDamage, transform.forward * shooterSpeed);
    }

    private void Update()
    {
        Vector3 displacement = (transform.forward * speed + inheritedVelocity) * Time.deltaTime;
        float distanceThisFrame = displacement.magnitude;
        Vector3 moveDirection = distanceThisFrame > 1e-6f ? displacement / distanceThisFrame : transform.forward;

        if (debugLogs &&
            Physics.SphereCast(transform.position, hitRadius, moveDirection, out RaycastHit any,
                distanceThisFrame, ~0, QueryTriggerInteraction.Collide))
        {
            Debug.Log($"[Projectile] no caminho: {any.collider.name} " +
                      $"(layer={LayerMask.LayerToName(any.collider.gameObject.layer)}, " +
                      $"trigger={any.collider.isTrigger})", this);
        }

        if (Physics.SphereCast(transform.position, hitRadius, moveDirection, out RaycastHit hit,
                distanceThisFrame, hitMask, QueryTriggerInteraction.Ignore))
        {
            HandleHit(hit);
            return;
        }

        transform.position += displacement;

        remainingRange -= speed * Time.deltaTime;

        if (remainingRange <= 0f)
        {
            ReturnToPool();
        }
    }

    private void HandleHit(RaycastHit hit)
    {
        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

        if (debugLogs)
        {
            Debug.Log($"[Projectile] HIT {hit.collider.name}, damageable={(damageable != null)}", this);
        }

        if (damageable != null)
        {
            if (hitClip != null)
            {
                AudioSource.PlayClipAtPoint(hitClip, hit.point);
            }

            damageable.TakeDamage(damage);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (projectilePool == null)
        {
            Debug.LogError("Projectile has no pool assigned!", this);
            gameObject.SetActive(false);
            return;
        }

        projectilePool.ReturnProjectile(this);
    }
}