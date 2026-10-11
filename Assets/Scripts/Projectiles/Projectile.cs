using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip hitClip;
    
    private ProjectilePool projectilePool;
    
    private float speed;
    private float remainingRange;
    private float damage;

    public void SetPool(ProjectilePool pool)
    {
        projectilePool = pool;
    }
    
    public void Initialize(float projectileSpeed, float range, float projectileDamage)
    {
        speed = projectileSpeed;
        remainingRange = range;
        damage = projectileDamage;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            if (hitClip != null)
            {
                AudioSource.PlayClipAtPoint(hitClip, transform.position);
            }
            
            damageable.TakeDamage(damage);
            ReturnToPool();
        }
    }

    private void Update()
    {
        float distanceThisFrame = speed * Time.deltaTime;

        transform.position += transform.forward * distanceThisFrame;

        remainingRange -= distanceThisFrame;

        if (remainingRange <= 0f)
        {
            ReturnToPool();
        }
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
