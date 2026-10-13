using UnityEngine;

namespace SpaceFlight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class SpaceshipController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        float maxSpeed = 15f;

        [SerializeField, Min(0.1f)]
        float enginePower = 30f;

        [SerializeField, Range(0f, 1f)]
        float reverseRatio = 0.5f;

        [SerializeField, Min(0f)]
        float lateralGrip = 4f;

        [SerializeField]
        bool invertForward = true;

        [SerializeField, Min(1f)]
        float maxTurnRate = 120f;

        [SerializeField, Min(1f)]
        float turnAcceleration = 600f;

        [SerializeField]
        Transform visual;

        [SerializeField]
        float bankAngle = 35f;

        [SerializeField]
        float bankSmoothing = 8f;

        Rigidbody rb;
        Vector3 visualEuler;
        float yaw,
            yawRate,
            bank,
            turn,
            thrust;

        public void SetInput(float turn, float thrust)
        {
            this.turn = Mathf.Clamp(turn, -1f, 1f);
            this.thrust = Mathf.Clamp(thrust, -1f, 1f);
        }

        float DirSign => invertForward ? -1f : 1f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            rb.constraints =
                RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;

            yaw = transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (visual == transform)
            {
                Debug.LogWarning(
                    "SpaceshipController: 'visual' não pode ser a própria raiz. Atribui um filho.",
                    this
                );
                visual = null;
            }
            if (visual != null)
                visualEuler = visual.localEulerAngles;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            yawRate = Mathf.MoveTowards(yawRate, turn * maxTurnRate, turnAcceleration * dt);
            yaw += yawRate * dt;
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            rb.MoveRotation(rot);

            Vector3 fwd = rot * Vector3.forward * DirSign;
            Vector3 right = rot * Vector3.right;
            Vector3 v = rb.linearVelocity;
            float power = thrust >= 0f ? thrust : thrust * reverseRatio;
            float vf = Vector3.Dot(v, fwd);
            float vl = Vector3.Dot(v, right);
            Vector3 accel =
                fwd * (power * enginePower - vf * (enginePower / maxSpeed))
                - right * (vl * lateralGrip);
            rb.AddForce(accel, ForceMode.Acceleration);
        }

        void Update()
        {
            if (visual == null)
                return;
            float target = -DirSign * Mathf.Clamp(yawRate / maxTurnRate, -1f, 1f) * bankAngle;
            bank = Mathf.Lerp(bank, target, 1f - Mathf.Exp(-bankSmoothing * Time.deltaTime));
            visual.localRotation = Quaternion.Euler(
                visualEuler.x,
                visualEuler.y,
                visualEuler.z + bank
            );
        }
    }
}
