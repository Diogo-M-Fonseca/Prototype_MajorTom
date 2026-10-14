using UnityEngine;

// Valores de partida para o CameraFollow que segue o jogador.
// Os valores por defeito sao os do proprio CameraFollow.
[System.Serializable]
public class CameraFollowSettings
{
    [SerializeField, Min(0f)] private float _distance = 10f;
    [SerializeField] private float _height = 3f;
    [Tooltip("Distancia a frente da nave para onde a camara olha.")]
    [SerializeField, Min(0f)] private float _lookAheadDistance = 15f;
    [SerializeField, Min(0.01f)] private float _positionSmoothTime = 0.08f;
    [SerializeField, Min(0.1f)] private float _rotationSharpness = 8f;

    public float Distance => _distance;
    public float Height => _height;
    public float LookAheadDistance => _lookAheadDistance;
    public float PositionSmoothTime => _positionSmoothTime;
    public float RotationSharpness => _rotationSharpness;
}
