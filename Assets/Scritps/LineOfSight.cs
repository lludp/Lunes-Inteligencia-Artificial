using UnityEngine;

public class LineOfSight : MonoBehaviour
{
    public float distance = 15f;
    public float angle = 140f;
    public LayerMask obstacleMask;

    [Header("Ajustes de Altura")]
    public float eyeHeight = 1.5f; 
    public float targetOffset = 1.0f; 

   
    public void SetEyeHeight(float newHeight)
    {
        eyeHeight = newHeight;
    }

    public bool CanSeePlayer(Transform self, Transform target)
    {
        if (target == null) return false;

        Vector3 origin = self.position + Vector3.up * eyeHeight;
        Vector3 dest = target.position + Vector3.up * targetOffset;

        float dist = Vector3.Distance(origin, dest);
        if (dist > distance) return false;

        Vector3 dirToTarget = (dest - origin).normalized;
        if (Vector3.Angle(self.forward, dirToTarget) > angle / 2f) return false;

        if (Physics.Raycast(origin, dirToTarget, out RaycastHit hit, dist, obstacleMask))
        {
            Debug.DrawRay(origin, dirToTarget * dist, Color.red);
            return false;
        }

        Debug.DrawRay(origin, dirToTarget * dist, Color.green);
        return true;
    }

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;

        Gizmos.color = Color.yellow; 
        Gizmos.DrawWireSphere(origin, 0.2f); 

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(origin, distance);

        Vector3 leftRayDirection = Quaternion.Euler(0, -angle / 2, 0) * transform.forward;
        Vector3 rightRayDirection = Quaternion.Euler(0, angle / 2, 0) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(origin, leftRayDirection * distance);
        Gizmos.DrawRay(origin, rightRayDirection * distance);
    }
}