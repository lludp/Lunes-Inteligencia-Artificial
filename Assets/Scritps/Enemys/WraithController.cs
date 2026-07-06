using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WraithController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Velocidades")]
    public float patrolSpeed = 3f;
    public float chaseSpeed = 4.5f;
    public float fleeSpeed = 6.5f;
    public float rotationSpeed = 8f;

    [Header("Percepción")]
    public float dangerRange = 6f;
    public float fleeDistance = 12f;

    [Header("Navegación Theta*")]
    public float repathInterval = 0.4f;
    public float waypointTolerance = 1.0f;
    public float arriveRadius = 2f;

    NavMeshAgent agent;
    LineOfSight los;
    Animator anim;

    ITreeNode root;

    bool canSee;
    float distToPlayer;

    List<Vector3> path;
    int pathIndex;
    float repathTimer;
    float commandedSpeed;
    int animState;
    bool warnedNoGrid;

    bool hasStateParam;

    System.Func<PathNode, PathNode, bool> losBetween;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        los = GetComponent<LineOfSight>();
        anim = GetComponent<Animator>();

        agent.updateRotation = false;
        agent.autoBraking = false;

        losBetween = AreNeighbours;

        CacheAnimParams();
        BuildTree();
    }

    void BuildTree()
    {
        ITreeNode patrol = new ActionNode(DoPatrol);
        ITreeNode chase = new ActionNode(DoChase);
        ITreeNode flee = new ActionNode(DoFlee);

        ITreeNode qTooClose = new QuestionNode(() => distToPlayer <= dangerRange, flee, chase);
        ITreeNode qSeePlayer = new QuestionNode(() => canSee, qTooClose, patrol);

        root = qSeePlayer;
    }

    void Update()
    {
        if (player == null) return;

        canSee = los != null && los.CanSeePlayer(transform, player);
        distToPlayer = Vector3.Distance(transform.position, player.position);
        commandedSpeed = 0f;
        animState = 0;

        root.Execute();
        UpdateAnim();
    }

    void DoPatrol()
    {
        if (path == null || pathIndex >= path.Count)
            RequestThetaPath(GetRandomPatrolTarget());
        animState = 0;
        FollowPath(patrolSpeed);
    }

    void DoChase()
    {
        repathTimer -= Time.deltaTime;
        if (path == null || repathTimer <= 0f)
        {
            RequestThetaPath(player.position);
            repathTimer = repathInterval;
        }
        animState = 0;
        FollowPath(chaseSpeed);
    }

    void DoFlee()
    {
        repathTimer -= Time.deltaTime;
        if (path == null || repathTimer <= 0f)
        {
            Vector3 away = (transform.position - player.position);
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            Vector3 target = transform.position + away.normalized * fleeDistance;
            RequestThetaPath(target);
            repathTimer = repathInterval;
        }
        animState = 1;
        FollowPath(fleeSpeed);
    }

    void RequestThetaPath(Vector3 target)
    {
        PathfindingGrid grid = PathfindingGrid.Instance;
        if (grid == null)
        {
            if (!warnedNoGrid)
            {
                Debug.LogWarning("[Wraith] No hay PathfindingGrid en la escena.");
                warnedNoGrid = true;
            }
            return;
        }

        PathNode start = grid.GetClosestNode(transform.position);
        PathNode goal = grid.GetClosestNode(target);
        if (start == null || goal == null) return;

        List<Vector3> newPath = ThetaStarPathfinder.Run(start, goal, losBetween);
        if (newPath != null && newPath.Count > 0)
        {
            path = newPath;
            pathIndex = path.Count > 1 ? 1 : 0;
        }
    }

    static bool AreNeighbours(PathNode a, PathNode b)
    {
        if (a == null || b == null) return false;
        List<PathNode> list = a.neighbours;
        for (int i = 0; i < list.Count; i++)
            if (list[i] == b) return true;
        return false;
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
        }
    }

    void UpdateAnim()
    {
        if (!hasStateParam) return;
        int state = commandedSpeed > 0.1f ? animState : 0;
        anim.SetInteger("State", state);
    }
}
