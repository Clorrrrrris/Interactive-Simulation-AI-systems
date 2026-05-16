using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    public GameObject cylinderPrefab;
    public GameObject prismPrefab;
    public Transform table;

    public float pinballRadius;

    public int cylinderCount;
    public int prismCount;
    public int maxTries;
    public float cylinderSpacingMultiplier;
    public float prismSpacingMultiplier;

    public float xMarginRatio;
    public float zBottomMarginRatio; // margin for paddles
    public float zTopMarginRatio;

    private class ObstacleData
    {
        public Vector2 position;
        public float radius;
        public string type;
    }
    private List<ObstacleData> placedObstacles = new List<ObstacleData>();


    void Start()
    {
        GenerateObstacles();
    }

    void GenerateObstacles()
    {
        if (table == null)
        {
            Debug.LogError("[ObstacleManager] Table not assigned.");
            return;
        }

        Vector3 scale = table.localScale;
        float width = 10f * scale.x;
        float depth = 10f * scale.z;
        Vector2 center = new Vector2(table.position.x, table.position.z);

        float cylinderRadius = GetBoundingRadius(cylinderPrefab);
        // generate cylinders
        for (int i = 0; i < cylinderCount; i++)
        {
            Vector2 pos = GetValidPosition(width, depth, center, cylinderRadius, cylinderSpacingMultiplier);
            if (pos != Vector2.zero)
            {
                GameObject cyl = Instantiate(cylinderPrefab, new Vector3(pos.x, 0f, pos.y), Quaternion.identity);
                cyl.tag = "Cylinder";
                placedObstacles.Add(new ObstacleData { position = pos, radius = cylinderRadius, type = "Cylinder" });
            }
        }


        float prismRadius = GetBoundingRadius(prismPrefab);
        // generate triangular prisms
        for (int i = 0; i < prismCount; i++)
        {
            Vector2 pos = GetValidPosition(width, depth, center, prismRadius, prismSpacingMultiplier);
            if (pos != Vector2.zero)
            {
                float angle = Random.Range(0f, 360f);
                if (Mathf.Abs(Mathf.Sin(angle * Mathf.Deg2Rad)) < 0.2f)
                    angle += 30f;

                GameObject prism = Instantiate(prismPrefab, new Vector3(pos.x, 0f, pos.y), Quaternion.Euler(0f, angle, 0f));
                prism.tag = "Prism";

                Renderer r = prism.GetComponentInChildren<Renderer>();
                if (r != null)
                {
                    Vector3 size = r.bounds.size;
                    prismRadius = Mathf.Sqrt(size.x * size.x + size.z * size.z) * 0.5f;
                }

                placedObstacles.Add(new ObstacleData { position = pos, radius = prismRadius, type = "Prism" });
            }
        }
    }

    Vector2 GetValidPosition(float width, float depth, Vector2 center, float thisRadius, float spacingMultiplier)
    {
        for (int tries = 0; tries < maxTries; tries++)
        {
            float minX = center.x - width / 2f + width * xMarginRatio + thisRadius;
            float maxX = center.x + width / 2f - width * xMarginRatio - thisRadius;
            float minZ = center.y - depth / 2f + depth * zBottomMarginRatio + thisRadius;
            float maxZ = center.y + depth / 2f - depth * zTopMarginRatio - thisRadius;

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
        Renderer r = prefab.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            Vector3 size = r.bounds.size;
            return Mathf.Sqrt(size.x * size.x + size.z * size.z) * 0.5f;
        }
        return 1f;
    }

}