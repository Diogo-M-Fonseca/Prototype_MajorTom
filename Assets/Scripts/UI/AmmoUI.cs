using UnityEngine;
using TMPro;

public class AmmoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private GunController gunController;

    private void OnEnable()
    {
        if (gunController == null)
            return;

        gunController.OnAmmoChanged += UpdateAmmo;
        UpdateAmmo(gunController.CurrentAmmo, gunController.MaxAmmo);
    }

    private void OnDisable()
    {
        if (gunController != null)
        {
            gunController.OnAmmoChanged -= UpdateAmmo;
        }
    }

    private void UpdateAmmo(int currentAmmo, int maxAmmo)
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo}";
        }
    }
}
