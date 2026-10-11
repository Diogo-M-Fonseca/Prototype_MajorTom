using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Audio")]
    [SerializeField] private AudioClip deathClip;
    
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
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
