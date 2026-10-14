using UnityEngine;

// Valores de partida para o SpaceshipController e o ShipInputAdapter do jogador.
// Os valores por defeito sao os do proprio SpaceshipController.
[System.Serializable]
public class PlayerMovementSettings
{
    [Header("Nave")]
    [SerializeField, Min(0.1f)] private float _maxSpeed = 15f;
    [SerializeField, Min(0.1f)] private float _enginePower = 30f;
    [SerializeField, Range(0f, 1f)] private float _reverseRatio = 0.5f;
    [SerializeField, Min(0.1f)] private float _throttleResponse = 4f;
    [SerializeField, Min(0f)] private float _lateralGrip = 4f;
    [Tooltip("Liga so se o nariz do modelo apontar para -Z. A camara usa o mesmo valor. " +
             "O ponto de disparo que a tool cria fica em +Z.")]
    [SerializeField] private bool _invertForward;

    [Header("Rato")]
    [SerializeField, Min(0f)] private float _mouseSensitivity = 0.15f;
    [SerializeField] private bool _invertMouseY = true;
    [SerializeField] private bool _invertMouseX;
    [SerializeField, Range(0f, 0.1f)] private float _aimSmoothing = 0.03f;

    [Header("Roll (A/D)")]
    [SerializeField, Min(1f)] private float _maxRollRate = 120f;
    [SerializeField, Min(1f)] private float _rollAcceleration = 600f;

    [Header("Input")]
    [SerializeField] private bool _lockCursor = true;

    public float MaxSpeed => _maxSpeed;
    public float EnginePower => _enginePower;
    public float ReverseRatio => _reverseRatio;
    public float ThrottleResponse => _throttleResponse;
    public float LateralGrip => _lateralGrip;
    public bool InvertForward => _invertForward;

    public float MouseSensitivity => _mouseSensitivity;
    public bool InvertMouseY => _invertMouseY;
    public bool InvertMouseX => _invertMouseX;
    public float AimSmoothing => _aimSmoothing;

    public float MaxRollRate => _maxRollRate;
    public float RollAcceleration => _rollAcceleration;

    public bool LockCursor => _lockCursor;
}
