using UnityEngine;

// Parametros de partida para os assets de um tipo de inimigo.
// O nome e todos os valores sao teus: cada tipo e simplesmente o nome + os numeros que escolheres.
[System.Serializable]
public class EnemyDataSettings
{
    [Header("Identificacao")]
    [SerializeField] private string _name = "NewEnemy";
    [SerializeField] private string _folder = "Assets/Enemies/Data";

    [Header("Stats")]
    [SerializeField] private float _maxHealth = 100f;

    [Header("Movimento")]
    [SerializeField] private float _minSpeed = 15f;
    [SerializeField] private float _maxSpeed = 35f;
    [SerializeField] private float _acceleration = 20f;
    [SerializeField] private float _lateralGrip = 100f;
    [SerializeField] private float _maxTurnRate = 100f;

    [Header("Perfil de comportamento")]
    [SerializeField] private bool _createFlockProfile = true;
    [SerializeField] private bool _createSoloProfile = true;
    [SerializeField] private float _neighbourRadius = 60f;
    [SerializeField] private float _separationRadius = 15f;
    [SerializeField] private float _pursueWeight = 2f;
    [SerializeField] private float _separationWeight = 1.5f;
    [SerializeField] private float _alignmentWeight = 1f;
    [SerializeField] private float _cohesionWeight = 0.8f;
    [SerializeField] private float _avoidWeight = 5f;

    [Header("Behaviours com parametros")]
    [SerializeField] private float _leadFactor = 1f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private float _avoidDistance = 40f;
    [SerializeField] private float _avoidRadius = 2.5f;

    [Header("Arma (opcional)")]
    [SerializeField] private bool _createGun;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _fireRate = 4f;
    [SerializeField] private float _projectileSpeed = 80f;
    [SerializeField] private float _range = 150f;

    public string Name => _name;
    public string Folder => _folder;

    public float MaxHealth => _maxHealth;

    public float MinSpeed => _minSpeed;
    public float MaxSpeed => _maxSpeed;
    public float Acceleration => _acceleration;
    public float LateralGrip => _lateralGrip;
    public float MaxTurnRate => _maxTurnRate;

    public bool CreateFlockProfile => _createFlockProfile;
    public bool CreateSoloProfile => _createSoloProfile;
    public float NeighbourRadius => _neighbourRadius;
    public float SeparationRadius => _separationRadius;
    public float PursueWeight => _pursueWeight;
    public float SeparationWeight => _separationWeight;
    public float AlignmentWeight => _alignmentWeight;
    public float CohesionWeight => _cohesionWeight;
    public float AvoidWeight => _avoidWeight;

    public float LeadFactor => _leadFactor;
    public LayerMask ObstacleMask => _obstacleMask;
    public float AvoidDistance => _avoidDistance;
    public float AvoidRadius => _avoidRadius;

    public bool CreateGun => _createGun;
    public float Damage => _damage;
    public float FireRate => _fireRate;
    public float ProjectileSpeed => _projectileSpeed;
    public float Range => _range;

    public void SetObstacleMask(LayerMask mask) => _obstacleMask = mask;
}
