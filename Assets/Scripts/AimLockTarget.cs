using UnityEngine;

public class AimLockTarget : MonoBehaviour, IAimLockable
{
    private Transform _lockPoint;

    private bool _showLead;

    public Transform LockPoint => _lockPoint != null ? _lockPoint : transform;

    public bool IsLockable => isActiveAndEnabled;

    public bool ShowLead => _showLead;

    private void OnEnable() => AimLockRegistry.Register(this);

    private void OnDisable() => AimLockRegistry.Unregister(this);
}