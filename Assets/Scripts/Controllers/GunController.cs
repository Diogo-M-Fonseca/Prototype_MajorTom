using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GunController : MonoBehaviour
{
    private const string FireActionPath = "Player/Attack";

    [Header("Gun Configuration")]
    [SerializeField]
    private GunData gunData;

    [SerializeField]
    private ProjectilePool projectilePool;

    [Header("Firing Point")]
    [SerializeField]
    private Transform firingPoint;

    [Header("Velocity Inheritance")]
    [Tooltip(
        "Transform da nave cuja velocidade a bala herda. Se vazio, usa o Rigidbody do pai ou o root."
    )]
    [SerializeField]
    private Transform shipTransform;

    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip shootClip;

    [SerializeField]
    private AudioClip reloadClip;

    [Header("Input")]
    [SerializeField]
    private InputActionReference fireAction;

    [SerializeField]
    private InputActionReference reloadAction;

    [Header("Aim Animation")]
    [SerializeField]
    private Transform aimTransform;

    [Tooltip("Valor 0-1 ao longo do tempo normalizado (0 a 1). Editável no Inspector.")]
    [SerializeField]
    private AnimationCurve aimCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.3f, 1f),
        new Keyframe(1f, 0f)
    );

    [SerializeField]
    private float aimDuration = 0.1f;

    [SerializeField]
    private float aimScaleAmount = 0.3f;

    private Vector3 aimBaseScale = Vector3.one;
    private Coroutine aimCoroutine;

    private float nextFireTime;
    private bool isFiring;
    private bool playerInputAllowed = true;
    private bool isReloading;
    private Coroutine reloadCoroutine;

    private Vector3 lastShipPosition;
    private Vector3 shipVelocity;

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
        if (shipTransform == null)
        {
            Rigidbody body = GetComponentInParent<Rigidbody>();
            shipTransform = body != null ? body.transform : transform.root;
        }
        lastShipPosition = shipTransform.position;

        if (aimTransform != null)
        {
            aimBaseScale = aimTransform.localScale;
        }

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
        if (Time.deltaTime > 0f)
        {
            shipVelocity = (shipTransform.position - lastShipPosition) / Time.deltaTime;
            lastShipPosition = shipTransform.position;
        }

        if (gunData == null || projectilePool == null || firingPoint == null)
        {
            return;
        }

        if (
            playerInputAllowed
            && reloadAction != null
            && reloadAction.action != null
            && reloadAction.action.WasPressedThisFrame()
        )
        {
            StartReload();
        }

        if (isReloading)
        {
            return;
        }

        bool inputPressed =
            playerInputAllowed
            && fireAction != null
            && fireAction.action != null
            && fireAction.action.IsPressed();

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

        float inheritedSpeed = Mathf.Max(0f, Vector3.Dot(shipVelocity, firingPoint.forward));

        projectilePool.GetProjectile(
            firingPoint.position,
            firingPoint.rotation,
            gunData.ProjectileSpeed,
            gunData.Range,
            gunData.Damage,
            inheritedSpeed
        );

        CurrentAmmo--;
        NotifyAmmoChanged();

        if (audioSource != null && shootClip != null)
        {
            audioSource.PlayOneShot(shootClip);
            AnimateAIM();
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

    private void AnimateAIM()
    {
        if (aimTransform == null)
        {
            return;
        }

        if (aimCoroutine != null)
        {
            StopCoroutine(aimCoroutine);
        }

        aimCoroutine = StartCoroutine(AimAnimation());
    }

    private IEnumerator AimAnimation()
    {
        float t = 0f;
        while (t < aimDuration)
        {
            t += Time.deltaTime;
            float curveValue = aimCurve.Evaluate(Mathf.Clamp01(t / aimDuration));
            aimTransform.localScale = aimBaseScale * (1f + curveValue * aimScaleAmount);
            yield return null;
        }

        aimTransform.localScale = aimBaseScale;
        aimCoroutine = null;
    }
}
