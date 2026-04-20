using UnityEngine;
using UnityEngine.AI;
using System.Collections; 

public class StalkerController : MonoBehaviour
{
    public Transform player;
    public float attackRange = 2f;
    public float searchTime = 3f;
    public float roamingRadius = 20f; 

    private NavMeshAgent agent;
    private LineOfSight los;
    private Animator anim;

    private bool chasing;
    private bool isSearching;
    private bool isAttacking;
    private Vector3 lastKnownPosition;
    private float searchTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        los = GetComponent<LineOfSight>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (isAttacking) return;


        if (los.CanSeePlayer(transform, player))
        {
            lastKnownPosition = player.position;
            chasing = true;
            isSearching = false;

            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                AttackPlayer();
            }
            else
            {
                agent.destination = player.position; 
            }
        }
        else if (chasing)
        {
            agent.destination = lastKnownPosition; 

            if (agent.remainingDistance < 0.5f)
            {
                chasing = false;
                isSearching = true;
                searchTimer = 0f;
            }
        }
        else if (isSearching)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchTime) isSearching = false;
        }
        else
        {
            PatrolRandomly(); 
        }

        UpdateAnimations();
    }

    Vector3 GetRandomPoint(Vector3 center, float range)
    {
        for (int i = 0; i < 30; i++) 
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return center; 
    }
    void PatrolRandomly()
    {
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            agent.destination = GetRandomPoint(transform.position, roamingRadius);
        }
    }

    void UpdateAnimations()
    {
        if (agent.velocity.magnitude > 0.1f) anim.SetInteger("State", 1);
        else anim.SetInteger("State", 0);
    }

    void AttackPlayer()
    {
        StartCoroutine(ExecuteStalkerAttack());
    }

    IEnumerator ExecuteStalkerAttack()
    {
        isAttacking = true;
        agent.isStopped = true;
        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(1.8f);

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.Die(); 
        }

        yield return new WaitForSeconds(0.7f);

        isAttacking = false;
        if (agent != null) agent.isStopped = false;
    }    void ResetAttack()
    {
        isAttacking = false;
        if (agent != null) agent.isStopped = false;
    }
}