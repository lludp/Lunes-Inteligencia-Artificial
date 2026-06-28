using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class PathfindingGrid : MonoBehaviour
{
    public static PathfindingGrid Instance { get; private set; }

    [Header("Generación de nodos")]
    [Tooltip("Si está activo, siembra nodos sobre TODO el NavMesh horneado (todos los pisos), ignorando Grid World Size.")]
    public bool useNavMeshBounds = true;
    [Tooltip("Margen extra alrededor de los límites del NavMesh.")]
    public float boundsPadding = 2f;
    [Tooltip("Área (X,Z) manual, solo si 'Use Nav Mesh Bounds' está desactivado.")]
    public Vector2 gridWorldSize = new Vector2(40, 40);
    [Tooltip("Separación horizontal entre nodos. Más chico = más nodos.")]
    public float nodeSpacing = 3f;
    [Tooltip("Paso vertical al barrer alturas: permite captar varios pisos en el mismo X,Z (escaleras, planta baja y primer piso).")]
    public float verticalStep = 2f;
    [Tooltip("Tope de seguridad para no generar demasiados nodos.")]
    public int maxNodes = 4000;

    [Header("Conexiones")]
    [Tooltip("Distancia máxima para conectar dos nodos como vecinos.")]
    public float connectionRadius = 5f;
    [Tooltip("No conectar nodos si hay una pared (borde del NavMesh) entre ellos.")]
    public bool blockConnectionsThroughWalls = true;

    private readonly List<PathNode> nodes = new List<PathNode>();
    private List<Vector3> lastPath;

    void Awake()
    {
        Instance = this;
        CollectNodes();
    }

    void CollectNodes()
    {
        nodes.Clear();
        nodes.AddRange(GetComponentsInChildren<PathNode>(true));
    }


    public List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld)
    {
        if (nodes.Count == 0) CollectNodes();

        PathNode start = GetClosestNode(startWorld);
        PathNode goal = GetClosestNode(targetWorld);
        if (start == null || goal == null) return null;

        List<PathNode> nodePath = AStarPathfinder.Run(
            start,
            n => n == goal,
            n => n.neighbours,
            (a, b) => Vector3.Distance(a.worldPosition, b.worldPosition),  
            n => Vector3.Distance(n.worldPosition, goal.worldPosition));   

        if (nodePath.Count == 0) return null;

        var points = new List<Vector3>(nodePath.Count);
        for (int i = 0; i < nodePath.Count; i++)
            points.Add(nodePath[i].worldPosition);

        lastPath = points;
        return points;
    }

    public PathNode GetClosestNode(Vector3 worldPos)
    {
        PathNode closest = null;
        float best = Mathf.Infinity;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;
            float d = (nodes[i].worldPosition - worldPos).sqrMagnitude;
            if (d < best)
            {
                best = d;
                closest = nodes[i];
            }
        }
        return closest;
    }

    public Vector3 GetRandomWalkablePosition()
    {
        if (nodes.Count == 0) CollectNodes();
        if (nodes.Count == 0) return transform.position;
        return nodes[Random.Range(0, nodes.Count)].worldPosition;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 0.5f, gridWorldSize.y));

        if (lastPath != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < lastPath.Count - 1; i++)
                Gizmos.DrawLine(lastPath[i] + Vector3.up * 0.2f, lastPath[i + 1] + Vector3.up * 0.2f);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Nodes")]
    public void GenerateNodes()
    {
        ClearNodes();

        float minX, maxX, minZ, maxZ, minY, maxY;

        if (useNavMeshBounds)
        {
            NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
            if (tri.vertices == null || tri.vertices.Length == 0)
            {
                Debug.LogWarning("[PathfindingGrid] No hay NavMesh horneado. Bakeá el NavMesh (Window > AI > Navigation) o usá Grid World Size manual.");
                return;
            }

            Vector3 v0 = tri.vertices[0];
            minX = maxX = v0.x; minY = maxY = v0.y; minZ = maxZ = v0.z;
            for (int i = 1; i < tri.vertices.Length; i++)
            {
                Vector3 v = tri.vertices[i];
                if (v.x < minX) minX = v.x; else if (v.x > maxX) maxX = v.x;
                if (v.y < minY) minY = v.y; else if (v.y > maxY) maxY = v.y;
                if (v.z < minZ) minZ = v.z; else if (v.z > maxZ) maxZ = v.z;
            }
            minX -= boundsPadding; maxX += boundsPadding;
            minZ -= boundsPadding; maxZ += boundsPadding;
            minY -= verticalStep; maxY += verticalStep;
        }
        else
        {
            minX = transform.position.x - gridWorldSize.x / 2f;
            maxX = transform.position.x + gridWorldSize.x / 2f;
            minZ = transform.position.z - gridWorldSize.y / 2f;
            maxZ = transform.position.z + gridWorldSize.y / 2f;
            minY = transform.position.y - 10f;
            maxY = transform.position.y + 10f;
        }

        
        float dedupeCell = nodeSpacing * 0.6f;
        var dedupe = new Dictionary<Vector3Int, bool>();
        var positions = new List<Vector3>();

        for (float x = minX; x <= maxX; x += nodeSpacing)
        {
            for (float z = minZ; z <= maxZ; z += nodeSpacing)
            {
                for (float y = maxY; y >= minY; y -= verticalStep)
                {
                    if (!NavMesh.SamplePosition(new Vector3(x, y, z), out NavMeshHit hit, verticalStep, NavMesh.AllAreas))
                        continue;

                    Vector3Int key = new Vector3Int(
                        Mathf.RoundToInt(hit.position.x / dedupeCell),
                        Mathf.RoundToInt(hit.position.y / dedupeCell),
                        Mathf.RoundToInt(hit.position.z / dedupeCell));
                    if (dedupe.ContainsKey(key)) continue;

                    dedupe[key] = true;
                    positions.Add(hit.position);

                    if (positions.Count >= maxNodes)
                    {
                        Debug.LogWarning($"[PathfindingGrid] Se alcanzó el tope de {maxNodes} nodos. Subí Node Spacing o bajá maxNodes.");
                        x = maxX; z = maxZ; y = minY; 
                        break;
                    }
                }
            }
        }

        var created = new List<PathNode>(positions.Count);
        for (int i = 0; i < positions.Count; i++)
        {
            var go = new GameObject($"Node_{i}");
            Undo.RegisterCreatedObjectUndo(go, "Generate Nodes");
            go.transform.position = positions[i];
            go.transform.SetParent(transform, true);
            created.Add(go.AddComponent<PathNode>());
        }

        ConnectNodes(created);

        nodes.Clear();
        nodes.AddRange(created);

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log($"[PathfindingGrid] {created.Count} nodos generados sobre el NavMesh (multi-piso).");
    }

    void ConnectNodes(List<PathNode> all)
    {
        float cell = Mathf.Max(0.1f, connectionRadius);
        var buckets = new Dictionary<Vector3Int, List<int>>();

        Vector3Int CellOf(Vector3 p) => new Vector3Int(
            Mathf.FloorToInt(p.x / cell),
            Mathf.FloorToInt(p.y / cell),
            Mathf.FloorToInt(p.z / cell));

        for (int i = 0; i < all.Count; i++)
        {
            all[i].neighbours.Clear();
            Vector3Int c = CellOf(all[i].worldPosition);
            if (!buckets.TryGetValue(c, out var list)) { list = new List<int>(); buckets[c] = list; }
            list.Add(i);
        }

        float sqrRadius = connectionRadius * connectionRadius;

        for (int i = 0; i < all.Count; i++)
        {
            PathNode a = all[i];
            Vector3Int c = CellOf(a.worldPosition);

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        var key = new Vector3Int(c.x + dx, c.y + dy, c.z + dz);
                        if (!buckets.TryGetValue(key, out var list)) continue;

                        for (int k = 0; k < list.Count; k++)
                        {
                            int j = list[k];
                            if (j == i) continue;
                            PathNode b = all[j];

                            if ((a.worldPosition - b.worldPosition).sqrMagnitude > sqrRadius) continue;

                            if (blockConnectionsThroughWalls &&
                                NavMesh.Raycast(a.worldPosition, b.worldPosition, out _, NavMesh.AllAreas))
                                continue;

                            a.neighbours.Add(b);
                        }
                    }
        }
    }

    [ContextMenu("Clear Nodes")]
    public void ClearNodes()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (c.GetComponent<PathNode>() != null)
                DestroyImmediate(c.gameObject);
        }
        nodes.Clear();
    }
#endif
}
