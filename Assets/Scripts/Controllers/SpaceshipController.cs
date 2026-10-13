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

        [SerializeField, Min(0.1f)]
        float throttleResponse = 4f;

        [SerializeField, Min(0f)]
        float lateralGrip = 4f;

        [SerializeField]
        bool invertForward = true;

        
        [SerializeField, Min(0f)]
        float mouseSensitivity = 0.15f;

        [SerializeField]
        bool invertMouseY = true;

        [SerializeField]
        bool invertMouseX = false;


        [SerializeField, Range(0f, 0.1f)]
        float aimSmoothing = 0.03f;

        [Header("Roll (A/D)")]
        [SerializeField, Min(1f)]
        float maxRollRate = 120f;

        [SerializeField, Min(1f)]
        float rollAcceleration = 600f;

        Rigidbody rb;
        float pendingPitch,
            pendingYaw,
            rollRate,
            roll,
            thrust,
            throttle;
        Vector2 smoothedStep; 

        float DirSign => invertForward ? -1f : 1f;

        public void SetInput(float roll, float thrust)
        {
            this.roll = Mathf.Clamp(roll, -1f, 1f);
            this.thrust = Mathf.Clamp(thrust, -1f, 1f);
        }

        public void AddLook(float mouseX, float mouseY)
        {
            float x = invertMouseX ? -mouseX : mouseX;
            float y = invertMouseY ? -mouseY : mouseY;
            pendingYaw += x * mouseSensitivity * DirSign;
            pendingPitch += -y * mouseSensitivity * DirSign;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.None;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            Vector2 raw = new Vector2(pendingPitch, pendingYaw);
            pendingPitch = 0f;
            pendingYaw = 0f;

            float a = aimSmoothing <= 0f ? 1f : 1f - Mathf.Exp(-dt / aimSmoothing);
            smoothedStep += (raw - smoothedStep) * a;
            if (raw == Vector2.zero && smoothedStep.sqrMagnitude < 1e-6f)
                smoothedStep = Vector2.zero;

            float pitchStep = smoothedStep.x;
            float yawStep = smoothedStep.y;

            rollRate = Mathf.MoveTowards(rollRate, roll * maxRollRate, rollAcceleration * dt);
            float rollStep = -rollRate * DirSign * dt;

            rb.angularVelocity = Vector3.zero;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(pitchStep, yawStep, rollStep));

            Vector3 fwd = rb.rotation * Vector3.forward * DirSign;
            Vector3 v = rb.linearVelocity;
            float vf = Vector3.Dot(v, fwd);
            Vector3 sideways = v - fwd * vf;

            throttle = Mathf.MoveTowards(throttle, thrust, throttleResponse * dt);
            float power = throttle >= 0f ? throttle : throttle * reverseRatio;
            Vector3 accel =
                fwd * (power * enginePower - vf * (enginePower / maxSpeed))
                - sideways * lateralGrip;
            rb.AddForce(accel, ForceMode.Acceleration);
        }
    }
}
