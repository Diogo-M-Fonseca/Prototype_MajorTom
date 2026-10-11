using UnityEngine;
using UnityEngine.InputSystem;

public class ProjectilePoolTester : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference fireAction;

    [Header("Projectile")]
    [SerializeField] private ProjectilePool projectilePool;
    [SerializeField] private float speed = 20f;
    [SerializeField] private float range = 30f;

    private void OnEnable()
    {
        if (fireAction == null || fireAction.action == null)
            return;

        fireAction.action.performed += OnFire;
        fireAction.action.Enable();
    }

    private void OnDisable()
    {
        if (fireAction == null || fireAction.action == null)
            return;

        fireAction.action.performed -= OnFire;
        fireAction.action.Disable();
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        if (projectilePool == null)
            return;

        projectilePool.GetProjectile(
            transform.position,
            transform.rotation,
            speed,
            range
        );
    }
}
