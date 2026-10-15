using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private ShipStats _stats;
    [SerializeField] private AudioClip _deathClip;

    [Header("Defeat")]
    [SerializeField] private GameObject DefeatPanel;
    [SerializeField] private Behaviour[] gameplayBehaviourToDisableOnVictory;
    [SerializeField] private float slowDownDuration = 1.5f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private GameObject playerShipToDisableOnDeath;

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

        playerShipToDisableOnDeath.SetActive(false);

        if (DefeatPanel != null)
        {
            DefeatPanel.SetActive(true);
        }

        foreach (Behaviour behaviour in gameplayBehaviourToDisableOnVictory)
        {
            if (behaviour != null)
            {
                behaviour.enabled = false;
            }
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        StartCoroutine(DefeatSequence());
    }

    private IEnumerator DefeatSequence()
    {
        float elapsedTime = 0f;
        float initialTimeScale = Time.timeScale;

        while (elapsedTime < slowDownDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            
            float progress = elapsedTime / slowDownDuration;
            Time.timeScale = Mathf.Lerp(initialTimeScale, 0f, progress);

            yield return null;
        }

        Time.timeScale = 0f;

        if (DefeatPanel != null)
        {
            DefeatPanel.SetActive(true);
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
