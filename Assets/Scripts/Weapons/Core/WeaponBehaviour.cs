using UnityEngine;

public abstract class WeaponBehaviour : ScriptableObject
{
    public abstract void StartFiring(Weapon weapon);
    public abstract void StopFiring(Weapon weapon);
    public abstract void Tick(Weapon weapon, float deltaTime);
    public virtual void OnEquipped(Weapon weapon) { }
    public virtual void OnUnequipped(Weapon weapon) { }
}
