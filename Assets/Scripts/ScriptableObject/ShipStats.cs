using UnityEngine;

[CreateAssetMenu(fileName = "ShipStats", menuName = "Scriptable Objects/ShipStats")]
public class ShipStats : ScriptableObject
{
    [SerializeField] private float _maxHealth = 100f;

    public float MaxHealth => _maxHealth;

}
