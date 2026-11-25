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
    private readonly Action<float, float> setMovementLimit;
    private readonly Func<Vector3> getShooterVelocity;
    private readonly Func<float> getSpeedMultiplier;

    public Vector3 ShooterVelocity => getShooterVelocity != null ? getShooterVelocity() : Vector3.zero;
    public float SpeedMultiplier => getSpeedMultiplier != null ? getSpeedMultiplier() : 1f;

    public WeaponContext(Transform firingPoint, MonoBehaviour coroutineRunner, ProjectilePool projectilePool, AudioSource audioSource, LaserBeam laserBeam, Action<bool> setMovementLocked,
        Func<Vector3> getShooterVelocity = null, Func<float> getSpeedMultiplier = null,
        Action<float, float> setMovementLimit = null)
    {
        this.setMovementLimit = setMovementLimit;
        this.getShooterVelocity = getShooterVelocity;
        this.getSpeedMultiplier = getSpeedMultiplier;
        FiringPoint = firingPoint;
        CoroutineRunner = coroutineRunner;
        ProjectilePool = projectilePool;
        AudioSource = audioSource;
        LaserBeam = laserBeam;
        this.setMovementLocked = setMovementLocked;
    }

    public void SetMovementLimit(float speedFraction, float turnScale)
    {
        setMovementLimit?.Invoke(speedFraction, turnScale);
    }

    public void SetMovementLocked(bool locked)
    {
        setMovementLocked?.Invoke(locked);
    }
}