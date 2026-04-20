using UnityEngine;
using UnityEngine.AI;
using System.Collections; 

public class CowardController : MonoBehaviour
{
    public enum State { Patrol, Flee, Scared }
    public State current = State.Patrol;

    public Transform player;
    public Transform[] points;
    private NavMeshAgent agent;
    private LineOfSight los;
    private Animator anim;
    private int idx;
    private bool isWaiting;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        los = GetComponent<LineOfSight>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        bool canSee = los.CanSeePlayer(transform, player);

        switch (current)
        {
            case State.Patrol:
                anim.SetInteger("State", 0); 
                if (canSee) current = State.Flee;

                if (!agent.hasPath || agent.remainingDistance < 0.5f)
                {
                    idx = (idx + 1) % points.Length;
                    agent.destination = points[idx].position;
                }
                break;

            case State.Flee:
                anim.SetInteger("State", 1); 
                agent.speed = 6f; 

                Vector3 dir = (transform.position - player.position).normalized;
                agent.destination = transform.position + dir * 6f;

                if (!canSee && agent.remainingDistance < 1f)
                {
                    current = State.Scared;
                }
                break;

            case State.Scared:
                if (!isWaiting) StartCoroutine(WaitAndCalmDown());
                break;
        }
    }

    IEnumerator WaitAndCalmDown()
    {
        isWaiting = true;
        agent.isStopped = true;      
        anim.SetInteger("State", 2); 

        yield return new WaitForSeconds(4f); 

        agent.isStopped = false;
        agent.speed = 3.5f;          
        current = State.Patrol;
        isWaiting = false;
    }
}