using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private ShipStats stats;

    [Header("Audio")]
    [SerializeField] private AudioClip deathClip;

    private float currentHealth;

    private void Awake()
    {
        if (stats == null)
        {
            Debug.LogError($"{name}: falta o ShipStats.", this);
            return;
        }

        currentHealth = stats.MaxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0f)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (deathClip != null)
        {
            AudioSource.PlayClipAtPoint(deathClip, transform.position);
        }

        Destroy(gameObject);
    }
}