using UnityEngine;

public class GunAimer : MonoBehaviour
{
    [SerializeField] private SpaceshipController ship;

    [SerializeField, Range(0f, 89f)] private float maxTraverseAngle;

    [SerializeField, Min(0f)] private float turnSpeed = 150f;

    [SerializeField, Min(0f)] private float convergeDistance;

    private Quaternion restLocalRotation;
    private Quaternion currentWorld;

    private void Awake()
    {
        restLocalRotation = transform.localRotation;
        currentWorld = transform.rotation;

        if (ship == null)
            ship = GetComponentInParent<SpaceshipController>();
    }
    private void Update()
    {
        if (ship == null || !ship.UsesFreeReticle)
            return;

        Quaternion? target = ComputeTarget(out bool fixedToHull);
        transform.rotation = fixedToHull && target.HasValue ? target.Value : currentWorld;
    }

    private void LateUpdate()
    {
        if (ship == null)
            return;

        if (!ship.UsesFreeReticle)
        {
            transform.localRotation = restLocalRotation;
            currentWorld = transform.rotation;
            return;
        }

        Quaternion? target = ComputeTarget(out bool fixedToHull);

        if (target == null)
            return;

        currentWorld = fixedToHull || turnSpeed <= 0f
            ? target.Value
            : Quaternion.RotateTowards(currentWorld, target.Value, turnSpeed * Time.deltaTime);

        transform.rotation = currentWorld;
    }

    private Quaternion? ComputeTarget(out bool fixedToHull)
    {
        fixedToHull = maxTraverseAngle <= 0.01f;

        Vector3 nose = ship.Nose;
        Vector3 dir = nose;

        if (!fixedToHull)
        {
            Vector3 ideal = ship.AimDirection;

            if (ideal.sqrMagnitude > 1e-6f)
                dir = Vector3.RotateTowards(nose, ideal, maxTraverseAngle * Mathf.Deg2Rad, 0f);
        }

        if (convergeDistance > 0f)
        {
            Vector3 toPoint = ship.transform.position + dir * convergeDistance - transform.position;

            if (toPoint.sqrMagnitude > 1e-4f)
                dir = toPoint.normalized;
        }

        if (dir.sqrMagnitude < 1e-6f)
            return null;

        return Quaternion.LookRotation(dir, ship.transform.up);
    }
}