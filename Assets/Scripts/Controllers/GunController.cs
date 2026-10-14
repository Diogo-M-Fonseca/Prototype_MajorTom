using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

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
    [SerializeField] private AudioClip reloadClip;

    [Header("Input")]
    [SerializeField] private InputActionReference fireAction;
    [SerializeField] private InputActionReference reloadAction;

    private float nextFireTime;
    private bool isFiring;
    private bool playerInputAllowed = true;
    private bool isReloading;
    private Coroutine reloadCoroutine;

    public float Range => gunData != null ? gunData.Range : 0f;
    public event Action<int, int> OnAmmoChanged;
    public int CurrentAmmo { get; private set; }
    public int MaxAmmo => gunData != null ? gunData.MaxAmmo : 0;
    public bool IsReloading => isReloading;

    private void NotifyAmmoChanged()
    {
        OnAmmoChanged?.Invoke(CurrentAmmo, MaxAmmo);
    }

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
        if (gunData == null)
        {
            Debug.LogError("GunController: GunData não foi atribuído.", this);
            return;
        }
        CurrentAmmo = MaxAmmo;
        NotifyAmmoChanged();
    }

    private void OnEnable()
    {
        if (fireAction != null && fireAction.action != null)
        {
            fireAction.action.Enable();
        }

        if (reloadAction != null && reloadAction.action != null)
        {
            reloadAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (fireAction != null && fireAction.action != null)
        {
            fireAction.action.Disable();
        }

        if (reloadAction != null && reloadAction.action != null)
        {
            reloadAction.action.Disable();
        }

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        isReloading = false;
    }

    private void Update()
    {
        if (gunData == null || projectilePool == null || firingPoint == null)
        {
            return;
        }

        if (playerInputAllowed && reloadAction != null && reloadAction.action != null && reloadAction.action.WasPressedThisFrame())
        {
            StartReload();
        }

        if (isReloading)
        {
            return;
        }

        bool inputPressed = playerInputAllowed && fireAction != null && fireAction.action != null && fireAction.action.IsPressed();

        if (isFiring || inputPressed)
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        if (isReloading || Time.time < nextFireTime)
        {
            return;
        }

        if (CurrentAmmo <= 0)
        {
            StartReload();
            return;
        }

        if (gunData.FireRate <= 0f)
        {
            return;
        }

        float fireInterval = 1f / gunData.FireRate;
        nextFireTime = Time.time + fireInterval;

        projectilePool.GetProjectile(firingPoint.position, firingPoint.rotation, gunData.ProjectileSpeed, gunData.Range, gunData.Damage);

        CurrentAmmo--;
        NotifyAmmoChanged();

        if (audioSource != null && shootClip != null)
        {
            audioSource.PlayOneShot(shootClip);
        }

        if (CurrentAmmo <= 0)
        {
            StartReload();
        }
    }

    private void StartReload()
    {
        if (isReloading || CurrentAmmo == MaxAmmo)
        {
            return;
        }

        isReloading = true;

        if (audioSource != null && reloadClip != null)
        {
            audioSource.PlayOneShot(reloadClip);
        }

        reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        yield return new WaitForSeconds(gunData.ReloadDuration);

        CurrentAmmo = MaxAmmo;
        NotifyAmmoChanged();
        isReloading = false;
        reloadCoroutine = null;

        NotifyAmmoChanged();
    }
}