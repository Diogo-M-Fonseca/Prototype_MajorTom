using System;
using UnityEngine;

public class WeaponContext
{
    public Transform FiringPoint { get; }
    public MonoBehaviour CoroutineRunner { get; }
    public ProjectilePool ProjectilePool { get; }
    public AudioSource AudioSource { get; }
    public LaserBeam LaserBeam { get; }

    private readonly Action<bool> setMovementLocked;

    public WeaponContext(Transform firingPoint, MonoBehaviour coroutineRunner,ProjectilePool projectilePool, AudioSource audioSource,LaserBeam laserBeam, Action<bool> setMovementLocked)
    {
        FiringPoint = firingPoint;
        CoroutineRunner = coroutineRunner;
        ProjectilePool = projectilePool;
        AudioSource = audioSource;
        LaserBeam = laserBeam;
        this.setMovementLocked = setMovementLocked;
    }

    public void SetMovementLocked(bool locked)
    {
        setMovementLocked?.Invoke(locked);
    }
}
