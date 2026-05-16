using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestinationManager : MonoBehaviour
{
    public Transform table; // level surface
    public ObstacleManager obstacleManager;
    public GameObject goalPrefab;

    public int maxTries;
    public float marginRatio;
    public float spacingMultiplier;

    private GameObject goalMarker;
    public Vector3 goalPosition;

    public void CreateRandomGoal(float agentRadius)
    {
        if (table == null || obstacleManager == null)
        {
            Debug.LogError("[GoalManager] Table or ObstacleManager not assigned.");
            return;
        }

        // calculate level size
        Vector3 scale = table.localScale;
        float width = 10f * scale.x;
        float depth = 10f * scale.z;
        Vector2 center = new Vector2(table.position.x, table.position.z);

        Vector2 pos2D = GetValidPosition(width, depth, center, agentRadius);
        if (pos2D == Vector2.zero)
        {
            Debug.LogWarning("[GoalManager] Failed to find valid goal position.");
            return;
        }

        goalPosition = new Vector3(pos2D.x, 0f, pos2D.y);
        if (goalMarker != null) Destroy(goalMarker);

        goalMarker = Instantiate(goalPrefab, goalPosition, Quaternion.identity);
        goalMarker.name = "DestinationMarker";
        Renderer r = goalMarker.GetComponent<Renderer>();
    }

    Vector2 GetValidPosition(float width, float depth, Vector2 center, float radius)
    {
        for (int tries = 0; tries < maxTries; tries++)
        {
            float minX = center.x - width / 2f + width * marginRatio;
            float maxX = center.x + width / 2f - width * marginRatio;
            float minZ = center.y - depth / 2f + width * marginRatio;
            float maxZ = center.y + depth / 2f - width * marginRatio;

            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            Vector2 candidate = new Vector2(x, z);

            bool overlap = false;
            foreach (var obs in obstacleManager.GetPlacedObstacles())
            {
                float minDist = (radius + obs.radius) * spacingMultiplier;
                if (Vector2.Distance(candidate, obs.position) < minDist)
                {
                    overlap = true;
                    break;
                }
            }

            if (!overlap) return candidate;
        }

        return Vector2.zero;
    }

    public GameObject GetCurrentGoal()
    {
        return goalMarker;
    }
}