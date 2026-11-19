using UnityEngine;
using UnityEngine.Pool;

public class AsteroidPool : MonoBehaviour
{
    [Header("Fragmentos")]
    [SerializeField] private Asteroid _fragmentPrefab;
    [SerializeField, Min(0)] private int _fragmentInitialCapacity = 30;
    [SerializeField, Min(1)] private int _fragmentMaxCapacity = 300;
    [SerializeField, Min(0)] private int _fragmentPrewarm = 0;

    [Header("Recursos")]
    [SerializeField] private ResourcePickup _resourcePrefab;
    [SerializeField, Min(0)] private int _resourceInitialCapacity = 20;
    [SerializeField, Min(1)] private int _resourceMaxCapacity = 200;
    [SerializeField, Min(0)] private int _resourcePrewarm = 0;

    private ObjectPool<Asteroid> _fragments;
    private ObjectPool<ResourcePickup> _resources;

    private void Awake()
    {
        _fragments = new ObjectPool<Asteroid>(
            createFunc: CreateFragment,
            actionOnGet: OnTakeFragment,
            actionOnRelease: OnReturnedFragment,
            actionOnDestroy: OnDestroyFragment,
            collectionCheck: true,
            defaultCapacity: _fragmentInitialCapacity,
            maxSize: _fragmentMaxCapacity
        );

        _resources = new ObjectPool<ResourcePickup>(
            createFunc: CreateResource,
            actionOnGet: OnTakeResource,
            actionOnRelease: OnReturnedResource,
            actionOnDestroy: OnDestroyResource,
            collectionCheck: true,
            defaultCapacity: _resourceInitialCapacity,
            maxSize: _resourceMaxCapacity
        );

        Prewarm();
    }

    private Asteroid CreateFragment()
    {
        Asteroid fragment = Instantiate(_fragmentPrefab, transform);
        fragment.SetPool(this);
        fragment.gameObject.SetActive(false);

        return fragment;
    }

    private void OnTakeFragment(Asteroid fragment)
    {
        fragment.transform.SetParent(null);
        fragment.gameObject.SetActive(true);
    }

    private void OnReturnedFragment(Asteroid fragment)
    {
        fragment.transform.SetParent(transform);
        fragment.gameObject.SetActive(false);
    }

    private void OnDestroyFragment(Asteroid fragment)
    {
        Destroy(fragment.gameObject);
    }

    public Asteroid GetFragment(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Asteroid fragment = _fragments.Get();
        fragment.transform.SetPositionAndRotation(position, rotation);
        fragment.transform.localScale = scale;

        return fragment;
    }

    public void ReturnFragment(Asteroid fragment)
    {
        _fragments.Release(fragment);
    }

    private ResourcePickup CreateResource()
    {
        ResourcePickup resource = Instantiate(_resourcePrefab, transform);
        resource.SetPool(this);
        resource.gameObject.SetActive(false);

        return resource;
    }

    private void OnTakeResource(ResourcePickup resource)
    {
        resource.transform.SetParent(null);
        resource.gameObject.SetActive(true);
    }

    private void OnReturnedResource(ResourcePickup resource)
    {
        resource.transform.SetParent(transform);
        resource.gameObject.SetActive(false);
    }

    private void OnDestroyResource(ResourcePickup resource)
    {
        Destroy(resource.gameObject);
    }

    public ResourcePickup GetResource(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        ResourcePickup resource = _resources.Get();
        resource.transform.SetPositionAndRotation(position, rotation);
        resource.Launch(velocity);

        return resource;
    }

    public void ReturnResource(ResourcePickup resource)
    {
        _resources.Release(resource);
    }

    private void Prewarm()
    {
        if (_fragmentPrefab != null)
        {
            Asteroid[] fragments = new Asteroid[_fragmentPrewarm];

            for (int i = 0; i < fragments.Length; i++)
                fragments[i] = _fragments.Get();

            for (int i = 0; i < fragments.Length; i++)
                _fragments.Release(fragments[i]);
        }

        if (_resourcePrefab != null)
        {
            ResourcePickup[] resources = new ResourcePickup[_resourcePrewarm];

            for (int i = 0; i < resources.Length; i++)
                resources[i] = _resources.Get();

            for (int i = 0; i < resources.Length; i++)
                _resources.Release(resources[i]);
        }
    }
}