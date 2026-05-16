using System.Collections.Generic;
using UnityEngine;

public class MushroomSpawner : MonoBehaviour
{
    public GameObject mushroomPrefab;
    public Transform ground;
    public Transform caveArea;
    public ObstacleSpawner obstacleSpawner;

    public int count;
    public int maxTries;
    public float spacingMultiplier;
    public float xMarginRatio;
    public float zMarginRatio;

    private List<Vector2> placed = new List<Vector2>();
    private float mushroomRadius;
    private float radiusX;
    private float radiusZ;
    float rockRadius;


    void Start()
    {
        count = Random.Range(5, 11); // random 5–10 mushrooms

        mushroomRadius = GetRadius(mushroomPrefab);
        rockRadius = obstacleSpawner.RockRadius();
        radiusX = caveArea.localScale.x * 0.5f;
        radiusZ = caveArea.localScale.z * 0.5f;

        Generate();
    }


    void Generate()
    {
        Vector3 scale = ground.localScale;
        float width = 10f * scale.x;
        float depth = 10f * scale.z;
        Vector2 center = new Vector2(ground.position.x, ground.position.z);
        List<Vector2> obstacles = obstacleSpawner ? obstacleSpawner.GetObstaclePositions() : null;
        
        for (int i = 0; i < count; i++)
        {
            Vector2 pos = SampleValidPos(width, depth, center, mushroomRadius, obstacles);
            if (pos != Vector2.zero)
            {
                float y = ground.position.y + mushroomPrefab.transform.localScale.y * 0.5f;
                Instantiate(mushroomPrefab, new Vector3(pos.x, y, pos.y), Quaternion.identity);
                placed.Add(pos);
            }
        }
    }

    Vector2 SampleValidPos(float width, float depth, Vector2 center, float radius, List<Vector2> obstacles)
    {
        for (int t = 0; t < maxTries; t++)
        {
            float minX = center.x - width / 2f + width * xMarginRatio + radius + 5f;
            float maxX = center.x + width / 2f - width * xMarginRatio - radius + 5f;
            float minZ = center.y - depth / 2f + depth * zMarginRatio + radius;
            float maxZ = center.y + depth / 2f - depth * zMarginRatio - radius;

            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            Vector2 p = new Vector2(x, z);

            // avoid cave area
            if (IsInsideCave(p, caveArea.position))
                continue;

            // avoid obstacles
            if (obstacles != null)
            {
                foreach (var o in obstacles)
                {
                    float minDist = (mushroomRadius + rockRadius) * spacingMultiplier;
                    if ((p - o).magnitude < minDist)
                        goto NextTry;
                }
            }

            // avoid mushrooms
            foreach (var m in placed)
            {
                if ((p - m).magnitude < radius * 2f * spacingMultiplier)
                    goto NextTry;
            }

            return p;

        NextTry:
            continue;
        }

        return Vector2.zero;
    }

    bool IsInsideCave(Vector2 p, Vector3 caveCenter)
    {
        Vector2 c = new Vector2(caveCenter.x, caveCenter.z);

        float dx = p.x - c.x;
        float dz = p.y - c.y;

        return (dx * dx) / (radiusX * radiusX) +
               (dz * dz) / (radiusZ * radiusZ) < 1f;
    }

    float GetRadius(GameObject prefab)
    {
        Renderer r = prefab.GetComponentInChildren<Renderer>();
        Vector3 size = r.bounds.size;
        return Mathf.Sqrt(size.x * size.x + size.z * size.z) * 0.5f;
    }


/*
    private void OnDrawGizmos()
    {
        if (ground == null || caveArea == null) return;

        Vector3 scale = ground.localScale;
        float width = 10f * scale.x;
        float depth = 10f * scale.z;
        Vector3 center = ground.position;

        float minX = center.x - width / 2f + width * xMarginRatio + 5f;
        float maxX = center.x + width / 2f - width * xMarginRatio + 5f;
        float minZ = center.z - depth / 2f + depth * zMarginRatio;
        float maxZ = center.z + depth / 2f - depth * zMarginRatio;

        // Ground
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(center, new Vector3(width, 0.05f, depth));

        // Valid spawn area
        Gizmos.color = Color.cyan;
        Vector3 validCenter = new Vector3((minX + maxX) / 2f, 0, (minZ + maxZ) / 2f);
        Vector3 validSize = new Vector3(maxX - minX, 0.05f, maxZ - minZ);
        Gizmos.DrawWireCube(validCenter, validSize);

        // cave area
        float rx = caveArea.localScale.x * 0.5f;
        float rz = caveArea.localScale.z * 0.5f;

        DrawEllipse(caveArea.position, rx, rz, Color.red);
    }

    void DrawEllipse(Vector3 center, float radiusX, float radiusZ, Color c, int seg = 60)
    {
        Gizmos.color = c;

        Vector3 prev = center + new Vector3(radiusX, 0, 0);

        for (int i = 1; i <= seg; i++)
        {
            float angle = (float)i / seg * Mathf.PI * 2f;

            Vector3 next = center + new Vector3(
                Mathf.Cos(angle) * radiusX,
                0,
                Mathf.Sin(angle) * radiusZ
            );

            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
    */
}