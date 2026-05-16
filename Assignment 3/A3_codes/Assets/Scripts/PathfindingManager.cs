using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathfindingManager : MonoBehaviour
{
    public ObstacleManager obstacleManager;
    public AgentManager agentManager;
    public DestinationManager destinationManager;
    public ObstacleVertexDetector vertexDetector;
    public TerrainManager terrainManager;
    public RVGDrawer drawer;

    private Transform startPoint;
    private Transform goalPoint;

    private float agentRadius;

    public enum RVGMode { Naive, Improved }
    public RVGMode mode;

    public bool visualizeInGameMode;
    private List<Vector3> finalPath = new();
    private List<Vector3> nodes = new(); // all convex nodes + start + goal
    private List<(Vector3, Vector3)> rawEdges = new(); // all possible edges

    private List<Vector3> naivePath = new();
    private List<Vector3> improvedPath = new();

    [SerializeField] private LayerMask obstacleLayer;

    void Start()
    {
        StartCoroutine(InitializeAndBuild());
    }

    private IEnumerator InitializeAndBuild()
    {
        // wait for AgentManager and DestinationManager to finish spawning
        yield return new WaitForSeconds(0.5f);
        FindDynamicReferences();
        BuildRVG();
    }

    // find start(agent) and destination points
    void FindDynamicReferences()
    {
        GameObject agent = agentManager.GetCurrentAgent();
        startPoint = agent.transform;

        GameObject goal = destinationManager.GetCurrentGoal();
        goalPoint = goal.transform;

        agentRadius = agentManager.GetAgentRadius();
    }

    void BuildRVG()
    {
        List<Vector3[]> obstacleReflexes = vertexDetector.GetAllObstacleVertices();
        nodes.Clear();
        rawEdges.Clear();
        finalPath.Clear();

        float vertexYOffset = 1f;
        foreach (var reflex in obstacleReflexes)
        {
            for (int i = 0; i < reflex.Length; i++)
                reflex[i].y += vertexYOffset;
            nodes.AddRange(reflex);
        }

        // add start and destination
        Vector3 startPos = startPoint.position + Vector3.up * vertexYOffset;
        Vector3 goalPos = goalPoint.position + Vector3.up * vertexYOffset;
        nodes.Add(startPos);
        nodes.Add(goalPos);

        // connect reflexes
        AddBoundaryEdges(obstacleReflexes);
        AddBitangentEdges(nodes, obstacleReflexes);

        // build visibility graph
        var graph = BuildGraph();

        naivePath = Dijsktra(startPos, goalPos, graph);
        improvedPath = AStar(startPos, goalPos, graph);

        if (mode == RVGMode.Naive)
            finalPath = Dijsktra(startPos, goalPos, graph);
        else
            finalPath = AStar(startPos, goalPos, graph);

        OutputPathStats();

        if (visualizeInGameMode)
            Visualize();
    }

    void OutputPathStats()
    {
        if (naivePath.Count > 1 && improvedPath.Count > 1)
        {
            float naiveLength = 0f, improvedLength = 0f;
            float naiveCost = 0f, improvedCost = 0f;

            // naive path
            for (int i = 0; i < naivePath.Count - 1; i++)
            {
                float segLength = Vector3.Distance(naivePath[i], naivePath[i + 1]);
                float segCost = (terrainManager.GetCostAtPosition(naivePath[i]) +
                                terrainManager.GetCostAtPosition(naivePath[i + 1])) / 2f;
                naiveLength += segLength;
                naiveCost += segLength * segCost;
            }

            // improved path
            for (int i = 0; i < improvedPath.Count - 1; i++)
            {
                float segLength = Vector3.Distance(improvedPath[i], improvedPath[i + 1]);
                float segCost = (terrainManager.GetCostAtPosition(improvedPath[i]) +
                                terrainManager.GetCostAtPosition(improvedPath[i + 1])) / 2f;
                improvedLength += segLength;
                improvedCost += segLength * segCost;
            }

            Debug.Log($"[RVG] Naive path → Nodes: {naivePath.Count}, Length: {naiveLength:F2}, TotalCost: {naiveCost:F2}");
            Debug.Log($"[RVG] Improved path → Nodes: {improvedPath.Count}, Length: {improvedLength:F2}, TotalCost: {improvedCost:F2}");
        }
    }

    void Visualize()
    {
        RVGDrawer drawer = FindAnyObjectByType<RVGDrawer>();
        if (drawer != null)
        {
            // all RVG edges (yellow)
            foreach (var e in rawEdges)
                drawer.DrawPath(new List<Vector3> { e.Item1, e.Item2 }, Color.yellow, 0.8f, 2f);

            // final path (red)
            if (finalPath.Count > 1)
                drawer.DrawPath(finalPath, Color.red, 1.2f, 3f);

            // naive path(blue)
            if (naivePath.Count > 1)
                drawer.DrawPath(naivePath, Color.blue, 0.8f, 2.5f);

            // improved path(green)
            if (improvedPath.Count > 1)
                drawer.DrawPath(improvedPath, Color.green, 0.8f, 2.5f);
        }
    }

    void AddBoundaryEdges(List<Vector3[]> reflexes)
    {
        foreach (var reflex in reflexes)
        {
            int n = reflex.Length;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = reflex[i];
                Vector3 b = reflex[(i + 1) % n];
                rawEdges.Add((a, b));
            }
        }
    }

    void AddBitangentEdges(List<Vector3> nodes, List<Vector3[]> reflexes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                Vector3 p1 = nodes[i];
                Vector3 p2 = nodes[j];

                if (BelongsToSameObstacle(p1, p2, reflexes))
                    continue;
                if (IsBitangent(p1, p2))
                    rawEdges.Add((p1, p2));
            }
        }
    }



    bool BelongsToSameObstacle(Vector3 p1, Vector3 p2, List<Vector3[]> reflexes)
    {
        foreach (var reflex in reflexes)
        {
            int found = 0;
            foreach (var v in reflex)
            {
                if (Vector3.Distance(v, p1) < 0.01f || Vector3.Distance(v, p2) < 0.01f)
                    found++;
            }
            if (found == 2)
                return true;
        }
        return false;
    }

    // check if obstacle collide with line (bitangent)
    bool IsBitangent(Vector3 p1, Vector3 p2)
    {
        Vector3 dir = (p2 - p1).normalized;
        float dist = Vector3.Distance(p1, p2);

        Vector3 start = p1 - dir * 0.5f;

        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized * agentRadius;
        Vector3 left = -right;

        Vector3[] starts = new Vector3[]{ start, start + right, start + left };

        foreach (var s in starts)
        {
            if (Physics.Raycast(
                s, dir, out RaycastHit hit, dist + 0.5f,
                obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                Debug.DrawLine(s, hit.point, Color.red, 0, false);
                return false;
            }
        }
        return true;
    }


    // build adjacency graph from rawEdges
    Dictionary<Vector3, List<(Vector3 neighbor, float cost)>> BuildGraph()
    {
        Dictionary<Vector3, List<(Vector3 neighbor, float cost)>> graph = new();

        foreach (var edge in rawEdges)
        {
            float cost = Vector3.Distance(edge.Item1, edge.Item2);
            if (!graph.ContainsKey(edge.Item1))
                graph[edge.Item1] = new List<(Vector3, float)>();
            if (!graph.ContainsKey(edge.Item2))
                graph[edge.Item2] = new List<(Vector3, float)>();

            graph[edge.Item1].Add((edge.Item2, cost));
            graph[edge.Item2].Add((edge.Item1, cost)); // undirected
        }

        return graph;
    }

    List<Vector3> Dijsktra(Vector3 s, Vector3 goal, Dictionary<Vector3, List<(Vector3 neighbor, float cost)>> graph)
    {
        var g = new Dictionary<Vector3, float>();
        var f = new Dictionary<Vector3, float>();
        var came = new Dictionary<Vector3, Vector3>();
        var fringe = new List<Vector3>();

        foreach (var v in graph.Keys)
        {
            g[v] = Mathf.Infinity;
            f[v] = Mathf.Infinity;
        }

        g[s] = 0;
        f[s] = 0;
        fringe.Add(s);

        while (fringe.Count > 0)
        {
            // extract node with minimum f
            Vector3 c = fringe[0];
            foreach (var v in fringe)
                if (f[v] < f[c]) c = v;

            if (Vector3.Distance(c, goal) < 0.1f)
                return ReconstructPath(came, c);

            fringe.Remove(c);

            foreach (var (n, w) in graph[c])
            {
                float terrainCost = (terrainManager.GetCostAtPosition(c) + terrainManager.GetCostAtPosition(n)) / 2f;
                float d = g[c] + w;
                if (d < g[n])
                {
                    if (!fringe.Contains(n))
                        fringe.Add(n);

                    g[n] = d;
                    f[n] = g[n];
                    came[n] = c;
                }
            }
        }

        return new List<Vector3>();
    }

    List<Vector3> AStar(Vector3 s, Vector3 goal, Dictionary<Vector3, List<(Vector3 neighbor, float cost)>> graph)
    {
        var g = new Dictionary<Vector3, float>();
        var f = new Dictionary<Vector3, float>();
        var came = new Dictionary<Vector3, Vector3>();
        var fringe = new List<Vector3>();

        foreach (var v in graph.Keys)
        {
            g[v] = Mathf.Infinity;
            f[v] = Mathf.Infinity;
        }

        g[s] = 0;
        f[s] = Heuristic(s, goal);
        fringe.Add(s);

        while (fringe.Count > 0)
        {
            // extract node with minimum f
            Vector3 c = fringe[0];
            foreach (var v in fringe)
                if (f[v] < f[c]) c = v;

            if (Vector3.Distance(c, goal) < 0.1f)
                return ReconstructPath(came, c);

            fringe.Remove(c);

            foreach (var (n, w) in graph[c])
            {
                float terrainCost = (terrainManager.GetCostAtPosition(c) + terrainManager.GetCostAtPosition(n)) / 2f;
                float d = g[c] + w * terrainCost;

                if (d < g[n])
                {
                    if (!fringe.Contains(n))
                        fringe.Add(n);

                    g[n] = d;
                    f[n] = g[n] + Heuristic(n, goal);
                    came[n] = c;
                }
            }
        }
        return new List<Vector3>();
    }

    List<Vector3> ReconstructPath(Dictionary<Vector3, Vector3> cameFrom, Vector3 current)
    {
        List<Vector3> path = new() { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        return path;
    }

    float Heuristic(Vector3 n, Vector3 goal)
    {
        float baseDist = Vector3.Distance(n, goal);
        float costN = terrainManager.GetCostAtPosition(n);
        float costGoal = terrainManager.GetCostAtPosition(goal);
        float minCost = terrainManager.GetMinCost();

        float avgCost = Mathf.Max(minCost, (costN + costGoal) * 0.5f);
        return baseDist * avgCost * 1.5f;
    }

    public List<Vector3> GetFinalPath()
    {
        return finalPath;
    }


#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // all nodes (yellow)
        Gizmos.color = Color.yellow;
        foreach (var node in nodes)
            Gizmos.DrawSphere(node, 1f);

        // raw edges (yellow)
        Gizmos.color = Color.yellow;
        foreach (var e in rawEdges)
            Gizmos.DrawLine(e.Item1, e.Item2);

        // naive path(blue)
        if (naivePath.Count > 1)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < naivePath.Count - 1; i++)
                Gizmos.DrawLine(naivePath[i], naivePath[i + 1]);
        }

        // improved path(green)
        if (improvedPath.Count > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < improvedPath.Count - 1; i++)
                Gizmos.DrawLine(improvedPath[i], improvedPath[i + 1]);
        }

        // final path(red)
        if (finalPath.Count > 1)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < finalPath.Count - 1; i++)
                Gizmos.DrawLine(finalPath[i], finalPath[i + 1]);
        }
    }
#endif
}