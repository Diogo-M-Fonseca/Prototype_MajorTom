using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int initialCapacity = 10;
    [SerializeField] private int maxCapacity = 100;

    private ObjectPool<Projectile> pool;

    private void Awake()
    {
        pool = new ObjectPool<Projectile>(
            createFunc: CreateProjectile,
            actionOnGet: OnTakeFromPool,
            actionOnRelease: OnReturnedToPool,
            actionOnDestroy: OnDestroyPoolObject,
            collectionCheck: true,
            defaultCapacity: initialCapacity,
            maxSize: maxCapacity
        );
    }

    private Projectile CreateProjectile()
    {
        Projectile projectile = Instantiate(projectilePrefab,transform);
        projectile.SetPool(this);
        projectile.gameObject.SetActive(false);

        return projectile;
    }

    private void OnTakeFromPool(Projectile projectile)
    {
        projectile.transform.SetParent(null);
        projectile.gameObject.SetActive(true);
    }

    private void OnReturnedToPool(Projectile projectile)
    {
        projectile.transform.SetParent(transform);
        projectile.gameObject.SetActive(false);
    }

    private void OnDestroyPoolObject(Projectile projectile)
    {
        Destroy(projectile.gameObject);
    }

    public Projectile GetProjectile(Vector3 position,Quaternion rotation,float speed,float range)
    {
        Projectile projectile = pool.Get();
        projectile.transform.SetPositionAndRotation(position, rotation);
        projectile.Initialize(speed, range);

        return projectile;
    }

    public void ReturnProjectile(Projectile projectile)
    {
        pool.Release(projectile);
    }
}
