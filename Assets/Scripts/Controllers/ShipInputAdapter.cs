using SpaceFlight;
using UnityEngine;
using UnityEngine.InputSystem;
public class ShipInputAdapter : MonoBehaviour
{
    [SerializeField] SpaceshipController ship;
    [SerializeField] InputActionReference move;

    void OnEnable()  => move.action.Enable();
    void OnDisable() => move.action.Disable();

    void Update()
    {
        Vector2 m = move.action.ReadValue<Vector2>();
        ship.SetInput(m.x, m.y);
    }
}
