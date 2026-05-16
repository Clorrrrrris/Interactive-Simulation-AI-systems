using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SingleObstacleVertexFinder : MonoBehaviour
{
    public bool showGizmos;
    private List<Vector3> allVertices = new List<Vector3>();
    public float vertexYOffset = 0f;

    void Start()
    {
        GetAllVertices();
    }

    public List<Vector3> GetAllVertices()
    {
        allVertices.Clear();

        BoxCollider[] boxColliders = GetComponentsInChildren<BoxCollider>();

        foreach (BoxCollider col in boxColliders)
        {
            Vector3 c = col.center;
            Vector3 s = col.size * 0.5f;

            Vector3[] corners = new Vector3[8]
            {
                new Vector3(c.x - s.x, c.y - s.y, c.z - s.z),
                new Vector3(c.x + s.x, c.y - s.y, c.z - s.z),
                new Vector3(c.x - s.x, c.y - s.y, c.z + s.z),
                new Vector3(c.x + s.x, c.y - s.y, c.z + s.z),
                new Vector3(c.x - s.x, c.y + s.y, c.z - s.z),
                new Vector3(c.x + s.x, c.y + s.y, c.z - s.z),
                new Vector3(c.x - s.x, c.y + s.y, c.z + s.z),
                new Vector3(c.x + s.x, c.y + s.y, c.z + s.z)
            };
            List<int> selectedIndices = new List<int>();

            if (col.CompareTag("VerticalT"))
                selectedIndices = new List<int>() { 1, 0 }; //0,1 bottom; 4,5 top
            else if (col.CompareTag("VerticalULeft"))
                selectedIndices = new List<int>() { 2, 3 }; //2,3 bottom; 6,7 top
            else if (col.CompareTag("VerticalURight"))
                selectedIndices = new List<int>() { 2, 3 }; //2,3 bottom; 6,7 top
            else if (col.CompareTag("HorizontalU"))
                selectedIndices = new List<int>() { 1, 0 }; //0,1 bottom; 4,5 top
            else if (col.CompareTag("HorizontalT"))
                selectedIndices = new List<int>() { 0, 2, 3, 1 }; //0,2,3,1 bottom; 4,5,6,7 top

            foreach (var i in selectedIndices)
            {
                Vector3 worldV = col.transform.TransformPoint(corners[i]);
                worldV += Vector3.up * vertexYOffset;
                allVertices.Add(worldV);
            }
        }
        return allVertices;
    }
    

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGizmos || allVertices == null || allVertices.Count == 0) return;
        Gizmos.color = Color.green;
        foreach (var v in allVertices)
            Gizmos.DrawSphere(v, 1f);
    }
#endif

}