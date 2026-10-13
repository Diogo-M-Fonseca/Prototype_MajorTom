using UnityEngine;
using UnityEngine.InputSystem;

public class GunController : MonoBehaviour
{
    private const string FireActionPath = "Player/Attack";

    [Header("Gun Configuration")]
    [SerializeField] private GunData gunData;
    [SerializeField] private ProjectilePool projectilePool;

    [Header("Firing Point")]
    [SerializeField] private Transform firingPoint;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootClip;

    private InputAction fireAction;
    private float nextFireTime;
    private bool isFiring;
    private bool playerInputAllowed = true;

    public float Range => gunData != null ? gunData.Range : 0f;

    public void SetFiring(bool firing)
    {
        isFiring = firing;
    }

    public void SetPlayerInput(bool allowed)
    {
        playerInputAllowed = allowed;
    }

    private void Awake()
    {
        if (InputSystem.actions != null)
        {
            fireAction = InputSystem.actions.FindAction(FireActionPath);
        }

        if (fireAction == null)
        {
            Debug.LogWarning($"{name}: ação '{FireActionPath}' não encontrada em InputSystem.actions.", this);
        }
    }

    private void OnDisable()
    {
        isFiring = false;
    }

    private void Update()
    {
        bool inputPressed = playerInputAllowed && fireAction != null && fireAction.IsPressed();

        if (isFiring || inputPressed)
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        if (gunData == null || projectilePool == null || firingPoint == null)
            return;

        if (Time.time < nextFireTime)
            return;

        if (gunData.FireRate <= 0f)
            return;

        float fireInterval = 1f / gunData.FireRate;

        nextFireTime = Time.time + fireInterval;

        if (audioSource != null && shootClip != null)
        {
            audioSource.PlayOneShot(shootClip);
        }

        projectilePool.GetProjectile(
            firingPoint.position,
            firingPoint.rotation,
            gunData.ProjectileSpeed,
            gunData.Range,
            gunData.Damage
        );
    }
}