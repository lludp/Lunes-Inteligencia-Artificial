using UnityEngine;

public static class SteeringBehaviours
{

    public static Vector3 Seek(Transform self, Vector3 target) => Seek(self.position, target);
    public static Vector3 Flee(Transform self, Vector3 target) => Flee(self.position, target);

    public static Vector3 Seek(Vector3 self, Vector3 target)
        => (target - self).normalized;

    public static Vector3 Flee(Vector3 self, Vector3 target)
        => (self - target).normalized;

    public static Vector3 Arrive(Vector3 self, Vector3 target, float slowingRadius, out float speedFactor)
    {
        Vector3 toTarget = target - self;
        float distance = toTarget.magnitude;

        if (distance < 0.001f)
        {
            speedFactor = 0f;
            return Vector3.zero;
        }

        speedFactor = slowingRadius > 0f ? Mathf.Clamp01(distance / slowingRadius) : 1f;
        return toTarget / distance;
    }

    public static Vector3 Pursue(Vector3 self, Vector3 targetPos, Vector3 targetVelocity, float predictionTime)
    {
        Vector3 futurePos = targetPos + targetVelocity * predictionTime;
        return Seek(self, futurePos);
    }

    public static Vector3 Evade(Vector3 self, Vector3 targetPos, Vector3 targetVelocity, float predictionTime)
    {
        Vector3 futurePos = targetPos + targetVelocity * predictionTime;
        return Flee(self, futurePos);
    }

    public static Vector3 Wander(Vector3 currentForward, ref float wanderAngle, float jitter, float radius)
    {
        wanderAngle += Random.Range(-jitter, jitter);
        Vector3 circleCenter = currentForward.normalized;
        Vector3 displacement = Quaternion.Euler(0f, wanderAngle, 0f) * Vector3.forward * radius;
        return (circleCenter + displacement).normalized;
    }
}
