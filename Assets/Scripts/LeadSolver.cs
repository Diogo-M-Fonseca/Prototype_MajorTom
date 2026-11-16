using UnityEngine;
public static class LeadSolver
{
    public static bool Solve(
        Vector3 muzzle,
        Vector3 targetPos,
        Vector3 targetVel,
        float projectileSpeed,
        Vector3 shooterVel,
        bool inheritShooterSpeed,
        out Vector3 interceptPoint,
        out float timeToHit
    )
    {
        interceptPoint = targetPos;
        timeToHit = 0f;

        if (projectileSpeed <= 0.01f)
            return false;

        float speed = projectileSpeed;
        int iterations = inheritShooterSpeed ? 4 : 1;

        for (int i = 0; i < iterations; i++)
        {
            if (!SolveTime(targetPos - muzzle, targetVel, speed, out timeToHit))
            {
                timeToHit = 0f;
                interceptPoint = targetPos;
                return false;
            }

            interceptPoint = targetPos + targetVel * timeToHit;

            if (inheritShooterSpeed)
            {
                Vector3 dir = (interceptPoint - muzzle).normalized;
                speed = projectileSpeed + Mathf.Max(0f, Vector3.Dot(shooterVel, dir));
            }
        }

        return true;
    }

    // |d + v*t| = s*t  ->  (v.v - s²)t² + 2(d.v)t + d.d = 0
    private static bool SolveTime(Vector3 d, Vector3 v, float s, out float t)
    {
        t = 0f;
        float a = Vector3.Dot(v, v) - s * s;
        float b = 2f * Vector3.Dot(d, v);
        float c = Vector3.Dot(d, d);

        if (Mathf.Abs(a) < 1e-4f)
        {
            if (Mathf.Abs(b) < 1e-6f)
                return false;
            t = -c / b;
            return t > 0f;
        }

        float disc = b * b - 4f * a * c;
        if (disc < 0f)
            return false;

        float sqrt = Mathf.Sqrt(disc);
        float t1 = (-b - sqrt) / (2f * a);
        float t2 = (-b + sqrt) / (2f * a);
        float lo = Mathf.Min(t1, t2);
        float hi = Mathf.Max(t1, t2);

        t = lo > 0f ? lo : hi;
        return t > 0f;
    }
}
