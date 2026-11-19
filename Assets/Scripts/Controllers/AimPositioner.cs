using UnityEngine;


[DefaultExecutionOrder(10000)]
public class AimPositioner : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform shipTransform;


    [SerializeField]
    private Transform aimOrigin;

    [SerializeField]
    private RectTransform aimRect;

    [SerializeField]
    private Camera aimCamera;

    [Header("Settings")]
    [SerializeField]
    private float aimDistance = 100f;

    public float AimDistance => aimDistance;

    public Transform AimOriginTransform => aimOrigin != null ? aimOrigin : shipTransform;

    private SpaceshipController ship;
    private RectTransform aimParentRect;
    private Camera aimUICamera;

    private void Awake()
    {
        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (aimCamera == null)
        {
            Debug.LogError("AimPositioner: não há câmara (Aim Camera vazio e sem Camera.main).", this);
        }

        Transform shipRoot = shipTransform != null ? shipTransform : aimOrigin;
        if (shipRoot != null)
            ship = shipRoot.GetComponentInParent<SpaceshipController>();

        if (shipTransform == null && aimOrigin == null)
        {
            Debug.LogError("AimPositioner: Ship Transform (ou Aim Origin) não foi atribuído.", this);
        }

        if (aimRect == null)
        {
            Debug.LogError("AimPositioner: Aim Rect não foi atribuído.", this);
            return;
        }

        aimParentRect = aimRect.parent as RectTransform;

        Canvas canvas = aimRect.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            aimUICamera = canvas.worldCamera;
        }
    }

    private void LateUpdate()
    {
        UpdateAimPosition();
    }

    private void UpdateAimPosition()
    {
        Transform origin = aimOrigin != null ? aimOrigin : shipTransform;

        if (aimRect == null || aimParentRect == null || aimCamera == null || origin == null)
        {
            return;
        }

        Vector3 worldPoint =
            ship != null && ship.UsesFreeReticle
                ? origin.position + ship.AimDirection * Mathf.Abs(aimDistance)
                : origin.position + origin.forward * aimDistance;
        Vector3 screenPoint = aimCamera.WorldToScreenPoint(worldPoint);

        if (screenPoint.z <= 0f)
        {
            return;
        }

        if (
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                aimParentRect,
                screenPoint,
                aimUICamera,
                out Vector2 localPoint
            )
        )
        {
            aimRect.localPosition = new Vector3(
                localPoint.x,
                localPoint.y,
                aimRect.localPosition.z
            );
        }
    }
}