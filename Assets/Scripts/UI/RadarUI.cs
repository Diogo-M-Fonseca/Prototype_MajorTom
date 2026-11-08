using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _ship;
    [SerializeField] private bool _invertForward = true;
    [SerializeField] private RectTransform _container;
    [SerializeField] private Image _blipPrefab;

    [Header("Projection")]
    [SerializeField, Range(90f, 180f)] private float _maxAngle = 180f;

    [Header("Range")]
    [SerializeField, Min(1f)] private float _range = 300f;
    [SerializeField] private bool _showOutOfRange = true;
    [SerializeField, Range(0f, 1f)] private float _outOfRangeAlpha = 0.35f;

    [Header("Blip Size (by distance)")]
    [SerializeField, Min(0f)] private float _nearDistance = 20f;
    [SerializeField, Min(1f)] private float _nearSize = 20f;
    [SerializeField, Min(1f)] private float _farSize = 5f;
    [SerializeField] private AnimationCurve _sizeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Blip Color")]
    [SerializeField] private Color _color = Color.white;

    [Header("Update Rate")]
    [SerializeField, Min(0f)] private float _updateRate = 30f;

    private readonly List<RadarBlip> _blips = new();
    private float _nextUpdate;

    private void LateUpdate()
    {
        if (_ship == null || _container == null || _blipPrefab == null)
            return;

        if (_updateRate > 0f)
        {
            if (Time.unscaledTime < _nextUpdate)
                return;

            _nextUpdate = Time.unscaledTime + 1f / _updateRate;
        }

        Refresh();
    }

    private void Refresh()
    {
        Quaternion inverse = Quaternion.Inverse(GetRadarFrame());
        float radius = _container.rect.width * 0.5f;
        int used = 0;

        for (int i = 0; i < RadarTarget.All.Count; i++)
        {
            RadarTarget target = RadarTarget.All[i];
            if (target == null)
                continue;

            Vector3 local = inverse * (target.transform.position - _ship.position);
            float distance = local.magnitude;

            bool outOfRange = distance > _range;
            if (outOfRange && !_showOutOfRange)
                continue;

            Color color = _color;
            if (outOfRange)
                color.a *= _outOfRangeAlpha;

            RadarBlip blip = GetBlip(used++);
            blip.Root.anchoredPosition = Project(local, distance, radius);

            float size = SizeForDistance(distance);
            blip.Dot.rectTransform.sizeDelta = new Vector2(size, size);
            blip.Dot.color = color;
        }

        for (int i = used; i < _blips.Count; i++)
            _blips[i].Root.gameObject.SetActive(false);
    }

    private float SizeForDistance(float distance)
    {
        float span = Mathf.Max(_range - _nearDistance, 0.01f);
        float t = Mathf.Clamp01((distance - _nearDistance) / span);
        float curved = Mathf.Clamp01(_sizeCurve.Evaluate(t));

        return Mathf.Lerp(_nearSize, _farSize, curved);
    }

    private Vector2 Project(Vector3 local, float distance, float radius)
    {
        if (distance < 0.0001f)
            return Vector2.zero;

        Vector3 dir = local / distance;

        float angle = Mathf.Acos(Mathf.Clamp(dir.z, -1f, 1f)) * Mathf.Rad2Deg;

        Vector2 side = new Vector2(dir.x, dir.y);
        if (side.sqrMagnitude < 0.000001f)
            side = Vector2.up;

        float t = Mathf.Clamp01(angle / _maxAngle);
        return side.normalized * (t * radius);
    }

    private Quaternion GetRadarFrame()
    {
        Vector3 forward = _invertForward ? -_ship.forward : _ship.forward;
        return Quaternion.LookRotation(forward, _ship.up);
    }

    private RadarBlip GetBlip(int index)
    {
        if (index == _blips.Count)
            _blips.Add(CreateBlip());

        RadarBlip blip = _blips[index];
        blip.Root.gameObject.SetActive(true);
        return blip;
    }

    private RadarBlip CreateBlip()
    {
        GameObject rootObject = new GameObject("Blip", typeof(RectTransform));
        rootObject.layer = _container.gameObject.layer;

        RectTransform root = (RectTransform)rootObject.transform;
        root.SetParent(_container, false);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = Vector2.zero;

        Image dot = Instantiate(_blipPrefab, root);
        dot.name = "Dot";
        dot.raycastTarget = false;

        RectTransform dotRect = dot.rectTransform;
        dotRect.anchorMin = dotRect.anchorMax = dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = Vector2.zero;

        return new RadarBlip { Root = root, Dot = dot, Arrow = null };
    }
}