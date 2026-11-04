using UnityEngine;
using UnityEngine.Events;

public class SatelliteRing : MonoBehaviour
{
    [Header("Aro")]
    [SerializeField, Min(0.1f)] private float _innerRadius = 20f;
    [SerializeField] private bool _oneShot;
    [SerializeField, Min(0f)] private float _cooldown = 5f;

    [Header("Boost")]
    [SerializeField, Min(1f)] private float _speedMultiplier = 3f;
    [SerializeField, Min(0.1f)] private float _boostDuration = 4f;

    [Header("Escudo")]
    [SerializeField, Min(0f)] private float _shieldAmount = 50f;
    [SerializeField, Min(0.1f)] private float _shieldDuration = 10f;

    [Header("Feedback")]
    [SerializeField] private AudioClip _passClip;
    [SerializeField] private ParticleSystem _passEffect;
    [SerializeField] private UnityEvent _passed;

    private float _readyAt;

    public void OnZoneEntered(Collider other)
    {
        if (!enabled)
        {
            return;
        }

        if (Time.time <= _readyAt)
        {
            return;
        }

        Transform root = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;

        ShipBoost boost = root.GetComponentInParent<ShipBoost>();

        if (boost == null)
        {
            boost = root.GetComponentInChildren<ShipBoost>();
        }

        if (boost == null)
        {
            Debug.Log($"{name}: '{other.name}' entrou mas nao tem ShipBoost (root: '{root.name}').", this); // TEMP
            return;
        }

        PlayerShield shield = root.GetComponentInParent<PlayerShield>();

        if (shield == null)
        {
            shield = root.GetComponentInChildren<PlayerShield>();
        }

        Debug.Log($"{name}: boost ativado em '{boost.name}'.", this); // TEMP

        OnPlayerPassed(boost, shield, boost.transform.position);
    }

    private void OnPlayerPassed(ShipBoost boost, PlayerShield shield, Vector3 position)
    {
        boost.Activate(_speedMultiplier, _boostDuration);

        if (shield != null && _shieldAmount > 0f)
        {
            shield.Activate(_shieldAmount, _shieldDuration);
        }

        if (_passClip != null)
        {
            AudioSource.PlayClipAtPoint(_passClip, position);
        }

        if (_passEffect != null)
        {
            _passEffect.Play();
        }

        _passed?.Invoke();

        if (_oneShot)
        {
            enabled = false;
        }
        else
            _readyAt = Time.time + _cooldown;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 previous = Vector3.right * _innerRadius;
        const int segments = 48;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * _innerRadius;

            Gizmos.DrawLine(previous, next);
            previous = next;
        }

        Gizmos.DrawLine(Vector3.zero, Vector3.forward * _innerRadius * 0.5f);
    }
}
