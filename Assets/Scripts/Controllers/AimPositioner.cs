using UnityEngine;


[DefaultExecutionOrder(10000)]
public class AimPositioner : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform shipTransform;


    [SerializeField]
    private Transform aimOrigin;

    [SerializeField]
    private RectTransform aimRect;

    [SerializeField]
    private Camera aimCamera;

    [Header("Settings")]
    [SerializeField]
    private float aimDistance = 100f;

    [Header("Armas")]
    [SerializeField]
    private bool followWeapons = true;

    [SerializeField]
    private GunController[] guns;

    [Header("Proximidade (a mira cresce perto de objetos)")]
    [SerializeField]
    private bool scaleNearObjects = true;

    [SerializeField]
    private LayerMask proximityMask = ~0;

    [SerializeField, Min(1f)]
    private float proximityRange = 150f;

    [SerializeField, Min(0f)]
    private float proximityRadius = 2f;

    [SerializeField, Min(1f)]
    private float maxScale = 2.2f;

    [SerializeField, Min(0.1f)]
    private float scaleSharpness = 12f;

    [Header("Profundidade da mira")]
    [SerializeField]
    private bool snapToTargetDepth = true;

    [SerializeField, Min(1f)]
    private float depthRange = 500f;

    [SerializeField, Min(0.1f)]
    private float depthSharpness = 6f;

    [Header("Conforto")]
    [SerializeField, Min(0f)]
    private float screenSmoothing = 18f;

    [SerializeField, Min(0f)]
    private float maxLag = 120f;

    public float AimDistance => aimDistance;

    public static AimPositioner Instance { get; private set; }

    public RectTransform AimRect => aimRect;

    public bool HasAimRay { get; private set; }
    public Ray AimRay { get; private set; }

    public Transform AimOriginTransform => aimOrigin != null ? aimOrigin : shipTransform;

    private SpaceshipController ship;
    private RectTransform aimParentRect;
    private Camera aimUICamera;
    private Vector3 baseScale = Vector3.one;
    private float currentScale = 1f;
    private float currentDepth = -1f;
    private Vector2 smoothedLocal;
    private bool hasSmoothedLocal;
    private readonly RaycastHit[] hits = new RaycastHit[16];
    private WeaponManager weaponManager;
    private readonly System.Collections.Generic.List<Transform> muzzles = new System.Collections.Generic.List<Transform>();

    private void OnEnable() => Instance = this;

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Awake()
    {
        Instance = this;

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (aimCamera == null)
        {
            Debug.LogError("AimPositioner: não há câmara (Aim Camera vazio e sem Camera.main).", this);
        }

        Transform shipRoot = shipTransform != null ? shipTransform : aimOrigin;
        if (shipRoot != null)
            ship = shipRoot.GetComponentInParent<SpaceshipController>();

        if (followWeapons && ship != null)
        {
            if (guns == null || guns.Length == 0)
                guns = ship.GetComponentsInChildren<GunController>();

            weaponManager = ship.GetComponentInChildren<WeaponManager>();
        }

        if (shipTransform == null && aimOrigin == null)
        {
            Debug.LogError("AimPositioner: Ship Transform (ou Aim Origin) não foi atribuído.", this);
        }

        if (aimRect == null)
        {
            Debug.LogError("AimPositioner: Aim Rect não foi atribuído.", this);
            return;
        }

        aimParentRect = aimRect.parent as RectTransform;
        baseScale = aimRect.localScale;

        Canvas canvas = aimRect.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            aimUICamera = canvas.worldCamera;
        }
    }

    private void LateUpdate()
    {
        UpdateAimPosition();
    }

    private static Vector3 AimPoint(Transform muzzle, float distance) =>
        muzzle.position + muzzle.forward * distance;

    private void UpdateProximityScale(float nearest)
    {
        float target = 1f;

        if (scaleNearObjects && aimRect != null && nearest < proximityRange)
            target = Mathf.Lerp(maxScale, 1f, nearest / proximityRange);

        currentScale = Mathf.Lerp(currentScale, target, 1f - Mathf.Exp(-scaleSharpness * Time.deltaTime));

        if (aimRect != null)
            aimRect.localScale = baseScale * currentScale;
    }

    private float NearestHit(Vector3 start, Vector3 direction, float range)
    {
        int count = Physics.SphereCastNonAlloc(start, proximityRadius, direction, hits, range,
            proximityMask, QueryTriggerInteraction.Ignore);

        Transform self = ship != null ? ship.transform : null;
        float nearest = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider collider = hits[i].collider;

            if (self != null && collider.transform.IsChildOf(self))
                continue;

            nearest = Mathf.Min(nearest, hits[i].distance);
        }

        return nearest;
    }

    private void UpdateAimPosition()
    {
        Transform origin = aimOrigin != null ? aimOrigin : shipTransform;

        if (aimRect == null || aimParentRect == null || aimCamera == null || origin == null)
        {
            return;
        }

        muzzles.Clear();

        if (followWeapons)
        {
            if (guns != null)
            {
                foreach (GunController source in guns)
                {
                    Transform muzzle = source != null ? source.FiringPoint : null;

                    if (muzzle != null && !muzzles.Contains(muzzle))
                        muzzles.Add(muzzle);
                }
            }
        }

        if (followWeapons && muzzles.Count == 0 && weaponManager != null && weaponManager.FiringPoint != null)
            muzzles.Add(weaponManager.FiringPoint);

        float castRange = Mathf.Max(proximityRange, snapToTargetDepth ? depthRange : 0f);
        float nearest = float.MaxValue;

        if (muzzles.Count == 0)
        {
            Vector3 direction = ship != null && ship.UsesFreeReticle
                ? ship.AimDirection
                : origin.forward * Mathf.Sign(aimDistance);
            nearest = NearestHit(origin.position, direction, castRange);
        }
        else
        {
            foreach (Transform muzzle in muzzles)
                nearest = Mathf.Min(nearest, NearestHit(muzzle.position, muzzle.forward, castRange));
        }

        float targetDepth = snapToTargetDepth && nearest < castRange ? Mathf.Max(nearest, 5f) : Mathf.Abs(aimDistance);
        currentDepth = currentDepth < 0f
            ? targetDepth
            : Mathf.Lerp(currentDepth, targetDepth, 1f - Mathf.Exp(-depthSharpness * Time.deltaTime));
        float distance = currentDepth;

        Vector3 worldPoint;
        Vector3 rayOrigin;

        if (muzzles.Count > 0)
        {
            Vector3 sum = Vector3.zero;
            Vector3 originSum = Vector3.zero;

            foreach (Transform muzzle in muzzles)
            {
                sum += AimPoint(muzzle, distance);
                originSum += muzzle.position;
            }

            worldPoint = sum / muzzles.Count;
            rayOrigin = originSum / muzzles.Count;
        }
        else if (ship != null && ship.UsesFreeReticle)
        {
            worldPoint = origin.position + ship.AimDirection * distance;
            rayOrigin = origin.position;
        }
        else
        {
            worldPoint = origin.position + origin.forward * (distance * Mathf.Sign(aimDistance));
            rayOrigin = origin.position;
        }

        Vector3 rayDirection = worldPoint - rayOrigin;
        HasAimRay = rayDirection.sqrMagnitude > 1e-6f;

        if (HasAimRay)
            AimRay = new Ray(rayOrigin, rayDirection.normalized);

        UpdateProximityScale(nearest);
        Vector3 screenPoint = aimCamera.WorldToScreenPoint(worldPoint);

        if (screenPoint.z <= 0f)
        {
            return;
        }

        if (
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                aimParentRect,
                screenPoint,
                aimUICamera,
                out Vector2 localPoint
            )
        )
        {
            if (screenSmoothing > 0f && hasSmoothedLocal)
            {
                smoothedLocal = Vector2.Lerp(
                    smoothedLocal, localPoint, 1f - Mathf.Exp(-screenSmoothing * Time.deltaTime));

                Vector2 lag = smoothedLocal - localPoint;

                if (maxLag > 0f && lag.magnitude > maxLag)
                    smoothedLocal = localPoint + lag.normalized * maxLag;
            }
            else
            {
                smoothedLocal = localPoint;
                hasSmoothedLocal = true;
            }

            aimRect.localPosition = new Vector3(
                smoothedLocal.x,
                smoothedLocal.y,
                aimRect.localPosition.z
            );
        }
    }
}