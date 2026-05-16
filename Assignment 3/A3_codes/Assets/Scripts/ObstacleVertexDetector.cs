using System.Collections.Generic;
using UnityEngine;

//[ExecuteAlways]
public class ObstacleVertexDetector : MonoBehaviour
{
    public bool showGizmos;

    private List<Vector3[]> allObstacleVertices = new List<Vector3[]>();

    void Start()
    {
        GetAllObstacleVertices();
    }

    public List<Vector3[]> GetAllObstacleVertices()
    {
        allObstacleVertices.Clear();
        SingleObstacleVertexFinder[] obstacles = FindObjectsByType<SingleObstacleVertexFinder>(FindObjectsSortMode.None);

        foreach (var obs in obstacles)
        {
            //obs.GetAllVertices();
            List<Vector3> vertices = obs.GetAllVertices();
            allObstacleVertices.Add(vertices.ToArray());
        }
        return allObstacleVertices;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGizmos || allObstacleVertices.Count == 0) return;

        Gizmos.color = Color.green;
        foreach (Vector3[] vertexGroup in allObstacleVertices)
        {
            foreach (Vector3 v in vertexGroup)
                Gizmos.DrawSphere(v, 1f);
        }
    }
#endif
}