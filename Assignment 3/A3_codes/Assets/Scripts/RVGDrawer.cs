using UnityEngine;
using System.Collections.Generic;

public class RVGDrawer : MonoBehaviour
{
    [SerializeField] private Material lineMaterial;
    private readonly List<LineRenderer> activeLines = new();

    public void DrawPath(List<Vector3> path, Color color, float width, float heightOffset)
    {
        if (path == null || path.Count < 2) return;

        for (int i = 0; i < path.Count; i++)
        {
            path[i] += Vector3.up * heightOffset; 
        }

        GameObject lineObj = new GameObject("RuntimePathLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();

        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        lr.material = mat;
        
        //lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = lr.endWidth = width;
        lr.positionCount = path.Count;
        lr.useWorldSpace = true;
        lr.startColor = lr.endColor = color;
        lr.numCapVertices = 8;
        lr.numCornerVertices = 8;

        lr.SetPositions(path.ToArray());
        activeLines.Add(lr);
    }
}