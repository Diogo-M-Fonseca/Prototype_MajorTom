using System;
using UnityEngine;

public class PlayerShield : MonoBehaviour
{
    [SerializeField] private GameObject _visual;

    private float _current;
    private float _max;
    private float _remaining;

    public event Action<float, float> Changed;

    public bool IsActive => _current > 0f && _remaining > 0f;
    public float Current => _current;
    public float Max => _max;

    private void Awake()
    {
        if (_visual != null)
            _visual.SetActive(false);
    }

    public void Activate(float amount, float duration)
    {
        _max = Mathf.Max(0f, amount);
        _current = _max;
        _remaining = Mathf.Max(0.01f, duration);
        Refresh();
    }

    public float Absorb(float damage)
    {
        if (!IsActive || damage <= 0f)
            return damage;

        float absorbed = Mathf.Min(_current, damage);
        _current -= absorbed;
        Refresh();

        return damage - absorbed;
    }

    private void Update()
    {
        if (_remaining <= 0f)
            return;

        _remaining -= Time.deltaTime;

        if (_remaining <= 0f)
        {
            _remaining = 0f;
            _current = 0f;
            Refresh();
        }
    }

    private void Refresh()
    {
        if (_visual != null)
            _visual.SetActive(IsActive);

        Changed?.Invoke(_current, _max);
    }
}