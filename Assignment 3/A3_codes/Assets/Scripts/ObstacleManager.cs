using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    // prefabs
    public GameObject tShapePrefab;
    public GameObject uShapePrefab;
    public Transform table; // level surface

    public int totalObstacleCount; // initialized in Start()
    public int maxTries; // avoid stuck
    public float spacingMultiplier; 
    public float xMarginRatio; 
    public float zMarginRatio; 
    public float largestAgentRadius; // ensure largest agent can pass

    public class ObstacleData
    {
        public Vector2 position;
        public float radius;
        public bool isT;
    }

    public readonly List<ObstacleData> placedObstacles = new List<ObstacleData>();


    void Start()
    {
        if (totalObstacleCount <= 0)
            totalObstacleCount = Random.Range(8, 13); // 8-12 random obstacles
        GenerateObstacles();
    }

    void GenerateObstacles()
    {
        Vector3 scale = table.localScale;
        float width = 10f * scale.x;
        float depth = 10f * scale.z;
        Vector2 center = new Vector2(table.position.x, table.position.z);

        for (int i = 0; i < totalObstacleCount; i++)
        {
            bool isT = Random.value > 0.5f; // 50% T/U shape
            GameObject prefab = isT ? tShapePrefab : uShapePrefab;
            float radius = GetBoundingRadius(prefab);

            Vector2 pos = GetValidPosition(width, depth, center, radius, spacingMultiplier);
            if (pos == Vector2.zero) continue;

            float angle = Random.Range(0f, 360f);
            GameObject obstacle = Instantiate(prefab, new Vector3(pos.x, 5f, pos.y), Quaternion.Euler(0f, angle, 0f));
            obstacle.name = isT ? "TObstacle" : "UObstacle";
            obstacle.transform.parent = transform;

            placedObstacles.Add(new ObstacleData { position = pos, radius = radius });
        }
        //Debug.Log($"[ObstacleManager] Generated {placedObstacles.Count} obstacles.");
    }


    Vector2 GetValidPosition(float width, float depth, Vector2 center, float thisRadius, float spacingMultiplier)
    {
        for (int tries = 0; tries < maxTries; tries++)
        {
            float minX = center.x - width / 2f + width * xMarginRatio + thisRadius;
            float maxX = center.x + width / 2f - width * xMarginRatio - thisRadius;
            float minZ = center.y - depth / 2f + depth * zMarginRatio + thisRadius;
            float maxZ = center.y + depth / 2f - depth * zMarginRatio - thisRadius;

            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            Vector2 candidate = new Vector2(x, z);

            bool valid = true;
            foreach (var obs in placedObstacles)
            {
                float minDist = (thisRadius + obs.radius) * spacingMultiplier;
                if ((candidate - obs.position).magnitude < minDist)
                {
                    valid = false;
                    break;
                }
            }

            if (valid) return candidate;
        }

        return Vector2.zero;
    }

    float GetBoundingRadius(GameObject prefab)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 1f;

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);

        Vector3 size = bounds.size;
        return Mathf.Sqrt(size.x * size.x + size.z * size.z) * 0.6f;
    }

    public List<ObstacleData> GetPlacedObstacles()
    {
        return placedObstacles;
    }

/*
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (table != null)
            {
                // table margin
                Vector3 scale = table.localScale;
                float width = 10f * scale.x;
                float depth = 10f * scale.z;
                Vector3 center = table.position;

                // valid places (cyan)
                Gizmos.color = Color.cyan;
                float minX = center.x - width / 2f + width * xMarginRatio;
                float maxX = center.x + width / 2f - width * xMarginRatio;
                float minZ = center.z - depth / 2f + depth * zMarginRatio;
                float maxZ = center.z + depth / 2f - depth * zMarginRatio;
                Vector3 marginCenter = new Vector3((minX + maxX) / 2f, 0.05f, (minZ + maxZ) / 2f);
                Vector3 marginSize = new Vector3(maxX - minX, 0.05f, maxZ - minZ);
                Gizmos.DrawWireCube(marginCenter, marginSize);
            }

            // obstacle radius and type (T yellow, U blue)
            if (placedObstacles != null)
            {
                foreach (var obs in placedObstacles)
                {
                    Gizmos.color = obs.isT ? Color.yellow : Color.blue;
                    Vector3 pos3 = new Vector3(obs.position.x, 0.1f, obs.position.y);
                    Gizmos.DrawWireSphere(pos3, obs.radius); 
                    Gizmos.DrawLine(pos3, pos3 + Vector3.up * 1.0f);
                }
            }
        }
#endif
*/
}