using UnityEngine;


[DefaultExecutionOrder(10000)]
public class AimPositioner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform da nave que define a direção de mira.")]
    [SerializeField]
    private Transform shipTransform;

    [Tooltip(
        "Opcional: ponto de onde sai a direção de mira (ex.: o firingPoint). Se vazio, usa o Ship Transform."
    )]
    [SerializeField]
    private Transform aimOrigin;

    [Tooltip("Elemento de UI (RectTransform) que representa a mira.")]
    [SerializeField]
    private RectTransform aimRect;

    [Tooltip("Câmara usada para projetar o ponto. Se vazio, usa a Camera.main.")]
    [SerializeField]
    private Camera aimCamera;

    [Header("Settings")]
    [Tooltip("Distância (em metros) à frente da nave onde a mira é projetada.")]
    [SerializeField]
    private float aimDistance = 100f;

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

        Vector3 worldPoint = origin.position + origin.forward * aimDistance;
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
