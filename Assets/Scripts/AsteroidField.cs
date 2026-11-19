using UnityEngine;

public class AsteroidField : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private AsteroidPool _pool;

    [Header("Stats")]
    [SerializeField, Min(1f)] private float _health = 100f;

    [Header("Fragments")]
    [SerializeField, Min(0)] private int _fragmentGenerations = 2;
    [SerializeField] private Vector2Int _fragmentCount = new Vector2Int(3, 5);
    [SerializeField, Range(0.05f, 1f)] private float _fragmentScale = 0.4f;
    [SerializeField, Min(0f)] private float _scatterSpeed = 12f;
    [SerializeField, Min(0f)] private float _maxSpinSpeed = 90f;

    [Header("Resorces")]
    [SerializeField] private Vector2Int _resourceCount = new Vector2Int(1, 2);
    [SerializeField, Range(0f, 1f)] private float _dropChance = 1f;
    [SerializeField, Min(0f)] private float _resourceScatterSpeed = 6f;

    [Header("Feedback")]
    [SerializeField] private AudioClip _breakClip;
    [SerializeField] private ParticleSystem _breakEffect;

    public AsteroidPool Pool => _pool;
    public float Health => _health;
    public int FragmentGenerations => _fragmentGenerations;
    public Vector2Int FragmentCount => _fragmentCount;
    public float FragmentScale => _fragmentScale;
    public float ScatterSpeed => _scatterSpeed;
    public float MaxSpinSpeed => _maxSpinSpeed;
    public Vector2Int ResourceCount => _resourceCount;
    public float DropChance => _dropChance;
    public float ResourceScatterSpeed => _resourceScatterSpeed;
    public AudioClip BreakClip => _breakClip;
    public ParticleSystem BreakEffect => _breakEffect;
}
