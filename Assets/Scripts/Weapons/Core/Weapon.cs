using System;
using UnityEngine;

public class Weapon
{
    public WeaponData Data { get; }
    public WeaponContext Context { get; }

    public bool IsFiring { get; private set; }
    public bool IsOverheated { get; private set; }
    public float CurrentHeat { get; private set; }
    public float FireCooldown { get; private set; }
    public float ChargeProgress { get; private set; }
    public bool IsCharging { get; private set; }
    public bool IsLaserActive { get; private set; }

    public bool CanFire => FireCooldown <= 0f && !IsOverheated;

    public event Action<float> OnHeatChanged;
    public event Action<bool> OnOverheatChanged;

    public Weapon(WeaponData data, WeaponContext context)
    {
        Data = data;
        Context = context;
        CurrentHeat = 0f;
        FireCooldown = 0f;
    }

    public void StartFiring()
    {
        if (IsFiring || IsOverheated) return;

        IsFiring = true;
        Data.WeaponBehaviour.StartFiring(this);
    }

    public void StopFiring()
    {
        if (!IsFiring) return;

        IsFiring = false;
        Data.WeaponBehaviour.StopFiring(this);
    }

    public void StartFireCooldown(float duration)
    {
        FireCooldown = duration;
    }

    public void Tick(float deltaTime)
    {
        FireCooldown = Mathf.Max(0f, FireCooldown - deltaTime);
        
        UpdateHeat(deltaTime);
        
        Data.WeaponBehaviour.Tick(this, deltaTime);
    }

    private void UpdateHeat(float deltaTime)
    {
        if (IsFiring)
        {
            CurrentHeat += Data.HeatPerSecond * deltaTime;

            if (CurrentHeat >= 1f)
            {
                CurrentHeat = 1f;
                SetOverheated(true);
                StopFiring();
            }
        }

        else if (CurrentHeat > 0f)
        {
            CurrentHeat -= Data.CoolingPerSecond * deltaTime;
            CurrentHeat = Mathf.Max(0f, CurrentHeat);
        }

        if (IsOverheated && CurrentHeat <= Data.ResumeHeatThreshold)
        {
            SetOverheated(false);
        }

        OnHeatChanged?.Invoke(CurrentHeat);
        
    }

    private void SetOverheated(bool overheated)
    {
        if (IsOverheated == overheated) return;

        IsOverheated = overheated;
        OnOverheatChanged?.Invoke(IsOverheated);
    }

    public void Equip()
    {
        Data.WeaponBehaviour.OnEquipped(this);
    }

    public void Unequip()
    {
        StopFiring();
        Data.WeaponBehaviour.OnUnequipped(this);
    }

    public void SetCharging(bool charging)
    {
        IsCharging = charging;
    }

    public void SetLaserActive(bool active)
    {
        IsLaserActive = active;
    }

    public void SetChargeProgress(float progress)
    {
        ChargeProgress = Mathf.Clamp01(progress);
    }
}