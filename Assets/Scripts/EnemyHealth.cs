using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private ShipStats stats;

    [Header("Score")]
    [SerializeField, Min(0)] private int scoreValue = 10;
    [SerializeField] private ScoreManager scoreManager;

    [Header("Audio")]
    [SerializeField] private AudioClip deathClip;

    private float _currentHealth;
    private bool _isDead;

    private void Awake()
    {
        if (stats == null)
        {
            Debug.LogError($"{name}: falta o ShipStats.", this);
            return;
        }

        _currentHealth = stats.MaxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (_currentHealth <= 0f)
            return;

        _currentHealth -= damage;

        if (_currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDead)
            return;

        _isDead = true;

        if(scoreManager != null)
        {
            scoreManager.AddScore(scoreValue);
        }
        
        if (deathClip != null)
        {
            AudioSource.PlayClipAtPoint(deathClip, transform.position);
        }

        Destroy(gameObject);
    }
}