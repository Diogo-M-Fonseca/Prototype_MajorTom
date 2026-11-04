using UnityEngine;

// Parametros de partida para o SatelliteRing de um prefab de satelite (usados pela janela Ship Creator).
[System.Serializable]
public class SatelliteSettings
{
    [Header("Zona de passagem")]
    [Tooltip("Calcula o raio interior a partir do tamanho do modelo. Desliga para usar o valor fixo abaixo.")]
    [SerializeField] private bool _autoRadius = true;
    [Tooltip("Fracao do raio do modelo (o menor entre largura e altura) usada como raio interior. " +
             "Mais pequeno = a passagem exige ir mais ao centro.")]
    [SerializeField, Range(0.1f, 1f)] private float _autoRadiusFactor = 0.6f;
    [Tooltip("Raio fixo, em unidades locais (so usado com o raio automatico desligado).")]
    [SerializeField, Min(0.1f)] private float _innerRadius = 20f;
    [Tooltip("Ligado: o aro so pode ser usado uma vez.")]
    [SerializeField] private bool _oneShot;
    [Tooltip("Segundos ate o aro poder ser usado de novo (ignorado se One Shot).")]
    [SerializeField, Min(0f)] private float _cooldown = 5f;

    [Header("Boost")]
    [SerializeField, Min(1f)] private float _speedMultiplier = 3f;
    [SerializeField, Min(0.1f)] private float _boostDuration = 4f;

    [Header("Escudo")]
    [SerializeField, Min(0f)] private float _shieldAmount = 50f;
    [SerializeField, Min(0.1f)] private float _shieldDuration = 10f;

    [Header("Feedback")]
    [SerializeField] private AudioClip _passClip;

    public bool AutoRadius => _autoRadius;
    public float AutoRadiusFactor => _autoRadiusFactor;
    public float InnerRadius => _innerRadius;
    public bool OneShot => _oneShot;
    public float Cooldown => _cooldown;

    public float SpeedMultiplier => _speedMultiplier;
    public float BoostDuration => _boostDuration;

    public float ShieldAmount => _shieldAmount;
    public float ShieldDuration => _shieldDuration;

    public AudioClip PassClip => _passClip;
}
