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

        Vector3 relativeVel = inheritShooterSpeed ? targetVel - shooterVel : targetVel;

        if (!SolveTime(targetPos - muzzle, relativeVel, projectileSpeed, out timeToHit))
        {
            timeToHit = 0f;
            interceptPoint = targetPos;
            return false;
        }

        interceptPoint = targetPos + relativeVel * timeToHit;

        return true;
    }

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