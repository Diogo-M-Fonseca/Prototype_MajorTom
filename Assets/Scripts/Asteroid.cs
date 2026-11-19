using UnityEngine;

public class Asteroid : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField, Min(1f)] private float _health = 100f;
    [SerializeField, Min(0f)] private float _lifetime = 0f;

    [Header("Pool")]
    [SerializeField] private AsteroidPool _pool;

    [Header("Fragments")]
    [SerializeField, Min(0)] private int _fragmentGenerations = 1;
    [SerializeField] private Vector2Int _fragmentCount = new Vector2Int(3, 5);
    [SerializeField, Range(0.05f, 1f)] private float _fragmentScale = 0.4f;
    [SerializeField, Min(0f)] private float _scatterSpeed = 12f;
    [SerializeField, Min(0f)] private float _maxSpinSpeed = 90f;

    [Header("Resorce")]
    [SerializeField] private Vector2Int _resourceCount = new Vector2Int(1, 2);
    [SerializeField, Range(0f, 1f)] private float _dropChance = 1f;
    [SerializeField, Min(0f)] private float _resourceScatterSpeed = 6f;

    [Header("Feedback")]
    [SerializeField] private AudioClip _breakClip;
    [SerializeField] private ParticleSystem _breakEffect;

    private AsteroidField _field;
    private Rigidbody _body;
    private Renderer _renderer;
    private float _startHealth;
    private bool _isBroken;
    private bool _isPooledFragment;

    public event System.Action Damaged;

    private AsteroidPool Pool => _field != null && _field.Pool != null ? _field.Pool : _pool;
    private int FragmentGenerations => _field != null ? _field.FragmentGenerations : _fragmentGenerations;
    private Vector2Int FragmentCount => _field != null ? _field.FragmentCount : _fragmentCount;
    private float FragmentScale => _field != null ? _field.FragmentScale : _fragmentScale;
    private float ScatterSpeed => _field != null ? _field.ScatterSpeed : _scatterSpeed;
    private float MaxSpinSpeed => _field != null ? _field.MaxSpinSpeed : _maxSpinSpeed;
    private Vector2Int ResourceCount => _field != null ? _field.ResourceCount : _resourceCount;
    private float DropChance => _field != null ? _field.DropChance : _dropChance;
    private float ResourceScatterSpeed => _field != null ? _field.ResourceScatterSpeed : _resourceScatterSpeed;
    private AudioClip BreakClip => _field != null ? _field.BreakClip : _breakClip;
    private ParticleSystem BreakEffect => _field != null ? _field.BreakEffect : _breakEffect;

    private void Awake()
    {
        TryGetComponent(out _body);
        _renderer = GetComponentInChildren<Renderer>();
        _field = GetComponentInParent<AsteroidField>();
        _startHealth = _health;
    }

    private void OnEnable()
    {
        _health = _field != null ? _field.Health : _startHealth;
        _isBroken = false;

        if (_lifetime > 0f)
            Invoke(nameof(Expire), _lifetime);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Expire));
    }

    public void SetPool(AsteroidPool pool)
    {
        _pool = pool;
        _field = null;
        _isPooledFragment = true;
    }

    public void Launch(int fragmentGenerations, Vector3 velocity, Vector3 angularVelocity)
    {
        _fragmentGenerations = fragmentGenerations;

        if (_body == null)
            return;

        _body.useGravity = false;
        _body.linearVelocity = velocity;
        _body.angularVelocity = angularVelocity;
    }

    public void TakeDamage(float damage)
    {
        if (_isBroken || damage <= 0f)
            return;

        _health -= damage;
        Damaged?.Invoke();

        if (_health <= 0f)
            Break();
    }

    private void Break()
    {
        _isBroken = true;

        Vector3 center = transform.position;
        Vector3 inheritedVelocity = _body != null ? _body.linearVelocity : Vector3.zero;
        float radius = GetRadius();

        SpawnFragments(center, inheritedVelocity, radius);
        SpawnResources(center, inheritedVelocity, radius);
        PlayFeedback(center);

        Despawn();
    }

    private void Expire()
    {
        if (_isBroken)
            return;

        _isBroken = true;
        Despawn();
    }

    private void Despawn()
    {
        if (_isPooledFragment && _pool != null)
            _pool.ReturnFragment(this);
        else
            Destroy(gameObject);
    }

    private float GetRadius()
    {
        if (_renderer == null)
            return 1f;

        Vector3 extents = _renderer.bounds.extents;
        return Mathf.Max(extents.x, extents.y, extents.z) * 0.5f;
    }

    private void SpawnFragments(Vector3 center, Vector3 inheritedVelocity, float radius)
    {
        AsteroidPool pool = Pool;
        int generations = FragmentGenerations;

        if (pool == null || generations <= 0)
            return;

        Vector2Int countRange = FragmentCount;
        float scatterSpeed = ScatterSpeed;
        float maxSpinSpeed = MaxSpinSpeed;

        int count = Random.Range(countRange.x, countRange.y + 1);
        Vector3 scale = transform.localScale * FragmentScale;

        for (int i = 0; i < count; i++)
        {
            Vector3 direction = Random.onUnitSphere;

            Asteroid piece = pool.GetFragment(center + direction * radius, Random.rotation, scale);

            piece.Launch(
                generations - 1,
                inheritedVelocity + direction * Random.Range(scatterSpeed * 0.5f, scatterSpeed),
                Random.insideUnitSphere * (maxSpinSpeed * Mathf.Deg2Rad));
        }
    }

    private void SpawnResources(Vector3 center, Vector3 inheritedVelocity, float radius)
    {
        AsteroidPool pool = Pool;

        if (pool == null || Random.value > DropChance)
            return;

        Vector2Int countRange = ResourceCount;
        float scatterSpeed = ResourceScatterSpeed;

        int count = Random.Range(countRange.x, countRange.y + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 direction = Random.onUnitSphere;

            pool.GetResource(
                center + direction * (radius * 0.5f),
                Random.rotation,
                inheritedVelocity + direction * scatterSpeed);
        }
    }

    private void PlayFeedback(Vector3 center)
    {
        AudioClip clip = BreakClip;

        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, center);

        ParticleSystem effectPrefab = BreakEffect;

        if (effectPrefab == null)
            return;

        ParticleSystem effect = Instantiate(effectPrefab, center, Quaternion.identity);
        ParticleSystem.MainModule main = effect.main;
        Destroy(effect.gameObject, main.duration + main.startLifetime.constantMax);
    }
}