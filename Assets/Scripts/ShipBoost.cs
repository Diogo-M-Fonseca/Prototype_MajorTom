using System;
using SpaceFlight;
using UnityEngine;

public class ShipBoost : MonoBehaviour
{
    [SerializeField] private SpaceshipController _ship;

    [Header("Transições")]
    [SerializeField, Min(0.01f)] private float _rampInTime = 0.5f;
    [SerializeField, Min(0.01f)] private float _rampOutTime = 1.5f;

    private float _speedMultiplier = 1f;
    private float _remaining;
    private float _intensity;

    public event Action Started;
    public event Action Ended;

    public bool IsActive => _remaining > 0f;
    public float Intensity => _intensity;

    private void Awake()
    {
        if (_ship == null)
            _ship = GetComponent<SpaceshipController>();
    }

    private void OnDisable()
    {
        _remaining = 0f;
        _intensity = 0f;

        if (_ship != null)
            _ship.SetBoost(1f, false);
    }

    public void Activate(float speedMultiplier, float duration)
    {
        bool wasActive = IsActive;

        _speedMultiplier = Mathf.Max(1f, speedMultiplier);
        _remaining = Mathf.Max(0.01f, duration);

        if (!wasActive)
            Started?.Invoke();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (_remaining > 0f)
        {
            _remaining -= dt;

            if (_remaining <= 0f)
            {
                _remaining = 0f;
                Ended?.Invoke();
            }
        }

        float target = IsActive ? 1f : 0f;
        float rampTime = target > _intensity ? _rampInTime : _rampOutTime;
        _intensity = Mathf.MoveTowards(_intensity, target, dt / rampTime);

        _ship.SetBoost(Mathf.Lerp(1f, _speedMultiplier, _intensity), IsActive);
    }
}