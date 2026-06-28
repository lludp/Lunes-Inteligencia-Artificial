using UnityEngine;

/// <summary>
/// Steering behaviors. Todas devuelven una dirección de movimiento (vector),
/// salvo Arrive que además modula la velocidad por su factor de frenado.
/// </summary>
public static class SteeringBehaviours
{
    // --- Compatibilidad con la versión anterior (reciben Transform) ---
    public static Vector3 Seek(Transform self, Vector3 target) => Seek(self.position, target);
    public static Vector3 Flee(Transform self, Vector3 target) => Flee(self.position, target);

    /// <summary>Ir directo hacia el objetivo.</summary>
    public static Vector3 Seek(Vector3 self, Vector3 target)
        => (target - self).normalized;

    /// <summary>Huir en dirección opuesta al objetivo.</summary>
    public static Vector3 Flee(Vector3 self, Vector3 target)
        => (self - target).normalized;

    /// <summary>
    /// Como Seek pero frenando al acercarse. <paramref name="speedFactor"/> (0..1)
    /// se usa para reducir la velocidad dentro del radio de frenado.
    /// </summary>
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
        return toTarget / distance; // dirección normalizada
    }

    /// <summary>
    /// Perseguir prediciendo la posición futura del objetivo según su velocidad.
    /// </summary>
    public static Vector3 Pursue(Vector3 self, Vector3 targetPos, Vector3 targetVelocity, float predictionTime)
    {
        Vector3 futurePos = targetPos + targetVelocity * predictionTime;
        return Seek(self, futurePos);
    }

    /// <summary>
    /// Evadir: como Pursue pero huyendo de la posición futura del objetivo.
    /// </summary>
    public static Vector3 Evade(Vector3 self, Vector3 targetPos, Vector3 targetVelocity, float predictionTime)
    {
        Vector3 futurePos = targetPos + targetVelocity * predictionTime;
        return Flee(self, futurePos);
    }

    /// <summary>
    /// Wander: deambular. Mantiene una dirección que varía suavemente con el tiempo.
    /// <paramref name="wanderAngle"/> es estado que el llamador debe conservar entre frames.
    /// </summary>
    public static Vector3 Wander(Vector3 currentForward, ref float wanderAngle, float jitter, float radius)
    {
        wanderAngle += Random.Range(-jitter, jitter);
        Vector3 circleCenter = currentForward.normalized;
        Vector3 displacement = Quaternion.Euler(0f, wanderAngle, 0f) * Vector3.forward * radius;
        return (circleCenter + displacement).normalized;
    }
}
