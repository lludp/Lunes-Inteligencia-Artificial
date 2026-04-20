using UnityEngine;
using UnityEngine.AI;

public class GuardController : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent agent;
    private LineOfSight los;

    void Awake() { agent = GetComponent<NavMeshAgent>(); los = GetComponent<LineOfSight>(); }

    void Update()
    {
        
        if (los.CanSeePlayer(transform, player))
        {
            if (Vector3.Distance(transform.position, player.position) < 3f)
            {
                agent.isStopped = true; 
                transform.LookAt(player);
            }
            else
            {
                agent.isStopped = false;
                agent.destination = player.position; 
            }
        }
        else
        {
            agent.isStopped = true; 
        }
    }
}