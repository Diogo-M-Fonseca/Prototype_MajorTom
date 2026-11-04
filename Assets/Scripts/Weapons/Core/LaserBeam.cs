using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField, Min(0.1f)] private float range = 500f;
    [SerializeField, Min(0f)] private float damagePerSecond = 20f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Visual")]
    [SerializeField] private LineRenderer laserLine;

    private Transform firingPoint;
    private bool isActive;

    private void Awake()
    {
        if (laserLine == null)
        {
            Debug.LogError("LaserBeam: LineRenderer não configurado.", this);
            return;
        }

        laserLine.useWorldSpace = true;
        laserLine.positionCount = 2;
        laserLine.enabled = false;
    }

    private void Update()
    {
        if (!isActive || firingPoint == null || laserLine == null)
            return;

        UpdateBeam();
    }

    public void StartBeam(Transform origin)
    {
        if (origin == null || laserLine == null)
            return;

        firingPoint = origin;
        isActive = true;
        laserLine.enabled = true;
    }

    public void StopBeam()
    {
        isActive = false;
        firingPoint = null;

        if (laserLine != null)
            laserLine.enabled = false;
    }

    private void UpdateBeam()
    {
        Vector3 origin = firingPoint.position;
        Vector3 direction = firingPoint.forward;

        Vector3 endPosition = origin + direction * range;

        if (Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            range,
            hitMask,
            QueryTriggerInteraction.Ignore))
        {
            endPosition = hit.point;
            Debug.Log($"Laser hit: {hit.collider.name} (layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}, damageable={hit.collider.GetComponentInParent<IDamageable>() != null})", this);

            IDamageable damageable =
                hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }

        laserLine.SetPosition(0, origin);
        laserLine.SetPosition(1, endPosition);
    }
}
