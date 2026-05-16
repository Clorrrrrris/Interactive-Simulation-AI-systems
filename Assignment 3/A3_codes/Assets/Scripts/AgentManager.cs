using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    public Transform table;  // level surface
    public ObstacleManager obstacleManager;
    public DestinationManager destinationManager;

    public GameObject agentSmall;
    public GameObject agentMedium;
    public GameObject agentLarge;

    public enum AgentSize { Small, Medium, Large }
    public AgentSize selectedSize;
    private GameObject currentAgent;
    public int maxTries;

    void Start()
    {
        StartCoroutine(SpawnAgentDelayed());
    }

    private IEnumerator SpawnAgentDelayed()
    {
        yield return null;  // wait obstacle created
        SpawnAgent();
    }

    void SpawnAgent()
    {
        if (currentAgent != null)
            Destroy(currentAgent);
        
        GameObject prefab = agentSmall;
        float yHeight = 0f;
        //float yHeight = 4f;
        if (selectedSize == AgentSize.Medium)
        {
            prefab = agentMedium;
            //yHeight = 5f;
        }
        else if (selectedSize == AgentSize.Large)
        {
            prefab = agentLarge;
            //yHeight = 6f;
        }
        float radius = GetBoundingRadius(prefab);

        // spawn agent
        Vector2 pos2D = GetValidPosition(radius);
        if (pos2D == Vector2.zero)
        {
            Debug.LogWarning("[AgentManager] Failed to find valid spawn position after max tries.");
            return;
        }
        Vector3 pos = new Vector3(pos2D.x, yHeight, pos2D.y);
        currentAgent = Instantiate(prefab, pos, Quaternion.identity);
        currentAgent.name = $"Agent_{selectedSize}";
        currentAgent.transform.parent = transform;

        // create start marker
        GameObject startMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        startMarker.transform.position = pos + Vector3.up * 2f;
        startMarker.transform.localScale = new Vector3(5f, 2f, 5f);
        startMarker.GetComponent<Renderer>().material.color = Color.cyan;
        startMarker.name = "StartMarker";
        Destroy(startMarker.GetComponent<Collider>());

        //Debug.Log($"Spawned {selectedSize} agent at {pos}, radius={radius:F2}");

        destinationManager.CreateRandomGoal(radius);

        AgentMover mover = currentAgent.GetComponent<AgentMover>();
        mover.table = table;
        mover.goalManager = destinationManager;
        mover.terrainManager = FindAnyObjectByType<TerrainManager>();
        mover.pathManager = FindAnyObjectByType<PathfindingManager>();
        mover.moveSpeed = 1f;  // initial speed
    }

    Vector2 GetValidPosition(float radius)
    {
        Vector3 scale = table.localScale;
        float width = 10f * scale.x;
        float depth = 10f * scale.z;
        Vector2 center = new Vector2(table.position.x, table.position.z);

        for (int tries = 0; tries < maxTries; tries++)
        {
            float minX = center.x - width / 2f + radius;
            float maxX = center.x + width / 2f - radius;
            float minZ = center.y - depth / 2f + radius;
            float maxZ = center.y + depth / 2f - radius;

            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            Vector2 candidate = new Vector2(x, z);

            bool overlap = IsOverlappingObstacle(candidate, radius);
            if (!overlap)
            {
                //Debug.Log($"Found valid position at {candidate} after {tries} tries.");
                return candidate;
            }
            else
            {
                //Debug.Log($"Overlap detected, retrying. ({tries})");
            }
        }

        return Vector2.zero;
    }

    bool IsOverlappingObstacle(Vector2 pos, float radius)
    {
        List<ObstacleManager.ObstacleData> obsList = obstacleManager.GetPlacedObstacles();
        foreach (var obs in obsList)
        {
            float minDist = (radius + obs.radius) * 1.2f; 
            float dist = Vector2.Distance(pos, obs.position);
            //Debug.Log($"Check: dist={dist:F2}, minDist={minDist:F2}, agentR={radius:F2}, obsR={obs.radius:F2}");
            if (dist < minDist)
                return true;
        }
        return false;
    }

    float GetBoundingRadius(GameObject prefab)
    {
        Renderer r = prefab.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            Vector3 size = r.bounds.size;
            return Mathf.Sqrt(size.x * size.x + size.z * size.z) * 0.6f;
        }
        //Debug.LogWarning($"[AgentManager] No Renderer found on {prefab.name}");
        return 5f;
    }

    public float GetAgentRadius()
    {
        if (agentSmall != null)
            return GetBoundingRadius(agentSmall);
        return 5f;
    }

    public GameObject GetCurrentAgent()
    {
        return currentAgent;
    }

/*
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (currentAgent != null)
        {
            Gizmos.color = Color.green;
            float r = GetBoundingRadius(currentAgent);
            Vector3 p = currentAgent.transform.position;
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.DrawWireDisc(p, Vector3.up, r); // agent radius

            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireDisc(p, Vector3.up, r * 1.2f); // minDist
        }
    }
#endif
*/
}