using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    Transform target;

    [Tooltip("Igual ao 'invertForward' da nave.")]
    [SerializeField]
    bool invertForward = true;

    [SerializeField, Min(0f)]
    float distance = 10f;

    [SerializeField]
    float height = 3f;

    [Tooltip("Distância à frente da nave para onde a câmera olha.")]
    [SerializeField, Min(0f)]
    float lookAheadDistance = 15f;

    [SerializeField, Min(0.01f)]
    float positionSmoothTime = 0.08f;

    [Tooltip("Maior = a câmera acompanha a rotação mais depressa.")]
    [SerializeField, Min(0.1f)]
    float rotationSharpness = 8f;

    Vector3 posVelocity;

    void Start()
    {
        if (target == null)
        {
            enabled = false;
            return;
        }
        SnapToTarget();
    }

    Vector3 Forward => target.forward * (invertForward ? -1f : 1f);

    Vector3 DesiredPosition => target.position - Forward * distance + target.up * height;

    Quaternion DesiredRotation()
    {
        Vector3 lookPoint = target.position + Forward * lookAheadDistance;
        return Quaternion.LookRotation(lookPoint - transform.position, target.up);
    }

    void SnapToTarget()
    {
        transform.position = DesiredPosition;
        transform.rotation = DesiredRotation();
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            DesiredPosition,
            ref posVelocity,
            positionSmoothTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            DesiredRotation(),
            1f - Mathf.Exp(-rotationSharpness * dt)
        );
    }
}
