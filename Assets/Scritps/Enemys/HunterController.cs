using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HunterController : MonoBehaviour
{
    public enum Input { SawPlayer, LostPlayer, EnterAttack, AttackDone }

    [Header("Referencias")]
    public Transform player;

    [Header("Velocidades")]
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 6f;
    public float rotationSpeed = 8f;

    [Header("Percepción / combate")]
    public float attackRange = 2f;
    public float attackDamageDelay = 1.2f;
    public float attackRecover = 0.8f;

    [Header("Navegación A*")]
    public float repathInterval = 0.4f;
    public float waypointTolerance = 1.0f;
    public float arriveRadius = 2f;

    NavMeshAgent agent;
    LineOfSight los;
    Animator anim;

    FSM<Input> fsm;

    bool canSee;
    float distToPlayer;

    List<Vector3> path;
    int pathIndex;
    float repathTimer;
    float commandedSpeed;
    bool warnedNoGrid;

    bool attackFinished;

    bool hasStateParam;
    bool hasAttackParam;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        los = GetComponent<LineOfSight>();
        anim = GetComponent<Animator>();

        agent.updateRotation = false;
        agent.autoBraking = false;

        CacheAnimParams();
        BuildFSM();
    }

    void BuildFSM()
    {
        var patrol = new PatrolState(this);
        var chase = new ChaseState(this);
        var attack = new AttackState(this);

        patrol.AddTransition(Input.SawPlayer, chase);

        chase.AddTransition(Input.LostPlayer, patrol);
        chase.AddTransition(Input.EnterAttack, attack);

        attack.AddTransition(Input.AttackDone, chase);
        attack.AddTransition(Input.LostPlayer, patrol);

        fsm = new FSM<Input>(patrol);
    }

    void Update()
    {
        if (player == null) return;

        canSee = los != null && los.CanSeePlayer(transform, player);
        distToPlayer = Vector3.Distance(transform.position, player.position);
        commandedSpeed = 0f;

        fsm.OnUpdate();
        UpdateAnim();
    }

    void DoPatrol()
    {
        if (path == null || pathIndex >= path.Count)
            RequestPath(GetRandomPatrolTarget());
        FollowPath(patrolSpeed);
    }

    void DoChase()
    {
        repathTimer -= Time.deltaTime;
        if (path == null || repathTimer <= 0f)
        {
            RequestPath(player.position);
            repathTimer = repathInterval;
        }
        FollowPath(chaseSpeed);
    }

    void BeginAttack()
    {
        attackFinished = false;
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        commandedSpeed = 0f;
        if (hasStateParam) anim.SetInteger("State", 0);
        if (hasAttackParam) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackDamageDelay);

        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange + 0.5f)
        {
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null) stats.Die();
        }

        yield return new WaitForSeconds(attackRecover);
        attackFinished = true;
    }

    void RequestPath(Vector3 target)
    {
        if (PathfindingGrid.Instance == null)
        {
            if (!warnedNoGrid)
            {
                Debug.LogWarning("[Hunter] No hay PathfindingGrid en la escena.");
                warnedNoGrid = true;
            }
            return;
        }

        List<Vector3> newPath = PathfindingGrid.Instance.FindPath(transform.position, target);
        if (newPath != null && newPath.Count > 0)
        {
            path = newPath;
            pathIndex = path.Count > 1 ? 1 : 0;
        }
    }

    bool FollowPath(float speed)
    {
        if (path == null || pathIndex >= path.Count) return true;

        Vector3 target = path[pathIndex];
        bool isLast = pathIndex == path.Count - 1;

        Vector3 dir;
        float speedFactor = 1f;
        if (isLast)
            dir = SteeringBehaviours.Arrive(transform.position, target, arriveRadius, out speedFactor);
        else
            dir = SteeringBehaviours.Seek(transform.position, target);

        MoveWithSteering(dir, speed * speedFactor);

        Vector3 flat = target - transform.position;
        flat.y = 0f;
        if (flat.magnitude <= waypointTolerance)
            pathIndex++;

        return pathIndex >= path.Count;
    }

    void MoveWithSteering(Vector3 dir, float speed)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        agent.Move(dir * speed * Time.deltaTime);

        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSpeed * Time.deltaTime);

        commandedSpeed = speed;
    }

    Vector3 GetRandomPatrolTarget()
    {
        if (PathfindingGrid.Instance != null)
            return PathfindingGrid.Instance.GetRandomWalkablePosition();
        return transform.position;
    }

    void CacheAnimParams()
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Int && p.name == "State") hasStateParam = true;
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == "Attack") hasAttackParam = true;
        }
    }

    void UpdateAnim()
    {
        if (!hasStateParam) return;
        anim.SetInteger("State", commandedSpeed > 0.1f ? 1 : 0);
    }

    class PatrolState : State<Input>
    {
        readonly HunterController o;
        public PatrolState(HunterController owner) { o = owner; }

        public override void Execute()
        {
            if (o.canSee) { _fsm.Transition(Input.SawPlayer); return; }
            o.DoPatrol();
        }
    }

    class ChaseState : State<Input>
    {
        readonly HunterController o;
        public ChaseState(HunterController owner) { o = owner; }

        public override void Enter() { o.repathTimer = 0f; }

        public override void Execute()
        {
            if (!o.canSee) { _fsm.Transition(Input.LostPlayer); return; }
            if (o.distToPlayer <= o.attackRange) { _fsm.Transition(Input.EnterAttack); return; }
            o.DoChase();
        }
    }

    class AttackState : State<Input>
    {
        readonly HunterController o;
        public AttackState(HunterController owner) { o = owner; }

        public override void Enter() { o.BeginAttack(); }

        public override void Execute()
        {
            if (!o.attackFinished) return;
            if (o.canSee) _fsm.Transition(Input.AttackDone);
            else _fsm.Transition(Input.LostPlayer);
        }
    }
}
