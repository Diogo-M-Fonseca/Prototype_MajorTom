using UnityEngine;


[CreateAssetMenu(fileName = "NewGun", menuName = "ScriptableObjects/New Gun")]
public class GunData : ScriptableObject
{
    [Header("Gun Info")]
    [SerializeField] private string gunName;

    [Header("Gun Stats")]
    [SerializeField] private float damage;
    [SerializeField] private float fireRate;
    [SerializeField] private float projectileSpeed;
    [SerializeField] private float range;
    [SerializeField] private int maxAmmo;
    [SerializeField] private float reloadDuration;

    public string GunName => gunName;
    public float Damage => damage;
    public float FireRate => fireRate;
    public float ProjectileSpeed => projectileSpeed;
    public float Range => range;
    public int MaxAmmo => maxAmmo;
    public float ReloadDuration => reloadDuration;
}
