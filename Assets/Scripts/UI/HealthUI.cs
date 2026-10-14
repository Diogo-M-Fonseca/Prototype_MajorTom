using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image healthBar;

    private void Update()
    {
        healthBar.fillAmount = playerHealth.CurrentHealth / playerHealth.MaxHealth;
    }
}
