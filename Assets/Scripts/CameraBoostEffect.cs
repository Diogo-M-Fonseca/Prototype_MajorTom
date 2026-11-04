using UnityEngine;

public class CameraBoostEffect : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private ShipBoost _boost;
    [SerializeField] private CameraFollow _follow;

    [Header("FOV")]
    [SerializeField, Min(0f)] private float _fovIncrease = 35f;
    [SerializeField] private AnimationCurve _fovCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float _punchFov = 15f;
    [SerializeField, Min(0.1f)] private float _punchDecay = 2.5f;

    [Header("Recuo da câmara")]
    [SerializeField, Min(0f)] private float _extraDistance = 6f;

    [Header("Riscos de estrelas (opcional)")]
    [SerializeField] private ParticleSystem _streaks;
    [SerializeField, Min(0f)] private float _streaksMaxRate = 120f;

    private Camera _camera;
    private float _baseFov;
    private float _punch;
    private float _searchTimer;
    private ShipBoost _bound;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _baseFov = _camera.fieldOfView;

        if (_follow == null)
            _follow = GetComponent<CameraFollow>();
    }

    private void OnEnable()
    {
        _camera.fieldOfView = _baseFov;
        Bind(_boost);
    }

    private void OnDisable()
    {
        Bind(null);

        if (_camera != null)
            _camera.fieldOfView = _baseFov;

        if (_follow != null)
            _follow.ExtraDistance = 0f;

        if (_streaks != null)
            _streaks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void Bind(ShipBoost boost)
    {
        if (_bound == boost)
            return;

        if (_bound != null)
            _bound.Started -= OnBoostStarted;

        _bound = boost;

        if (_bound != null)
            _bound.Started += OnBoostStarted;
    }

    private void OnBoostStarted()
    {
        _punch = 1f;
    }

    private void LateUpdate()
    {
        if (_bound == null)
        {
            _searchTimer -= Time.unscaledDeltaTime;

            if (_searchTimer <= 0f)
            {
                _searchTimer = 0.5f;
                _boost = FindFirstObjectByType<ShipBoost>();
                Bind(_boost);
            }
        }

        float intensity = _bound != null ? _bound.Intensity : 0f;
        _punch = Mathf.MoveTowards(_punch, 0f, _punchDecay * Time.deltaTime);

        float fov = _baseFov + _fovCurve.Evaluate(intensity) * _fovIncrease + _punch * _punchFov;
        _camera.fieldOfView = Mathf.Clamp(fov, 1f, 170f);

        if (_follow != null)
            _follow.ExtraDistance = intensity * _extraDistance;

        UpdateStreaks(intensity);
    }

    private void UpdateStreaks(float intensity)
    {
        if (_streaks == null)
            return;

        ParticleSystem.EmissionModule emission = _streaks.emission;
        emission.rateOverTimeMultiplier = _streaksMaxRate * intensity;

        if (intensity > 0.01f && !_streaks.isPlaying)
            _streaks.Play();
        else if (intensity <= 0.01f && _streaks.isPlaying)
            _streaks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}