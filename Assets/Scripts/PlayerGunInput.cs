using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGunInput : MonoBehaviour
{
    [SerializeField] private GunController _gun;
    [SerializeField] private InputActionReference _fireAction;

    private void OnEnable()
    {
        if (_fireAction != null && _fireAction.action != null)
            _fireAction.action.Enable();
    }

    private void OnDisable()
    {
        if (_fireAction != null && _fireAction.action != null)
            _fireAction.action.Disable();

        if (_gun != null)
            _gun.SetFiring(false);
    }

    private void Update()
    {
        if (_gun == null || _fireAction == null || _fireAction.action == null)
            return;

        _gun.SetFiring(_fireAction.action.IsPressed());
    }
}
