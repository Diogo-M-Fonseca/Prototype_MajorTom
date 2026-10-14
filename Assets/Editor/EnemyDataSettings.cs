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

    [Header("Perfis estaticos (sem FSM)")]
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

    [Header("FSM (estados e transicoes)")]
    [SerializeField] private bool _createFsm = true;
    [SerializeField, Min(0f)] private float _commitTime = 1f;

    [Header("FSM: pesos por estado")]
    [SerializeField] private float _wanderWeight = 1f;
    [SerializeField] private float _orbitWeight = 2f;
    [SerializeField] private float _fleeWeight = 3f;

    [Header("FSM: Wander (Patrol)")]
    [SerializeField] private float _wanderFrequency = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _wanderVertical = 0.6f;

    [Header("FSM: distancias das transicoes")]
    [SerializeField] private float _detectRange = 300f;
    [SerializeField] private float _loseRange = 500f;
    [SerializeField] private float _closeRange = 40f;
    [SerializeField] private float _orbitDistance = 60f;
    [SerializeField] private float _orbitBreakRange = 120f;

    [Header("FSM: fuga e duracoes")]
    [SerializeField, Range(0f, 1f)] private float _fleeHealthFraction = 0.25f;
    [SerializeField] private float _orbitMinDuration = 3f;
    [SerializeField] private float _orbitMaxDuration = 6f;
    [SerializeField] private float _fleeMinDuration = 4f;
    [SerializeField] private float _fleeMaxDuration = 7f;

    [Header("Feedback")]
    [SerializeField] private bool _hitFlash = true;
    [SerializeField, Min(0.01f)] private float _hitFlashDuration = 0.08f;

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

    public bool CreateFsm => _createFsm;
    public float CommitTime => _commitTime;
    public float WanderWeight => _wanderWeight;
    public float OrbitWeight => _orbitWeight;
    public float FleeWeight => _fleeWeight;
    public float WanderFrequency => _wanderFrequency;
    public float WanderVertical => _wanderVertical;
    public float DetectRange => _detectRange;
    public float LoseRange => _loseRange;
    public float CloseRange => _closeRange;
    public float OrbitDistance => _orbitDistance;
    public float OrbitBreakRange => _orbitBreakRange;
    public float FleeHealthFraction => _fleeHealthFraction;
    public float OrbitMinDuration => _orbitMinDuration;
    public float OrbitMaxDuration => _orbitMaxDuration;
    public float FleeMinDuration => _fleeMinDuration;
    public float FleeMaxDuration => _fleeMaxDuration;

    public bool HitFlash => _hitFlash;
    public float HitFlashDuration => _hitFlashDuration;

    public bool CreateGun => _createGun;
    public float Damage => _damage;
    public float FireRate => _fireRate;
    public float ProjectileSpeed => _projectileSpeed;
    public float Range => _range;

    public void SetObstacleMask(LayerMask mask) => _obstacleMask = mask;
}
