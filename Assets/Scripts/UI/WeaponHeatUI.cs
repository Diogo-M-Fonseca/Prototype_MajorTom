using UnityEngine;
using UnityEngine.UI;

public class WeaponHeatUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Image heatFill;

    [Header("Colours")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color overheatedColor = Color.red;

    private Weapon currentWeapon;

    private void Start()
    {
        if (weaponManager == null || heatFill == null)
        {
            Debug.LogError("WeaponHeatUI: References não configuradas.", this);
            return;
        }

        weaponManager.OnSelectedWeaponChanged += SetWeapon;

        SetWeapon(weaponManager.CurrentWeapon);
    }

    private void OnDestroy()
    {
        if (weaponManager != null)
            weaponManager.OnSelectedWeaponChanged -= SetWeapon;

        UnsubscribeFromWeapon();
    }

    private void SetWeapon(Weapon weapon)
    {
        UnsubscribeFromWeapon();

        currentWeapon = weapon;

        if (currentWeapon == null)
        {
            heatFill.fillAmount = 0f;
            return;
        }

        currentWeapon.OnHeatChanged += UpdateHeatBar;
        currentWeapon.OnOverheatChanged += UpdateOverheatState;

        UpdateHeatBar(currentWeapon.CurrentHeat);
        UpdateOverheatState(currentWeapon.IsOverheated);
    }

    private void UnsubscribeFromWeapon()
    {
        if (currentWeapon == null)
            return;

        currentWeapon.OnHeatChanged -= UpdateHeatBar;
        currentWeapon.OnOverheatChanged -= UpdateOverheatState;
    }

    private void UpdateHeatBar(float heat)
    {
        heatFill.fillAmount = heat;
    }

    private void UpdateOverheatState(bool overheated)
    {
        heatFill.color = overheated ? overheatedColor : normalColor;
    }
}
