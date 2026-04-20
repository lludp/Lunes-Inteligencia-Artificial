using UnityEngine;

public static class SteeringBehaviours
{
    public static Vector3 Seek(Transform self, Vector3 target) => (target - self.position).normalized;
    public static Vector3 Flee(Transform self, Vector3 target) => (self.position - target).normalized;
}