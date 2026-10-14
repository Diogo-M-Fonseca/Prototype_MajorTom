using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] private Material _flashMaterial;
    [SerializeField, Min(0.01f)] private float _duration = 0.08f;

    private IDamageable _damageable;
    private Renderer[] _renderers;
    private Material[][] _originals;
    private Material[][] _flashed;
    private float _flashEnd;
    private bool _isFlashing;

    private void Awake()
    {
        _damageable = GetComponent<IDamageable>();

        if (_damageable == null)
        {
            Debug.LogError($"{name}: HitFlash precisa de um componente IDamageable no mesmo objeto.", this);
        }

        CacheRenderers();
    }

    private void OnEnable()
    {
        if (_damageable != null)
        {
            _damageable.Damaged += OnDamaged;
        }
    }

    private void OnDisable()
    {
        if (_damageable != null)
        {
            _damageable.Damaged -= OnDamaged;
        }

        Restore();
    }

    private void Update()
    {
        if (_isFlashing && Time.time >= _flashEnd)
        {
            Restore();
        }
    }

    private void CacheRenderers()
    {
        Renderer[] all = GetComponentsInChildren<Renderer>();
        int count = 0;

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] is MeshRenderer || all[i] is SkinnedMeshRenderer)
            {
                count++;
            }
        }

        _renderers = new Renderer[count];
        _originals = new Material[count][];
        _flashed = new Material[count][];

        int index = 0;

        for (int i = 0; i < all.Length; i++)
        {
            if (!(all[i] is MeshRenderer || all[i] is SkinnedMeshRenderer))
            {
                continue;
            }

            Material[] original = all[i].sharedMaterials;
            Material[] flashed = new Material[original.Length];

            for (int m = 0; m < flashed.Length; m++)
            {
                flashed[m] = _flashMaterial;
            }

            _renderers[index] = all[i];
            _originals[index] = original;
            _flashed[index] = flashed;
            index++;
        }
    }

    private void OnDamaged()
    {
        if (_flashMaterial == null)
        {
            Debug.LogWarning($"{name}: HitFlash sem Flash Material atribuído.", this);
            return;
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].sharedMaterials = _flashed[i];
            }
        }

        _flashEnd = Time.time + _duration;
        _isFlashing = true;
    }

    private void Restore()
    {
        if (!_isFlashing)
        {
            return;
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].sharedMaterials = _originals[i];
            }
        }

        _isFlashing = false;
    }
}