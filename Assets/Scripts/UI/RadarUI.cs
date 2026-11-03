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

    [Header("Orientation")]
    [SerializeField] private bool _yawOnly = false;

    [Header("Update Rate")]
    [SerializeField, Min(0f)] private float _updateRate = 20f;

    [Header("Out of Range")]
    [SerializeField] private bool _showOutOfRange = true;
    [SerializeField, Range(0f, 1f)] private float _outOfRangeAlpha = 0.35f;

    [Header("Height Icons")]
    [SerializeField] private Sprite _aboveSprite;
    [SerializeField] private Sprite _levelSprite;
    [SerializeField] private Sprite _belowSprite;
    [SerializeField] private Color _aboveColor = Color.cyan;
    [SerializeField] private Color _levelColor = Color.white;
    [SerializeField] private Color _belowColor = Color.red;

    private readonly List<Image> _blips = new();
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

            Image blip = GetBlip(used++);
            blip.rectTransform.anchoredPosition = position;

            Sprite sprite;
            Color color;

            if (local.y > _levelTolerance)
            {
                sprite = _aboveSprite;
                color = _aboveColor;
            }
            else if (local.y < -_levelTolerance)
            {
                sprite = _belowSprite;
                color = _belowColor;
            }
            else
            {
                sprite = _levelSprite;
                color = _levelColor;
            }

            if (outOfRange)
                color.a *= _outOfRangeAlpha;

            blip.sprite = sprite;
            blip.color = color;
        }

        for (int i = used; i < _blips.Count; i++)
            _blips[i].gameObject.SetActive(false);
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

    private Image GetBlip(int index)
    {
        if (index == _blips.Count)
            _blips.Add(Instantiate(_blipPrefab, _container));

        _blips[index].gameObject.SetActive(true);
        return _blips[index];
    }
}
