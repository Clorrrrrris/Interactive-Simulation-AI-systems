using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AgentMover : MonoBehaviour
{
    public Transform table; // level surface
    public DestinationManager goalManager;
    public TerrainManager terrainManager;
    public PathfindingManager pathManager;

    public float moveSpeed;
    private List<Vector3> path = new();
    private int currentIndex = 0;
    private Vector3 goalPosition;
    private bool moving = false;

    void Start()
    {
        StartCoroutine(BeginMovement());
        float width = 10f * table.localScale.x;
        // it would take them around 2s to get fully across the level length 
        // in a straight line if walking cost were 1.0 throughout
        moveSpeed = width / 2f;  
    }

    private IEnumerator BeginMovement()
    {
        yield return new WaitForSeconds(1.0f); // wait path built

        if (pathManager != null)
        {
            path = pathManager.GetFinalPath();
            if (path.Count > 1)
            {
                moving = true;
                currentIndex = 1; // start from first segment
                //Debug.Log($"[AgentMover]Path loaded ({path.Count} nodes)");
            }
            else
                Debug.LogWarning("[AgentMover] Path not found.");
        }
    }

    void Update()
    {
        if (!moving || path == null || currentIndex >= path.Count) return;
        
        Vector3 target = path[currentIndex];
        float terrainCost = terrainManager.GetCostAtPosition(transform.position);

        float adjustedSpeed = moveSpeed / terrainCost; // speed decreases as cost increases
        float step = adjustedSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, step);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            currentIndex++;
            if (currentIndex >= path.Count)
                moving = false;
        }
    }
}