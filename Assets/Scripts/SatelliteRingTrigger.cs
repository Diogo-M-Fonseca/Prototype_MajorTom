using UnityEngine;

public class SatelliteRingTrigger : MonoBehaviour
{
    [SerializeField] private SatelliteRing _ring;

    private void Awake()
    {
        if (_ring == null)
            _ring = GetComponentInParent<SatelliteRing>();

        if (_ring == null)
        {
            Debug.LogWarning($"{name}: SatelliteRingTrigger nao encontrou o SatelliteRing.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger: {other.name}", this);
        if (_ring != null)
        {
            _ring.OnZoneEntered(other);
        }
    }
}