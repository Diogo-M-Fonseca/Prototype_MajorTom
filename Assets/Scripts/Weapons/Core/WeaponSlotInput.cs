using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class WeaponSlotInput : MonoBehaviour
{
    [SerializeField] private WeaponManager _manager;
    [SerializeField, Min(0)] private int _leftSlot = 0;
    [SerializeField, Min(0)] private int _rightSlot = 1;

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (_manager == null || mouse == null)
            return;

        UpdateSlot(_leftSlot, mouse.leftButton);
        UpdateSlot(_rightSlot, mouse.rightButton);
    }

    private void OnDisable()
    {
        StopSlot(_leftSlot);
        StopSlot(_rightSlot);
    }

    private void UpdateSlot(int slot, ButtonControl button)
    {
        Weapon weapon = _manager.GetWeapon(slot);

        if (weapon == null)
            return;

        if (button.isPressed)
            weapon.StartFiring();
        else
            weapon.StopFiring();
    }

    private void StopSlot(int slot)
    {
        if (_manager == null)
            return;

        _manager.GetWeapon(slot)?.StopFiring();
    }
}
