using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private ShipStats _stats;
    [SerializeField] private AudioClip _deathClip;

    private float _currentHealth;

    public event Action<float, float> HealthChanged;
    public event Action Damaged;
    public event Action Died;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _stats != null ? _stats.MaxHealth : 0f;

    private void Awake()
    {
        if (_stats == null)
        {
            Debug.LogError($"{name}: falta o ShipStats.", this);
            return;
        }

        _currentHealth = _stats.MaxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (_currentHealth <= 0f)
            return;

        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        HealthChanged?.Invoke(_currentHealth, MaxHealth);
        Damaged?.Invoke();

        if (_currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        if (_deathClip != null)
            AudioSource.PlayClipAtPoint(_deathClip, transform.position);

        Died?.Invoke();

        gameObject.SetActive(false);
    }
}
