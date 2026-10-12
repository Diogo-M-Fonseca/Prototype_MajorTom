using UnityEngine;

[CreateAssetMenu(fileName = "MovementProfile", menuName = "Scriptable Objects/MovementProfile")]
public class MovementProfile : ScriptableObject
{
    [SerializeField] private float _minSpeed = 15f;
    [SerializeField] private float _maxSpeed = 35f;
    [SerializeField] private float _acceleration = 20f;
    [SerializeField] private float _lateralGrip = 100f;
    [SerializeField] private float _maxTurnRate = 120f;

    public float MinSpeed => _minSpeed;
    public float MaxSpeed => _maxSpeed;
    public float Acceleration => _acceleration;
    public float LateralGrip => _lateralGrip;
    public float MaxTurnRate => _maxTurnRate;


}
