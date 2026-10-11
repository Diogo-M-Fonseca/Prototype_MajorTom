using UnityEngine;


[CreateAssetMenu(fileName = "NewGun", menuName = "ScriptableObjects/New Gun")]
public class GunData : ScriptableObject
{
    [Header("Gun Info")]
    [SerializeField] private string gunName;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Gun Stats")]
    [SerializeField] private float damage;
    [SerializeField] private float fireRate;
    [SerializeField] private float projectileSpeed;
    [SerializeField] private float range;

    public string GunName => gunName;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float Damage => damage;
    public float FireRate => fireRate;
    public float ProjectileSpeed => projectileSpeed;
    public float Range => range;
}
