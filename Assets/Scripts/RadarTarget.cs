using System.Collections.Generic;
using UnityEngine;

public class RadarTarget : MonoBehaviour, IAimLockable
{
    public static readonly List<RadarTarget> All = new();

    public Transform LockPoint => transform;

    public bool IsLockable => isActiveAndEnabled;

    public bool ShowLead => true;

    private void OnEnable()
    {
        All.Add(this);
        AimLockRegistry.Register(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
        AimLockRegistry.Unregister(this);
    }

}