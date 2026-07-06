using System;
using System.Collections.Generic;

public static class AStarPathfinder
{
    public static List<PathNode> Run(
        PathNode initialNode,
        Func<PathNode, bool> isSatisfied,
        Func<PathNode, List<PathNode>> getConnections,
        Func<PathNode, PathNode, float> getCosts,
        Func<PathNode, float> heuristic,
        int watchDog = 10000)
    {
        var pending = new PathPriorityQueue<PathNode>();
        var visited = new HashSet<PathNode>();
        var parents = new Dictionary<PathNode, PathNode>();
        var costs = new Dictionary<PathNode, float>();

        costs[initialNode] = 0;
        pending.Enqueue(initialNode, 0);
        visited.Add(initialNode);

        int counter = 0;

        while (!pending.IsEmpty)
        {
            if (++counter > watchDog) break;

            PathNode node = pending.Dequeue();

            if (isSatisfied(node))
            {
                var path = new List<PathNode> { node };
                PathNode current = node;
                while (parents.ContainsKey(current))
                {
                    path.Add(parents[current]);
                    current = parents[current];
                }
                path.Reverse();
                return path;
            }

            List<PathNode> children = getConnections(node);
            for (int i = 0; i < children.Count; ++i)
            {
                PathNode child = children[i];
                if (child == null || visited.Contains(child)) continue;

                float newCost = costs[node] + getCosts(node, child);
                if (costs.ContainsKey(child) && newCost > costs[child]) continue;

                costs[child] = newCost;
                pending.Enqueue(child, newCost + heuristic(child));
                visited.Add(child);
                parents[child] = node;
            }
        }

        return new List<PathNode>();
    }
}
