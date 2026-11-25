using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class SpaceshipController : MonoBehaviour
{
    [Header("Flight Mode")]
    [SerializeField]
    FlightMode flightMode = FlightMode.DirectNose;

    [SerializeField, Range(5f, 89f)]
    float reticleConeAngle = 35f;

    [SerializeField]
    bool reticleConeBySpeed = true;

    [SerializeField, Range(5f, 89f)]
    float reticleConeAtMaxSpeed = 60f;

    [SerializeField, Range(0.05f, 2f)]
    float alignSpeedAtStandstill = 0.4f;

    [SerializeField, Min(0.1f)]
    float alignSpeedAtMaxSpeed = 2f;

    [SerializeField, Min(0.1f)]
    float reticleConeResponse = 6f;

    [SerializeField, Min(1f)]
    float maxPitchRate = 70f;

    [SerializeField, Min(1f)]
    float maxYawRate = 90f;

    [SerializeField, Min(1f)]
    float turnAcceleration = 300f;

    [SerializeField, Range(0f, 80f)]
    float bankAngle = 35f;

    [SerializeField, Range(0f, 45f)]
    float pitchTiltAngle = 12f;

    [SerializeField, Min(0.1f)]
    float bankResponse = 8f;

    [SerializeField]
    Transform bankVisual;

    [Header("Movement")]
    [SerializeField, Min(0.1f)]
    float maxSpeed = 15f;

    [SerializeField, Min(0.1f)]
    float enginePower = 30f;

    [SerializeField, Range(0f, 1f)]
    float reverseRatio = 0.5f;

    [SerializeField, Min(0.1f)]
    float throttleResponse = 4f;

    [SerializeField, Range(0f, 1f)]
    float idleThrottle = 0.3f;

    [SerializeField, Min(1f)]
    float limitBrakeDecel = 40f;

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
    float boostMultiplier = 1f;
    bool boostForcesThrottle;

    float DirSign => invertForward ? -1f : 1f;

    Vector3 aimDir = Vector3.forward;
    Vector2 turnRate;
    float bank;
    float pitchTilt;
    Quaternion bankBaseRotation = Quaternion.identity;

    float currentCone = -1f;

    public bool UsesFreeReticle => flightMode == FlightMode.FreeReticle;

    public float CurrentReticleCone => currentCone < 0f ? reticleConeAngle : currentCone;

    public Vector3 Nose => transform.rotation * Vector3.forward * DirSign;

    public Vector3 AimDirection => UsesFreeReticle ? aimDir : Nose;

    float speedLimit = 1f;
    float turnLimit = 1f;

    public void SetMovementLimit(float speedFraction, float turnScale)
    {
        speedLimit = Mathf.Clamp01(speedFraction);
        turnLimit = Mathf.Max(0f, turnScale);
    }

    public void ClearMovementLimit()
    {
        speedLimit = 1f;
        turnLimit = 1f;
    }

    public void SetBoost(float multiplier, bool forceThrottle)
    {
        boostMultiplier = Mathf.Max(1f, multiplier);
        boostForcesThrottle = forceThrottle;
    }

    public void SetInput(float roll, float thrust)
    {
        this.roll = Mathf.Clamp(roll, -1f, 1f);
        this.thrust = Mathf.Clamp(thrust, -1f, 1f);
    }

    public void AddRotationDegrees(float pitch, float yaw)
    {
        pendingPitch += pitch;
        pendingYaw += yaw;
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

        aimDir = Nose;
        if (bankVisual == null)
            bankVisual = transform.Find("Model");
        if (bankVisual != null)
            bankBaseRotation = bankVisual.localRotation;
    }

    void LateUpdate()
    {
        if (bankVisual == null)
            return;

        float target = 0f;
        if (UsesFreeReticle && bankAngle > 0f)
            target = -Mathf.Clamp(turnRate.y / maxYawRate, -1f, 1f) * bankAngle * DirSign;

        float tiltTarget = UsesFreeReticle ? Mathf.Clamp(turnRate.x / maxPitchRate, -1f, 1f) * pitchTiltAngle : 0f;

        float a = 1f - Mathf.Exp(-bankResponse * Time.deltaTime);
        bank = Mathf.Lerp(bank, target, a);
        pitchTilt = Mathf.Lerp(pitchTilt, tiltTarget, a);
        bankVisual.localRotation = bankBaseRotation * Quaternion.Euler(pitchTilt, 0f, bank);
    }

    static float DesiredRate(float error, float maxRate, float accel, float dt)
    {
        float a = Mathf.Abs(error);
        float rate = Mathf.Min(maxRate, Mathf.Sqrt(2f * accel * a), a / dt);
        return Mathf.Sign(error) * rate;
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

        if (UsesFreeReticle)
        {
            Quaternion rot = rb.rotation;
            Vector3 nose = rot * Vector3.forward * DirSign;

            if (pitchStep != 0f || yawStep != 0f)
            {
                Vector3 local = Quaternion.Inverse(rot) * aimDir;
                aimDir = rot * (Quaternion.Euler(pitchStep, yawStep, 0f) * local);
            }

            float speedFraction = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
            float targetCone = reticleConeBySpeed
                ? Mathf.Lerp(reticleConeAngle, reticleConeAtMaxSpeed, speedFraction)
                : reticleConeAngle;
            float alignScale = reticleConeBySpeed
                ? Mathf.Lerp(alignSpeedAtStandstill, alignSpeedAtMaxSpeed, speedFraction)
                : 1f;
            alignScale *= turnLimit;

            currentCone = currentCone < 0f
                ? targetCone
                : Mathf.Lerp(currentCone, targetCone, 1f - Mathf.Exp(-reticleConeResponse * dt));

            if (Vector3.Angle(nose, aimDir) > currentCone)
                aimDir = Vector3.RotateTowards(nose, aimDir, currentCone * Mathf.Deg2Rad, 0f);
            aimDir.Normalize();

            Quaternion world = Quaternion.FromToRotation(nose, aimDir);
            Quaternion localErr = Quaternion.Inverse(rot) * world * rot;
            localErr.ToAngleAxis(out float errAngle, out Vector3 errAxis);
            if (errAngle > 180f)
                errAngle -= 360f;
            Vector3 err = float.IsNaN(errAxis.x) || float.IsInfinity(errAxis.x) ? Vector3.zero : errAxis * errAngle;

            float alignAccel = turnAcceleration * alignScale;
            float wantPitch = DesiredRate(err.x, maxPitchRate * alignScale, alignAccel, dt);
            float wantYaw = DesiredRate(err.y, maxYawRate * alignScale, alignAccel, dt);
            turnRate.x = Mathf.MoveTowards(turnRate.x, wantPitch, alignAccel * dt);
            turnRate.y = Mathf.MoveTowards(turnRate.y, wantYaw, alignAccel * dt);

            rb.MoveRotation(rot * Quaternion.Euler(turnRate.x * dt, turnRate.y * dt, rollStep));
        }
        else
        {
            turnRate = Vector2.zero;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(pitchStep, yawStep, rollStep));
            aimDir = Nose;
        }

        Vector3 fwd = rb.rotation * Vector3.forward * DirSign;
        Vector3 v = rb.linearVelocity;
        float vf = Vector3.Dot(v, fwd);
        Vector3 sideways = v - fwd * vf;

        float targetThrottle = boostForcesThrottle ? 1f : (thrust < -0.01f ? thrust : Mathf.Max(thrust, idleThrottle));

        if (speedLimit < 0.999f)
            targetThrottle = 0f;
        throttle = Mathf.MoveTowards(throttle, targetThrottle, throttleResponse * dt);
        float power = throttle >= 0f ? throttle : throttle * reverseRatio;

        float effectivePower = enginePower * boostMultiplier;
        float effectiveMaxSpeed = maxSpeed * boostMultiplier;
        Vector3 accel =
            fwd * (power * effectivePower - vf * (effectivePower / effectiveMaxSpeed))
            - sideways * lateralGrip;
        rb.AddForce(accel, ForceMode.Acceleration);

        if (speedLimit < 0.999f)
        {
            float cap = effectiveMaxSpeed * speedLimit;
            Vector3 current = rb.linearVelocity;

            if (current.magnitude > cap)
            {
                Vector3 goal = cap <= 0.01f ? Vector3.zero : current.normalized * cap;
                rb.linearVelocity = Vector3.MoveTowards(current, goal, limitBrakeDecel * dt);
            }
        }
    }
}