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

    [Header("Range")]
    [SerializeField, Min(1f)] private float _range = 150f;
    [SerializeField, Min(0f)] private float _levelTolerance = 8f;

    [Header("Arrow")]
    [SerializeField, Min(1f)] private float _arrowSize = 10f;
    [SerializeField, Min(0f)] private float _arrowGap = 1f;

    [Header("Orientation")]
    [SerializeField] private bool _yawOnly = false;

    [Header("Update Rate")]
    [SerializeField, Min(0f)] private float _updateRate = 20f;

    [Header("Out of Range")]
    [SerializeField] private bool _showOutOfRange = true;
    [SerializeField, Range(0f, 1f)] private float _outOfRangeAlpha = 0.35f;

    [Header("Sprites / Colors")]
    [SerializeField] private Sprite _aboveSprite;
    [SerializeField] private Sprite _levelSprite;
    [SerializeField] private Sprite _belowSprite;
    [SerializeField] private Color _aboveColor = Color.cyan;
    [SerializeField] private Color _levelColor = Color.white;
    [SerializeField] private Color _belowColor = Color.red;

    private readonly List<RadarBlip> _blips = new();
    private float _nextUpdate;
    private Vector3 _lastFlatForward = Vector3.forward;

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
        Quaternion frame = GetRadarFrame();
        float radius = _container.rect.width * 0.5f;
        float rangeSqr = _range * _range;
        int used = 0;

        for (int i = 0; i < RadarTarget.All.Count; i++)
        {
            RadarTarget target = RadarTarget.All[i];
            if (target == null)
                continue;

            Vector3 local = Quaternion.Inverse(frame) * (target.transform.position - _ship.position);
            bool outOfRange = local.sqrMagnitude > rangeSqr;

            if (outOfRange && !_showOutOfRange)
                continue;

            Vector2 flat = new Vector2(local.x, local.z);
            Vector2 position;

            if (outOfRange)
            {
                Vector2 dir = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector2.up;
                position = dir * radius;
            }
            else
            {
                position = flat / _range * radius;
            }

            float direction;
            Sprite arrowSprite;
            Color color;

            if (local.y > _levelTolerance)
            {
                direction = 1f;
                arrowSprite = _aboveSprite;
                color = _aboveColor;
            }
            else if (local.y < -_levelTolerance)
            {
                direction = -1f;
                arrowSprite = _belowSprite;
                color = _belowColor;
            }
            else
            {
                direction = 0f;
                arrowSprite = null;
                color = _levelColor;
            }

            if (outOfRange)
                color.a *= _outOfRangeAlpha;

            RadarBlip blip = GetBlip(used++);
            blip.Root.anchoredPosition = position;

            blip.Dot.sprite = _levelSprite;
            blip.Dot.color = color;

            ApplyArrow(blip, direction, arrowSprite, color);
        }

        for (int i = used; i < _blips.Count; i++)
            _blips[i].Root.gameObject.SetActive(false);
    }

    private void ApplyArrow(RadarBlip blip, float direction, Sprite sprite, Color color)
    {
        bool visible = direction != 0f;
        blip.Arrow.gameObject.SetActive(visible);

        if (!visible)
            return;

        float offset = blip.Dot.rectTransform.sizeDelta.y * 0.5f + _arrowGap + _arrowSize * 0.5f;

        RectTransform arrow = blip.Arrow.rectTransform;
        arrow.sizeDelta = new Vector2(_arrowSize, _arrowSize);
        arrow.anchoredPosition = new Vector2(0f, direction * offset);

        blip.Arrow.sprite = sprite;
        blip.Arrow.color = color;
    }

    private Quaternion GetRadarFrame()
    {
        Vector3 forward = _invertForward ? -_ship.forward : _ship.forward;

        if (!_yawOnly)
            return Quaternion.LookRotation(forward, _ship.up);

        Vector3 flat = Vector3.ProjectOnPlane(forward, Vector3.up);

        if (flat.sqrMagnitude < 0.0001f)
            flat = _lastFlatForward;
        else
            _lastFlatForward = flat.normalized;

        return Quaternion.LookRotation(flat, Vector3.up);
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

        GameObject arrowObject = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrowObject.layer = root.gameObject.layer;

        RectTransform arrowRect = (RectTransform)arrowObject.transform;
        arrowRect.SetParent(root, false);
        arrowRect.anchorMin = arrowRect.anchorMax = arrowRect.pivot = new Vector2(0.5f, 0.5f);

        Image arrow = arrowObject.GetComponent<Image>();
        arrow.raycastTarget = false;

        return new RadarBlip { Root = root, Dot = dot, Arrow = arrow };
    }
}