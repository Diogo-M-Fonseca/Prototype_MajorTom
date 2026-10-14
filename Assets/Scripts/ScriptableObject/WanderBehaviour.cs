using UnityEngine;

[CreateAssetMenu(menuName = "Ships/Steering/Wander")]
public class WanderBehaviour : SteeringBehaviour
{
    [SerializeField] private float _frequency = 0.8f;

    [SerializeField, Range(0f, 1f)] private float _verticalAmount = 0.6f;

    public override Vector3 Calculate(in SteeringContext context)
    {
        Vector3 forward = context.Forward;

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
        right.Normalize();

        Vector3 up = Vector3.Cross(forward, right);

        float t = Time.time * _frequency;

        float x = Mathf.Clamp((Mathf.PerlinNoise(t, context.Seed) - 0.5f) * 3f, -1f, 1f);
        float y = Mathf.Clamp((Mathf.PerlinNoise(context.Seed + 37.1f, t) - 0.5f) * 3f, -1f, 1f);

        return right * x + up * (y * _verticalAmount);
    }
}
