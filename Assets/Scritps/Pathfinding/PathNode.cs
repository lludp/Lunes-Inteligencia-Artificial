using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nodo de pathfinding como GameObject real en la escena (estilo del profe).
/// Cada nodo es un objeto en la jerarquía con su gizmo y la lista de vecinos a los
/// que está conectado. El A* (<see cref="AStarPathfinder"/>) recorre estos objetos.
/// Se generan automáticamente desde <see cref="PathfindingGrid"/>.
/// </summary>
public class PathNode : MonoBehaviour
{
    [Tooltip("Nodos vecinos navegables desde éste.")]
    public List<PathNode> neighbours = new List<PathNode>();

    public Vector3 worldPosition => transform.position;

    [Header("Gizmo")]
    public float gizmoRadius = 0.3f;

    void OnDrawGizmos()
    {
        Vector3 p = transform.position + Vector3.up * 0.1f;

        // El nodo.
        Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.95f);
        Gizmos.DrawSphere(p, gizmoRadius);

        // Las conexiones a sus vecinos.
        if (neighbours == null) return;
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
        for (int i = 0; i < neighbours.Count; i++)
        {
            if (neighbours[i] != null)
                Gizmos.DrawLine(p, neighbours[i].transform.position + Vector3.up * 0.1f);
        }
    }
}
