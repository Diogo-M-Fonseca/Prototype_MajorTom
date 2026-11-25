using UnityEngine;
using UnityEngine.UI;

public class ResourcePlanetPrompt : MonoBehaviour
{
    [SerializeField] private string _format = "[{0}] Entregar recursos  ({1}/{2})";

    [SerializeField] private Vector2 _screenOffset = new Vector2(0f, 40f);

    [SerializeField] private Color _color = new Color(0.4f, 1f, 0.6f, 1f);

    [Header("Info com o planeta focado (lock)")]
    [SerializeField] private bool _showInfoWhenLocked = true;

    [SerializeField] private Vector2 _infoOffset = new Vector2(0f, -90f);

    [SerializeField] private Color _infoColor = new Color(0.7f, 0.9f, 1f, 1f);

    private static Text s_label;
    private static ResourcePlanetPrompt s_owner;

    private static Text s_info;
    private static ResourcePlanetPrompt s_infoOwner;
    private static TargetLockSystem s_lockSystem;

    private ResourcePlanet _planet;
    private ResourceDelivery _delivery;
    private ResourceCollector _collector;
    private Camera _camera;

    private void Awake() => _planet = GetComponent<ResourcePlanet>();

    private void OnDisable()
    {
        Release();
        ReleaseInfo();
    }

    private void OnDestroy()
    {
        Release();
        ReleaseInfo();
    }

    private void LateUpdate()
    {
        UpdateInfo();

        if (_delivery == null)
        {
            _delivery = FindFirstObjectByType<ResourceDelivery>();
            _collector = _delivery != null ? _delivery.GetComponent<ResourceCollector>() : null;
        }

        if (_camera == null)
            _camera = Camera.main;

        bool show = _delivery != null && _collector != null && _camera != null
            && _delivery.NearbyPlanet == _planet
            && _planet.Remaining > 0
            && _collector.Count > 0;

        if (!show)
        {
            Release();
            return;
        }

        Vector3 center = _planet.transform.position;
        Vector3 toShip = _delivery.transform.position - center;
        Vector3 anchor = toShip.sqrMagnitude > 0.0001f
            ? center + toShip.normalized * _planet.SurfaceRadius
            : center;

        Vector3 screen = _camera.WorldToScreenPoint(anchor);

        if (screen.z <= 0f)
        {
            Release();
            return;
        }

        EnsureLabel();
        s_owner = this;
        s_label.gameObject.SetActive(true);
        s_label.color = _color;
        s_label.text = string.Format(_format, _delivery.KeyLabel, _collector.Count, _planet.Remaining);
        s_label.rectTransform.position = new Vector3(screen.x + _screenOffset.x, screen.y + _screenOffset.y, 0f);
    }

    private void UpdateInfo()
    {
        if (s_lockSystem == null)
            s_lockSystem = FindFirstObjectByType<TargetLockSystem>();

        IAimLockable locked = _showInfoWhenLocked && s_lockSystem != null ? s_lockSystem.CurrentTarget : null;

        bool focused = locked != null
            && locked.LockPoint != null
            && locked.LockPoint.GetComponentInParent<ResourcePlanet>() == _planet;

        if (_camera == null)
            _camera = Camera.main;

        Vector3 screen = focused && _camera != null
            ? _camera.WorldToScreenPoint(_planet.transform.position)
            : Vector3.zero;

        if (!focused || screen.z <= 0f)
        {
            if (s_infoOwner == this)
                ReleaseInfo();

            return;
        }

        s_infoOwner = this;
        EnsureInfoLabel();

        s_info.gameObject.SetActive(true);
        s_info.color = _infoColor;
        s_info.text = _planet.IsFull
            ? "Planeta completo"
            : $"Faltam {_planet.Remaining} recursos\n({_planet.Delivered}/{_planet.Capacity} entregues)";
        s_info.rectTransform.position = new Vector3(screen.x + _infoOffset.x, screen.y + _infoOffset.y, 0f);
    }

    private void ReleaseInfo()
    {
        if (s_infoOwner != this)
            return;

        s_infoOwner = null;

        if (s_info != null)
            s_info.gameObject.SetActive(false);
    }

    private static void EnsureInfoLabel()
    {
        if (s_info != null)
            return;

        GameObject canvasGo = new GameObject("PlanetInfoCanvas", typeof(Canvas));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        GameObject textGo = new GameObject("PlanetInfo", typeof(RectTransform), typeof(Text), typeof(Outline));
        textGo.transform.SetParent(canvasGo.transform, false);

        s_info = textGo.GetComponent<Text>();
        s_info.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        s_info.fontSize = 18;
        s_info.fontStyle = FontStyle.Bold;
        s_info.alignment = TextAnchor.UpperCenter;
        s_info.horizontalOverflow = HorizontalWrapMode.Overflow;
        s_info.raycastTarget = false;
        s_info.rectTransform.sizeDelta = new Vector2(320f, 50f);
    }

    private void Release()
    {
        if (s_owner != this)
            return;

        s_owner = null;

        if (s_label != null)
            s_label.gameObject.SetActive(false);
    }

    private static void EnsureLabel()
    {
        if (s_label != null)
            return;

        GameObject canvasGo = new GameObject("PlanetPromptCanvas", typeof(Canvas));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        GameObject textGo = new GameObject("Prompt", typeof(RectTransform), typeof(Text), typeof(Outline));
        textGo.transform.SetParent(canvasGo.transform, false);

        s_label = textGo.GetComponent<Text>();
        s_label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        s_label.fontSize = 22;
        s_label.fontStyle = FontStyle.Bold;
        s_label.alignment = TextAnchor.MiddleCenter;
        s_label.horizontalOverflow = HorizontalWrapMode.Overflow;
        s_label.raycastTarget = false;
        s_label.rectTransform.sizeDelta = new Vector2(420f, 36f);
    }
}