using UnityEngine;
using UnityEngine.InputSystem;

public class ResourceDelivery : MonoBehaviour
{
    [SerializeField] private ResourceCollector _collector;

    [SerializeField] private InputActionReference _deliverAction;

    [SerializeField] private AudioClip _deliverClip;

    [Header("Voo ate ao planeta")]
    [SerializeField, Min(1f)] private float _flySpeed = 60f;
    [SerializeField, Min(1f)] private float _flyAcceleration = 120f;
    [SerializeField, Min(0f)] private float _launchInterval = 0.12f;

    private readonly System.Collections.Generic.List<ResourcePickup> _launching = new();

    public ResourcePlanet NearbyPlanet { get; private set; }

    public string KeyLabel => _deliverAction != null ? _deliverAction.action.GetBindingDisplayString() : "E";

    public event System.Action<ResourcePlanet, int> Delivered;

    public event System.Action<ResourcePlanet> Rejected;

    private void Awake()
    {
        if (_collector == null)
            _collector = GetComponent<ResourceCollector>();
    }

    private void OnEnable()
    {
        if (_deliverAction != null)
            _deliverAction.action.Enable();
    }

    private void Update()
    {
        NearbyPlanet = FindNearestPlanet();

        if (NearbyPlanet != null && WasPressed())
            Deliver(NearbyPlanet);
    }

    private bool WasPressed()
    {
        if (_deliverAction != null)
            return _deliverAction.action.WasPressedThisFrame();

        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }

    private ResourcePlanet FindNearestPlanet()
    {
        ResourcePlanet best = null;
        float bestSqr = float.MaxValue;
        Vector3 position = transform.position;

        foreach (ResourcePlanet planet in ResourcePlanet.All)
        {
            float sqr = (planet.transform.position - position).sqrMagnitude;
            float radius = planet.DeliveryRadius;

            if (sqr > radius * radius || sqr >= bestSqr)
                continue;

            best = planet;
            bestSqr = sqr;
        }

        return best;
    }

    private void Deliver(ResourcePlanet planet)
    {
        if (_collector.Count == 0)
            return;

        if (planet.Remaining <= 0)
        {
            Rejected?.Invoke(planet);
            return;
        }

        int accepted = planet.Reserve(_collector.Count);

        _launching.Clear();
        int released = _collector.DetachCarried(accepted, _launching);

        for (int i = _launching.Count; i < accepted; i++)
            planet.CancelReservation();

        if (released <= 0)
            return;

        for (int i = 0; i < _launching.Count; i++)
            _launching[i].DeliverTo(planet, _flySpeed, _flyAcceleration, i * _launchInterval);

        _launching.Clear();

        if (_deliverClip != null)
            AudioSource.PlayClipAtPoint(_deliverClip, planet.transform.position);

        Delivered?.Invoke(planet, released);
    }
}