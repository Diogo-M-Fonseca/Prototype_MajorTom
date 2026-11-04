using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Data", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Information")]
    [SerializeField] private string weaponName;
    [SerializeField] private Sprite weaponIcon;

    [Header("Behavior")]
    [SerializeField] private WeaponBehaviour weaponBehaviour;

    [Header("Overheat Settings")]
    [SerializeField, Min(0.1f)] private float heatPerSecond = 0.25f;
    [SerializeField, Min(0.1f)] private float coolingPerSecond = 0.15f;
    [SerializeField, Range(0f, 1f)] private float resumeHeatThreshold = 0.3f;

    public string WeaponName => weaponName;
    public Sprite WeaponIcon => weaponIcon;
    public WeaponBehaviour WeaponBehaviour => weaponBehaviour;
    public float HeatPerSecond => heatPerSecond;
    public float CoolingPerSecond => coolingPerSecond;
    public float ResumeHeatThreshold => resumeHeatThreshold;
}
