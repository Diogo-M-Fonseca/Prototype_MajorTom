using System.Collections.Generic;
using UnityEngine;

public class ResourceCollector : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField, Min(0.1f)] private float _pickupRadius = 8f;
    [SerializeField, Min(1)] private int _maxCarried = 20;
    [SerializeField] private AudioClip _collectClip;

    [Header("Conection Point")]
    [SerializeField] private Transform _attachPoint;
    [SerializeField] private bool _invertForward = true;

    [Header("Cable")]
    [SerializeField, Min(0.1f)] private float _spacing = 4f;
    [SerializeField, Min(0.1f)] private float _stiffness = 20f;
    [SerializeField, Min(0f)] private float _damping = 5f;
    [SerializeField, Min(1f)] private float _maxStretch = 1.6f;

    private readonly List<ResourcePickup> _carried = new();
    private Vector3 _lastAttachPosition;

    public int Count => _carried.Count;
    public int MaxCarried => _maxCarried;
    public bool IsFull => _carried.Count >= _maxCarried;

    public event System.Action<int> CountChanged;

    private Transform AttachSource => _attachPoint != null ? _attachPoint : transform;

    private Vector3 BackDirection => AttachSource.forward * (_invertForward ? 1f : -1f);

    private void OnEnable()
    {
        _lastAttachPosition = AttachSource.position;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        dt = Mathf.Min(dt, 0.033f);

        CollectNearby();
        SimulateChain(dt);
    }

    public int ReleaseCarried(int amount)
    {
        int released = 0;

        while (released < amount && _carried.Count > 0)
        {
            int last = _carried.Count - 1;
            ResourcePickup pickup = _carried[last];
            _carried.RemoveAt(last);

            if (pickup != null)
                pickup.Consume();

            released++;
        }

        if (released > 0)
            CountChanged?.Invoke(_carried.Count);

        return released;
    }

    public int DetachCarried(int amount, List<ResourcePickup> output)
    {
        int detached = 0;

        while (detached < amount && _carried.Count > 0)
        {
            int last = _carried.Count - 1;
            ResourcePickup pickup = _carried[last];
            _carried.RemoveAt(last);

            if (pickup != null)
            {
                output.Add(pickup);
                detached++;
            }
        }

        if (detached > 0)
            CountChanged?.Invoke(_carried.Count);

        return detached;
    }

    private void CollectNearby()
    {
        if (IsFull)
            return;

        float radiusSqr = _pickupRadius * _pickupRadius;
        Vector3 position = transform.position;
        bool changed = false;

        for (int i = ResourcePickup.All.Count - 1; i >= 0; i--)
        {
            if (IsFull)
                break;

            ResourcePickup pickup = ResourcePickup.All[i];

            if (pickup == null || pickup.IsCarried)
                continue;

            if ((pickup.transform.position - position).sqrMagnitude > radiusSqr)
                continue;

            pickup.MarkCollected();
            _carried.Add(pickup);
            changed = true;

            if (_collectClip != null)
                AudioSource.PlayClipAtPoint(_collectClip, pickup.transform.position);
        }

        if (changed)
            CountChanged?.Invoke(_carried.Count);
    }

    private void SimulateChain(float dt)
    {
        Vector3 attachPosition = AttachSource.position;
        Vector3 attachVelocity = (attachPosition - _lastAttachPosition) / dt;
        _lastAttachPosition = attachPosition;

        Vector3 anchorPosition = attachPosition;
        Vector3 anchorVelocity = attachVelocity;
        Vector3 anchorBack = BackDirection;

        float maxLength = _spacing * _maxStretch;
        bool removed = false;

        for (int i = 0; i < _carried.Count; i++)
        {
            ResourcePickup pickup = _carried[i];

            if (pickup == null || !pickup.isActiveAndEnabled)
            {
                _carried.RemoveAt(i);
                i--;
                removed = true;
                continue;
            }

            Transform pickupTransform = pickup.transform;
            Vector3 position = pickupTransform.position;
            Vector3 velocity = pickup.Velocity;

            Vector3 desired = anchorPosition + anchorBack * _spacing;
            Vector3 acceleration = (desired - position) * _stiffness
                                   - (velocity - anchorVelocity) * _damping;

            velocity += acceleration * dt;
            position += velocity * dt;

            Vector3 offset = position - anchorPosition;

            if (offset.sqrMagnitude > maxLength * maxLength)
            {
                Vector3 direction = offset.normalized;
                position = anchorPosition + direction * maxLength;

                float outward = Vector3.Dot(velocity - anchorVelocity, direction);
                if (outward > 0f)
                    velocity -= direction * outward;
            }

            pickupTransform.position = position;
            pickup.Velocity = velocity;

            Vector3 chain = position - anchorPosition;
            if (chain.sqrMagnitude > 0.0001f)
                anchorBack = chain.normalized;

            anchorPosition = position;
            anchorVelocity = velocity;
        }

        if (removed)
            CountChanged?.Invoke(_carried.Count);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _pickupRadius);

        Transform source = AttachSource;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(source.position, source.position + BackDirection * _spacing);
    }
}