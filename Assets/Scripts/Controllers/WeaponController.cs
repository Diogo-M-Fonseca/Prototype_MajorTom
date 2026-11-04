using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponManager weaponManager;

    [Header("Input")]
    [SerializeField] private InputActionReference fireAction;
    [SerializeField] private InputActionReference weaponScrollAction;

    private void OnEnable()
    {
        fireAction?.action.Enable();
        weaponScrollAction?.action.Enable();
    }

    private void OnDisable()
    {
        fireAction?.action.Disable();
        weaponScrollAction?.action.Disable();

        weaponManager?.CurrentWeapon?.StopFiring();
    }

    private void Update()
    {
        if (weaponManager == null)
            return;

        HandleWeaponSelection();
        HandleFiring();
    }

    private void HandleWeaponSelection()
    {
        if (weaponScrollAction == null)
            return;

        float scroll = weaponScrollAction.action.ReadValue<Vector2>().y;

        if (scroll > 0.1f)
        {
            weaponManager.SelectNextWeapon();
        }
        else if (scroll < -0.1f)
        {
            weaponManager.SelectPreviousWeapon();
        }
    }

    private void HandleFiring()
    {
        Weapon currentWeapon = weaponManager.CurrentWeapon;

        if (currentWeapon == null || fireAction == null)
            return;

        if (fireAction.action.IsPressed())
            currentWeapon.StartFiring();
        else
            currentWeapon.StopFiring();
    }
}