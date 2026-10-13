using SpaceFlight;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShipInputAdapter : MonoBehaviour
{
    [SerializeField] SpaceshipController ship;

    [Tooltip("Vector2: x = A/D (roll), y = W/S (thrust)")]
    [SerializeField] InputActionReference move;

    [Tooltip("Vector2 ligado a <Mouse>/delta")]
    [SerializeField] InputActionReference look;

    [SerializeField] bool lockCursor = true;

    void OnEnable()
    {
        move.action.Enable();
        look.action.Enable();
        SetCursorLocked(lockCursor);
    }

    void OnDisable()
    {
        move.action.Disable();
        look.action.Disable();
        SetCursorLocked(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            SetCursorLocked(false);
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame && !lockedNow)
            SetCursorLocked(true);

        Vector2 m = move.action.ReadValue<Vector2>();
        ship.SetInput(m.x, m.y);

        if (lockedNow)
        {
            Vector2 l = look.action.ReadValue<Vector2>();
            ship.AddLook(l.x, l.y);
        }
    }

    bool lockedNow;

    void SetCursorLocked(bool locked)
    {
        lockedNow = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
