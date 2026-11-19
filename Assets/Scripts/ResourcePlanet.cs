using System.Collections.Generic;
using UnityEngine;

public class ResourcePlanet : MonoBehaviour
{
    public static readonly List<ResourcePlanet> All = new();

    [Header("Delivery")]
    [SerializeField, Min(1)] private int _capacity = 10;
    [SerializeField, Min(0.1f)] private float _deliveryRadius = 60f;

    [Header("State")]
    [SerializeField, Min(0)] private int _delivered;

    public int Capacity => _capacity;
    public int Delivered => _delivered;
    public int Incoming => _incoming;
    public int Remaining => Mathf.Max(0, _capacity - _delivered - _incoming);
    public bool IsFull => _delivered + _incoming >= _capacity;
    public float SurfaceRadius { get; private set; }
    public float DeliveryRadius => _deliveryRadius;
    public float Progress => _capacity > 0 ? (float)_delivered / _capacity : 1f;

    public event System.Action<int, int> Changed;

    private int _incoming;

    private void Awake()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        SurfaceRadius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
    }

    private void OnEnable() => All.Add(this);

    private void OnDisable() => All.Remove(this);

    public int Accept(int amount)
    {
        int accepted = Mathf.Clamp(amount, 0, Remaining);

        if (accepted <= 0)
            return 0;

        _delivered += accepted;
        Changed?.Invoke(_delivered, _capacity);
        return accepted;
    }

    public int Reserve(int amount)
    {
        int reserved = Mathf.Clamp(amount, 0, Remaining);
        _incoming += reserved;
        return reserved;
    }

    public void Receive()
    {
        if (_incoming > 0)
            _incoming--;

        _delivered = Mathf.Min(_capacity, _delivered + 1);
        Changed?.Invoke(_delivered, _capacity);
    }

    public void CancelReservation()
    {
        if (_incoming > 0)
            _incoming--;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsFull ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _deliveryRadius);
    }
}
