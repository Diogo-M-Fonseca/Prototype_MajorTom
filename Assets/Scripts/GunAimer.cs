using UnityEngine;

public class GunAimer : MonoBehaviour
{
    [SerializeField] private SpaceshipController ship;

    private Quaternion restLocalRotation;

    private void Awake()
    {
        restLocalRotation = transform.localRotation;
        if (ship == null)
            ship = GetComponentInParent<SpaceshipController>();
    }

    private void LateUpdate() => Apply();

    private void Update() => Apply();

    private void Apply()
    {
        if (ship == null)
            return;

        if (!ship.UsesFreeReticle)
        {
            transform.localRotation = restLocalRotation;
            return;
        }

        Vector3 dir = ship.AimDirection;
        if (dir.sqrMagnitude < 1e-6f)
            return;

        transform.rotation = Quaternion.LookRotation(dir, ship.transform.up);
    }
}