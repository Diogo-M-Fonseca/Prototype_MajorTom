using UnityEngine;

public class Projectile : MonoBehaviour
{
    private ProjectilePool projectilePool;
    
    private float speed;
    private float remainingRange;

    public void SetPool(ProjectilePool pool)
    {
        projectilePool = pool;
    }
    
    public void Initialize(float projectileSpeed, float range)
    {
        speed = projectileSpeed;
        remainingRange = range;
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
