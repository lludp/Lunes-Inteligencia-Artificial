using System;
using System.Collections.Generic;
using UnityEngine;

public static class ThetaStarPathfinder
{
    public static List<Vector3> Run(
        PathNode start,
        PathNode goal,
        Func<PathNode, PathNode, bool> hasLineOfSight,
        int watchDog = 10000)
    {
        if (start == null || goal == null) return null;

        var open = new PathPriorityQueue<PathNode>();
        var gScore = new Dictionary<PathNode, float>();
        var parent = new Dictionary<PathNode, PathNode>();
        var closed = new HashSet<PathNode>();

        gScore[start] = 0f;
        parent[start] = start;
        open.Enqueue(start, Heuristic(start, goal));

        int counter = 0;
        while (!open.IsEmpty)
        {
            if (++counter > watchDog) break;

            PathNode s = open.Dequeue();
            if (s == goal) return Reconstruct(parent, goal);

            closed.Add(s);

            List<PathNode> neighbours = s.neighbours;
            for (int i = 0; i < neighbours.Count; i++)
            {
                PathNode n = neighbours[i];
                if (n == null || closed.Contains(n)) continue;
                UpdateVertex(s, n, goal, gScore, parent, open, hasLineOfSight);
            }
        }

        return null;
    }

    static void UpdateVertex(
        PathNode s,
        PathNode n,
        PathNode goal,
        Dictionary<PathNode, float> g,
        Dictionary<PathNode, PathNode> parent,
        PathPriorityQueue<PathNode> open,
        Func<PathNode, PathNode, bool> hasLineOfSight)
    {
        PathNode p = parent[s];

        if (hasLineOfSight != null && hasLineOfSight(p, n))
        {
            float newG = g[p] + Distance(p, n);
            if (!g.ContainsKey(n) || newG < g[n])
            {
                g[n] = newG;
                parent[n] = p;
                open.Enqueue(n, newG + Heuristic(n, goal));
            }
        }
        else
        {
            float newG = g[s] + Distance(s, n);
            if (!g.ContainsKey(n) || newG < g[n])
            {
                g[n] = newG;
                parent[n] = s;
                open.Enqueue(n, newG + Heuristic(n, goal));
            }
        }
    }

    static List<Vector3> Reconstruct(Dictionary<PathNode, PathNode> parent, PathNode goal)
    {
        var path = new List<Vector3>();
        PathNode current = goal;
        while (true)
        {
            path.Add(current.worldPosition);
            PathNode p = parent[current];
            if (p == current) break;
            current = p;
        }
        path.Reverse();
        return path;
    }

    static float Distance(PathNode a, PathNode b) =>
        Vector3.Distance(a.worldPosition, b.worldPosition);

    static float Heuristic(PathNode a, PathNode goal) =>
        Vector3.Distance(a.worldPosition, goal.worldPosition);
}
