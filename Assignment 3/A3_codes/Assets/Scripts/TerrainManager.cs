using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class TerrainManager : MonoBehaviour
{
    public List<GameObject> terrainPlanes = new List<GameObject>();

    public float minCost;
    public float maxCost;

    public bool showCostInConsole;
    public bool showCostLabel;

    private Dictionary<GameObject, float> costMap = new Dictionary<GameObject, float>();

    void Start()
    {
        AssignColorsAndCosts();
    }

    void AssignColorsAndCosts()
    {
        if (terrainPlanes.Count == 0)
        {
            Debug.LogError("[TerrainColorAssigner] No planes assigned.");
            return;
        }

        foreach (GameObject plane in terrainPlanes)
        {
            // assign random costs
            float cost;
            if (Random.value > 0.5f)
                cost = Random.Range(minCost, (minCost + maxCost) / 2f);
            else
                cost = Random.Range((minCost + maxCost) / 2f, maxCost);
            costMap[plane] = cost;

            // color from light green to dark green, cost increase
            Color lightGreen = new Color(0.75f, 0.95f, 0.70f); // #BFF2B3
            Color darkGreen  = new Color(0.02f, 0.10f, 0.02f); // #051A05

            float t = (cost - minCost) / (maxCost - minCost);
            Color c = Color.Lerp(lightGreen, darkGreen, t);

            Renderer r = plane.GetComponent<Renderer>();
            if (r != null)
            {
                Material matCopy = new Material(r.sharedMaterial);
                matCopy.color = new Color(c.r, c.g, c.b, 0.8f); 
                r.material = matCopy;
            }

            if (showCostInConsole)
                Debug.Log($"{plane.name} → Cost = {cost:F2}");

            if (showCostLabel)
            {
                GameObject label = new GameObject("CostLabel");
                label.transform.SetParent(plane.transform, false);
                label.transform.localPosition = new Vector3(0, 10f, 0);
                label.transform.localRotation = Quaternion.Euler(90, 0, 0);

                TextMeshPro tmp = label.AddComponent<TextMeshPro>();
                tmp.text = cost.ToString("F1");
                tmp.fontSize = 10f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.black;
            }
        }
    }

    public float GetCostAtPosition(Vector3 worldPos)
    {
        foreach (var kv in costMap)
        {
            GameObject plane = kv.Key;
            Renderer r = plane.GetComponent<Renderer>();
            if (r == null) continue;

            Bounds b = r.bounds;

            if (worldPos.x > b.min.x && worldPos.x < b.max.x &&
                worldPos.z > b.min.z && worldPos.z < b.max.z)
            {
                return kv.Value;
            }
        }
        return 1.0f; 
    }

    public float GetMinCost()
    {
        if (costMap.Count == 0)
            return minCost; // fallback

        float min = float.PositiveInfinity;
        foreach (float c in costMap.Values)
            if (c < min)
                min = c;
        return min;
    }
}