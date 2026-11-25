using System.Collections.Generic;
using UnityEngine;

public interface IAimLockable
{
    Transform LockPoint { get; }

    bool IsLockable { get; }

    bool ShowLead { get; }
}

public static class AimLockRegistry
{
    public static readonly List<IAimLockable> All = new();

    public static void Register(IAimLockable lockable)
    {
        if (lockable != null && !All.Contains(lockable))
            All.Add(lockable);
    }

    public static void Unregister(IAimLockable lockable) => All.Remove(lockable);
}
