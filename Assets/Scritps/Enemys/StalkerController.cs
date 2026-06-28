using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Enemigo Stalker. Decisiones por FSM + Line of Sight, navegación por A* (grilla
/// propia, no NavMesh) y movimiento por steering behaviors.
///
///  - DECISIÓN: FSM (Patrol / Chase / Search / Attack) disparada por la LoS.
///  - PATHFINDING: A* sobre <see cref="PathfindingGrid"/> para rodear obstáculos
///    hacia el jugador o su última posición conocida.
///  - STEERING: Seek/Arrive para seguir los waypoints del camino y Pursue para
///    perseguir al jugador en línea directa cuando está cerca y a la vista.
///
/// El NavMeshAgent se usa SOLO como "mover" (agent.Move) para mantener al agente
/// pegado al piso navegable; la RUTA la decide el A* propio.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class StalkerController : MonoBehaviour
{
    public enum State { Patrol, Chase, Search, Attack }

    [Header("Referencias")]
    public Transform player;

    [Header("Velocidades")]
    public float moveSpeed = 4f;
    public float chaseSpeed = 6f;
    public float rotationSpeed = 8f;

    [Header("Combate / percepción")]
    public float attackRange = 2f;
    public float searchTime = 3f;
    [Tooltip("Si el jugador está a la vista y dentro de este rango, se persigue directo con Pursue (sin nodos).")]
    public float directChaseRange = 8f;

    [Header("Navegación (A*)")]
    [Tooltip("Cada cuánto recalcular el camino A* mientras persigue de lejos.")]
    public float repathInterval = 0.4f;
    public float waypointTolerance = 1.0f;
    public float arriveRadius = 2f;
    public float pursuePrediction = 0.5f;

    private NavMeshAgent agent;
    private LineOfSight los;
    private Animator anim;

    private State state = State.Patrol;
    private List<Vector3> path;
    private int pathIndex;
    private float repathTimer;

    private Vector3 lastKnownPosition;
    private float searchTimer;

    private Vector3 prevPlayerPos;
    private float commandedSpeed; // velocidad que comandamos este frame (para animaciones)
    private bool isAttacking;
    private bool warnedNoGrid;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        los = GetComponent<LineOfSight>();
        anim = GetComponent<Animator>();

        // Usamos el agente solo como mover: nosotros decidimos ruta y rotación.
        agent.updateRotation = false;
        agent.autoBraking = false;
    }

    void Start()
    {
        if (player != null) prevPlayerPos = player.position;
    }

    void Update()
    {
        if (isAttacking) return;
        if (player == null) return;

        float dt = Time.deltaTime;
        Vector3 playerVel = dt > 0f ? (player.position - prevPlayerPos) / dt : Vector3.zero;
        prevPlayerPos = player.position;

        bool canSee = los.CanSeePlayer(transform, player);
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        commandedSpeed = 0f;

        switch (state)
        {
            case State.Patrol:
                if (canSee)
                {
                    lastKnownPosition = player.position;
                    RequestPath(lastKnownPosition);
                    state = State.Chase;
                    break;
                }
                if (path == null || pathIndex >= path.Count)
                    RequestPath(GetRandomPatrolTarget());
                FollowPath(moveSpeed);
                break;

            case State.Chase:
                if (canSee)
                {
                    lastKnownPosition = player.position;

                    if (distToPlayer <= attackRange)
                    {
                        AttackPlayer();
                        break;
                    }

                    if (distToPlayer <= directChaseRange)
                    {
                        // Persecución directa con steering (Pursue) prediciendo al jugador.
                        Vector3 dir = SteeringBehaviours.Pursue(transform.position, player.position, playerVel, pursuePrediction);
                        MoveWithSteering(dir, chaseSpeed);
                        path = null; // forzamos recalcular A* si vuelve a alejarse
                    }
                    else
                    {
                        repathTimer -= dt;
                        if (path == null || repathTimer <= 0f)
                        {
                            RequestPath(player.position);
                            repathTimer = repathInterval;
                        }
                        FollowPath(chaseSpeed);
                    }
                }
                else
                {
                    // Perdió de vista: va por A* a la última posición conocida.
                    RequestPath(lastKnownPosition);
                    searchTimer = 0f;
                    state = State.Search;
                }
                break;

            case State.Search:
                if (canSee)
                {
                    lastKnownPosition = player.position;
                    RequestPath(lastKnownPosition);
                    state = State.Chase;
                    break;
                }

                bool arrived = FollowPath(moveSpeed);
                if (arrived)
                {
                    // Llegó al último punto conocido: mira alrededor un rato.
                    transform.Rotate(0f, 120f * dt, 0f);
                    searchTimer += dt;
                    if (searchTimer >= searchTime)
                    {
                        path = null;
                        state = State.Patrol;
                    }
                }
                break;
        }

        UpdateAnimations();
    }

    // ----------------- Navegación A* + steering -----------------

    void RequestPath(Vector3 target)
    {
        if (PathfindingGrid.Instance == null)
        {
            if (!warnedNoGrid)
            {
                Debug.LogWarning("[Stalker] No hay un PathfindingGrid en la escena. Agregá el componente PathfindingGrid a un GameObject centrado en el mapa.");
                warnedNoGrid = true;
            }
            return;
        }

        List<Vector3> newPath = PathfindingGrid.Instance.FindPath(transform.position, target);
        if (newPath != null && newPath.Count > 0)
        {
            path = newPath;
            // El primer nodo es nuestra propia celda: arrancamos en el siguiente.
            pathIndex = path.Count > 1 ? 1 : 0;
        }
    }

    /// <summary>Sigue el camino con Seek (tramos intermedios) y Arrive (último nodo). Devuelve true al terminar.</summary>
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

        Vector3 velocity = dir * speed;
        agent.Move(velocity * Time.deltaTime); // mueve sobre el NavMesh sin hacer pathfinding propio

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

    // ----------------- Animación y ataque -----------------

    void UpdateAnimations()
    {
        if (commandedSpeed > 0.1f) anim.SetInteger("State", 1);
        else anim.SetInteger("State", 0);
    }

    void AttackPlayer()
    {
        StartCoroutine(ExecuteStalkerAttack());
    }

    IEnumerator ExecuteStalkerAttack()
    {
        isAttacking = true;
        state = State.Attack;
        commandedSpeed = 0f;
        anim.SetInteger("State", 0);
        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(1.8f);

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats != null) stats.Die();

        yield return new WaitForSeconds(0.7f);

        isAttacking = false;
        state = State.Chase; // tras atacar, vuelve a evaluar al jugador
    }

    void ResetAttack()
    {
        isAttacking = false;
    }
}
