using SpaceFlight;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(11000)]
public class TargetLockSystem : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField, Range(0.02f, 1f)] private float selectionRadius = 0.25f;

    [Header("Camera")]
    [SerializeField, Range(0f, 1f)] private float cameraFocusBlend = 0.5f;

    [Header("Lead")]
    [SerializeField] private float fallbackProjectileSpeed = 100f;
    // if there's not a AimPositioner
    [SerializeField] private float aimDistance = 100f;
    [SerializeField, Min(0f)] private float leadSmoothing = 0.05f;

    [Header("UI")]
    [SerializeField] private float leadBoxSize = 44f;
    [SerializeField] private float targetBoxSize = 70f;
    [SerializeField] private float aimedTolerance = 1f;
    [SerializeField] private Color idleColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color aimedColor = new Color(0.2f, 1f, 0.3f, 1f);
    [SerializeField] private Color unreachableColor = new Color(1f, 0.25f, 0.25f, 0.8f);

    private SpaceshipController ship;
    private CameraFollow cameraFollow;
    private Camera cam;
    private WeaponManager weaponManager;
    private GunController gunController;
    private AimPositioner aimPositioner;

    private RadarTarget target;
    private Rigidbody targetBody;
    private Vector3 lastTargetPos;
    private Vector3 targetVel;
    private Vector3 smoothedLead;
    private bool hasSmoothedLead;

    private bool leadValid;
    private Transform leadMuzzle;
    private Vector3 leadDirection;

    public bool TryGetLead(out Transform muzzle, out Vector3 direction)
    {
        muzzle = leadMuzzle;
        direction = leadDirection;
        return leadValid && muzzle != null;
    }

    private RectTransform root;
    private RectTransform targetBox;
    private RectTransform leadBox;
    private RectTransform line;
    private Image targetImg, leadImg, lineImg;
    private Text distanceLabel;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Spawn();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Spawn();

    private static void Spawn()
    {
        if (FindFirstObjectByType<TargetLockSystem>() != null) return;
        if (FindFirstObjectByType<SpaceshipController>() == null) return;
        if (FindFirstObjectByType<CameraFollow>() == null) return;

        new GameObject("TargetLockSystem").AddComponent<TargetLockSystem>();
    }


    private void Awake()
    {
        if (!TryGetComponent(out AimAssist _))
            gameObject.AddComponent<AimAssist>();

        BuildUI();
        SetUIVisible(false);
    }

    private void OnDisable()
    {
        if (cameraFollow != null) cameraFollow.FocusTarget = null;
    }

    private void ResolveReferences()
    {
        if (ship == null) ship = FindFirstObjectByType<SpaceshipController>();
        if (cameraFollow == null) cameraFollow = FindFirstObjectByType<CameraFollow>();
        if (cam == null) cam = cameraFollow != null ? cameraFollow.GetComponent<Camera>() : Camera.main;
        if (weaponManager == null) weaponManager = FindFirstObjectByType<WeaponManager>();
        if (weaponManager == null && gunController == null) gunController = FindFirstObjectByType<GunController>();
        if (aimPositioner == null) aimPositioner = FindFirstObjectByType<AimPositioner>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        ResolveReferences();
        if (ship == null || cam == null) { SetUIVisible(false); return; }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.middleButton.wasPressedThisFrame)
            HandleSelectionClick(mouse);

        if (target != null && !target.isActiveAndEnabled)
            ClearTarget();
    }

    private void LateUpdate()
    {
        leadValid = false;

        if (target == null || ship == null || cam == null)
        {
            SetUIVisible(false);
            return;
        }

        UpdateTargetVelocity();
        UpdateUI();
    }


    private void HandleSelectionClick(Mouse mouse)
    {
        Vector2 refPoint;
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            GetMuzzle(out Transform muzzle, out _, out _, out _, out _);
            GetCrosshair(muzzle, out Vector3 aimOrigin, out Vector3 aimOffset);
            Vector3 sp = cam.WorldToScreenPoint(aimOrigin + aimOffset);
            refPoint = sp.z > 0f ? (Vector2)sp : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }
        else
        {
            refPoint = mouse.position.ReadValue();
        }

        float maxDist = selectionRadius * Screen.height;
        RadarTarget best = null;
        float bestDist = maxDist;

        for (int i = 0; i < RadarTarget.All.Count; i++)
        {
            RadarTarget candidate = RadarTarget.All[i];
            if (candidate == null) continue;

            Vector3 sp = cam.WorldToScreenPoint(candidate.transform.position);
            if (sp.z <= 0f) continue;

            float dist = Vector2.Distance(refPoint, sp);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }

        if (best == null || best == target)
            ClearTarget();
        else
            SetTarget(best);
    }

    private void SetTarget(RadarTarget newTarget)
    {
        target = newTarget;
        targetBody = newTarget.GetComponentInParent<Rigidbody>();
        lastTargetPos = newTarget.transform.position;
        targetVel = targetBody != null ? targetBody.linearVelocity : Vector3.zero;
        hasSmoothedLead = false;

        if (cameraFollow != null)
        {
            cameraFollow.FocusBlend = cameraFocusBlend;
            cameraFollow.FocusTarget = newTarget.transform;
        }
    }

    private void ClearTarget()
    {
        target = null;
        targetBody = null;
        hasSmoothedLead = false;
        if (cameraFollow != null) cameraFollow.FocusTarget = null;
        SetUIVisible(false);
    }


    private void UpdateTargetVelocity()
    {
        if (targetBody != null)
        {
            targetVel = targetBody.linearVelocity;
        }
        else if (Time.deltaTime > 0f)
        {
            targetVel = (target.transform.position - lastTargetPos) / Time.deltaTime;
        }

        lastTargetPos = target.transform.position;
    }

    private void GetMuzzle(
        out Transform muzzle,
        out float speed,
        out float range,
        out bool inheritShipSpeed,
        out bool isProjectile
    )
    {
        muzzle = ship.transform;
        speed = fallbackProjectileSpeed;
        range = float.MaxValue;
        inheritShipSpeed = false;
        isProjectile = true;

        if (weaponManager != null)
        {
            if (weaponManager.FiringPoint != null) muzzle = weaponManager.FiringPoint;

            Weapon weapon = weaponManager.CurrentWeapon;
            if (weapon != null && weapon.Data != null)
            {
                if (weapon.Data.WeaponBehaviour is ProjectileWeaponBehaviour projectile)
                {
                    speed = projectile.ProjectileSpeed;
                    range = projectile.Range;
                }
                else
                {
                    isProjectile = false; // laser: acerta instantaneamente, não há lead
                }
            }
        }
        else if (gunController != null)
        {
            if (gunController.FiringPoint != null) muzzle = gunController.FiringPoint;
            speed = gunController.ProjectileSpeed;
            range = gunController.Range;
            inheritShipSpeed = true;
        }
    }

    private void GetCrosshair(Transform muzzle, out Vector3 origin, out Vector3 offset)
    {
        Transform aimOrigin = aimPositioner != null ? aimPositioner.AimOriginTransform : null;
        if (aimOrigin != null)
        {
            origin = aimOrigin.position;
            offset = aimOrigin.forward * aimPositioner.AimDistance;
        }
        else
        {
            origin = muzzle.position;
            offset = muzzle.forward * aimDistance;
        }
    }


    private void UpdateUI()
    {
        GetMuzzle(out Transform muzzle, out float speed, out float range, out bool inherit, out bool isProjectile);

        Vector3 targetPos = target.transform.position;
        Vector3 shipVel = ship.TryGetComponent(out Rigidbody shipBody) ? shipBody.linearVelocity : Vector3.zero;
        float distance = Vector3.Distance(muzzle.position, targetPos);

        // Marcador no inimigo
        SetUIVisible(true);
        PlaceOnScreen(targetBox, cam.WorldToScreenPoint(targetPos), out bool targetOnScreen);
        targetBox.sizeDelta = Vector2.one * targetBoxSize;
        distanceLabel.text = Mathf.RoundToInt(distance) + " m";

        if (!isProjectile)
        {
            leadBox.gameObject.SetActive(false);
            line.gameObject.SetActive(false);
            targetImg.color = idleColor;
            return;
        }

        bool solved = LeadSolver.Solve(
            muzzle.position, targetPos, targetVel, speed, shipVel, inherit,
            out Vector3 intercept, out float time
        );

        if (hasSmoothedLead && leadSmoothing > 0f)
        {
            float a = 1f - Mathf.Exp(-Time.deltaTime / leadSmoothing);
            smoothedLead = Vector3.Lerp(smoothedLead, intercept, a);
        }
        else
        {
            smoothedLead = intercept;
            hasSmoothedLead = true;
        }

        bool outOfRange = solved && speed * time > range;

        Vector3 leadDir = (smoothedLead - muzzle.position).normalized;
        GetCrosshair(muzzle, out Vector3 aimOrigin, out Vector3 aimOffset);
        Quaternion toLead = Quaternion.FromToRotation(muzzle.forward, leadDir);
        Vector3 leadScreen = cam.WorldToScreenPoint(aimOrigin + toLead * aimOffset);
        Vector3 aimScreen = cam.WorldToScreenPoint(aimOrigin + aimOffset);

        PlaceOnScreen(leadBox, leadScreen, out bool leadOnScreen);
        leadBox.sizeDelta = Vector2.one * leadBoxSize;
        leadBox.gameObject.SetActive(true);

        leadMuzzle = muzzle;
        leadDirection = leadDir;
        leadValid = solved && !outOfRange;

        bool aimed =
            solved && !outOfRange && aimScreen.z > 0f && leadScreen.z > 0f &&
            Vector2.Distance(aimScreen, leadScreen) <= leadBoxSize * 0.5f * aimedTolerance;

        Color c = !solved || outOfRange ? unreachableColor : (aimed ? aimedColor : idleColor);
        leadImg.color = c;
        targetImg.color = c;
        lineImg.color = new Color(c.r, c.g, c.b, 0.45f);
        distanceLabel.color = c;

        bool showLine = targetOnScreen && leadOnScreen;
        line.gameObject.SetActive(showLine);
        if (showLine)
        {
            Vector2 a2 = targetBox.position;
            Vector2 b2 = leadBox.position;
            Vector2 diff = b2 - a2;
            line.position = (a2 + b2) * 0.5f;
            line.sizeDelta = new Vector2(diff.magnitude, 2f);
            line.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
        }
    }

    private void PlaceOnScreen(RectTransform rect, Vector3 screen, out bool onScreen)
    {
        const float margin = 40f;
        onScreen = screen.z > 0f
            && screen.x >= 0f && screen.x <= Screen.width
            && screen.y >= 0f && screen.y <= Screen.height;

        if (screen.z <= 0f)
        {
            screen.x = Screen.width - screen.x;
            screen.y = Screen.height - screen.y;
        }

        screen.x = Mathf.Clamp(screen.x, margin, Screen.width - margin);
        screen.y = Mathf.Clamp(screen.y, margin, Screen.height - margin);
        rect.position = new Vector3(screen.x, screen.y, 0f);
    }

    private void SetUIVisible(bool visible)
    {
        if (root != null) root.gameObject.SetActive(visible);
    }

    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("TargetLockCanvas", typeof(Canvas));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        root = (RectTransform)canvasGo.transform;

        Sprite square = MakeSprite(64, (x, y, s) => Mathf.Min(Mathf.Min(x, y), Mathf.Min(s - 1 - x, s - 1 - y)) < 3);
        Sprite brackets = MakeSprite(64, (x, y, s) =>
        {
            int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(s - 1 - x, s - 1 - y));
            if (edge >= 3) return false;
            int cx = Mathf.Min(x, s - 1 - x);
            int cy = Mathf.Min(y, s - 1 - y);
            return cx < 16 || cy < 16;
        });
        Sprite flat = MakeSprite(4, (x, y, s) => true);

        line = CreateImage("Line", flat, out lineImg);
        targetBox = CreateImage("TargetBox", brackets, out targetImg);
        leadBox = CreateImage("LeadBox", square, out leadImg);

        GameObject textGo = new GameObject("Distance", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(targetBox, false);
        RectTransform textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = new Vector2(0.5f, 0f);
        textRect.anchorMax = new Vector2(0.5f, 0f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -4f);
        textRect.sizeDelta = new Vector2(120f, 24f);

        distanceLabel = textGo.GetComponent<Text>();
        distanceLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        distanceLabel.fontSize = 16;
        distanceLabel.alignment = TextAnchor.UpperCenter;
        distanceLabel.raycastTarget = false;
    }

    private RectTransform CreateImage(string name, Sprite sprite, out Image image)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root, false);
        image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return (RectTransform)go.transform;
    }

    private delegate bool PixelFill(int x, int y, int size);

    private static Sprite MakeSprite(int size, PixelFill fill)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            pixels[y * size + x] = fill(x, y, size) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);

        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
