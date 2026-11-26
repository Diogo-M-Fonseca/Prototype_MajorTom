using UnityEngine;

[DefaultExecutionOrder(100)]
public class AimAssist : MonoBehaviour
{
    [Header("Auto-track (alvo bloqueado)")]
    [SerializeField]
    private bool autoTrackLockedTarget = true;

    [SerializeField, Min(1f)]
    private float lockOnDegreesPerSecond = 180f;

    [SerializeField, Min(0.1f)]
    private float lockOnSharpness = 8f;

    [Header("Assist suave (só quando o auto-track está desligado)")]
    [SerializeField, Range(0f, 1f)]
    private float assistLevel = 0.5f;

    [SerializeField, Min(1f)]
    private float maxCaptureAngle = 20f;

    [SerializeField, Min(0.1f)]
    private float maxPullRate = 8f;

    [SerializeField, Min(1f)]
    private float maxDegreesPerSecond = 90f;

    [Header("Debug")]
    [Tooltip("Desenha no Scene/Game (Gizmos): azul = nariz, amarelo = cano, verde = direção para onde a mira deve ir.")]
    [SerializeField]
    private bool debugDraw;

    private TargetLockSystem lockSystem;
    private SpaceshipController ship;
    private bool warnedMuzzleMismatch;

    public float AssistLevel
    {
        get => assistLevel;
        set => assistLevel = Mathf.Clamp01(value);
    }

    public bool AutoTrackLockedTarget
    {
        get => autoTrackLockedTarget;
        set => autoTrackLockedTarget = value;
    }

    private void Awake()
    {
        lockSystem = GetComponent<TargetLockSystem>();
    }

    private void Update()
    {
        if (ship == null)
            ship = FindFirstObjectByType<SpaceshipController>();
        if (ship == null)
            return;

        // O pedido do frame anterior termina aqui; só é renovado se houver assistência ativa.
        ship.ClearAimAssist();

        if (Time.timeScale <= 0f)
            return;

        if (!autoTrackLockedTarget && assistLevel <= 0f)
            return;

        if (lockSystem == null)
            lockSystem = FindFirstObjectByType<TargetLockSystem>();
        if (lockSystem == null)
            return;

        if (!lockSystem.TryGetAimDirection(out Transform muzzle, out Vector3 aimTarget))
            return;

        bool free = ship.UsesFreeReticle;
        Vector3 nose = ship.Nose;
        Vector3 from;

        if (free)
        {
            from = ship.AimDirection;
        }
        else
        {
            from = muzzle.forward;

            // Cano a apontar para trás do nariz = prefab/flags inconsistentes (invertForward vs FiringPoint).
            // Nesse caso alinha o NARIZ com o alvo (é o que a câmara mostra) e avisa.
            if (Vector3.Dot(from, nose) < 0f)
            {
                from = nose;
                if (!warnedMuzzleMismatch)
                {
                    warnedMuzzleMismatch = true;
                    Debug.LogWarning(
                        "[AimAssist] O FiringPoint aponta para o lado oposto do nariz da nave "
                        + "(SpaceshipController.invertForward vs rotação do FiringPoint). "
                        + "A assistência usa o nariz, mas as balas saem para trás: corrige a prefab.",
                        this);
                }
            }
        }

        if (debugDraw)
        {
            Vector3 o = muzzle.position;
            Debug.DrawRay(o, nose * 30f, Color.blue);
            Debug.DrawRay(o, muzzle.forward * 30f, Color.yellow);
            Debug.DrawRay(o, aimTarget * 30f, Color.green);
        }

        float error = Vector3.Angle(from, aimTarget);
        if (error < 0.02f)
            return;

        float sharpness;
        float maxDegPerSec;

        if (autoTrackLockedTarget)
        {
            sharpness = lockOnSharpness;
            maxDegPerSec = lockOnDegreesPerSecond;
        }
        else
        {
            float capture = maxCaptureAngle * assistLevel;
            if (error > capture)
                return;

            float weight = Mathf.Clamp01((capture - error) / (capture * 0.4f));
            sharpness = maxPullRate * assistLevel * weight;
            maxDegPerSec = maxDegreesPerSecond * assistLevel;
        }

        if (free)
        {
            float speed = Mathf.Min(error * sharpness, maxDegPerSec);
            float step = Mathf.Min(error, speed * Time.deltaTime);
            if (step > 0f)
                ship.RotateAimToward(aimTarget, step / error);
        }
        else
        {
            ship.SetAimAssist(aimTarget, from, sharpness, maxDegPerSec);
        }
    }

    private void OnDisable()
    {
        if (ship != null)
            ship.ClearAimAssist();
    }
}