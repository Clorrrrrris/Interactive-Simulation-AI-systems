using UnityEngine;

public class OgrePerception : MonoBehaviour
{
    public Transform ground;
    public Transform player; 

    public float viewRadius;
    public float viewAngle = 90f;

    public LayerMask obstacleMask;
    public LayerMask targetMask;

    void Start()
    {
        var bounds = ground.GetComponent<Renderer>().bounds;
        float widthX = bounds.size.x;
        float widthZ = bounds.size.z;
        viewRadius = (widthX + widthZ) * 0.25f; 
    }

    void Update()
    {
        Vector3 origin = transform.position;
        Vector3 p = player.transform.position;

        float dist = Vector3.Distance(origin, p);
    }

    public bool CanSeePlayer()
    {
        // check if player invisible
        var pc = player.GetComponent<PlayerController>();
        if (pc != null && pc.isInvisible)
            return false;

        Vector3 origin = transform.position + Vector3.up * 2f;
        Vector3 targetPos = player.position + Vector3.up * 1f; 
        Vector3 dir = (targetPos - origin).normalized;

        float dist = Vector3.Distance(origin, targetPos);
        //Debug.DrawLine(origin, targetPos, Color.green);

        // fov length
        if (dist > viewRadius) 
            return false;

        // fov angle
        if (Vector3.Angle(transform.forward, dir) > viewAngle * 0.5f)
            return false;

        // detect obstacle
        LayerMask mask = obstacleMask | targetMask;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, viewRadius, mask))
            return hit.transform == player;

        return false;
    }

    void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * 3f;

        Color fillColor = new Color(1f, 1f, 0f, 0.15f);
        Color lineColor = new Color(1f, 1f, 0f, 0.8f);

        Gizmos.color = lineColor;

        Vector3 leftDir = DirFromAngle(-viewAngle / 2f);
        Vector3 rightDir = DirFromAngle(viewAngle / 2f);

        Gizmos.DrawLine(origin, origin + leftDir * viewRadius);
        Gizmos.DrawLine(origin, origin + rightDir * viewRadius);

        Gizmos.color = fillColor;
        int segments = 25;
        float angleStep = viewAngle / segments;

        Vector3 prev = origin + DirFromAngle(-viewAngle / 2f) * viewRadius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = -viewAngle / 2f + angleStep * i;
            Vector3 next = origin + DirFromAngle(angle) * viewRadius;

            Gizmos.DrawLine(origin, next);
            Gizmos.DrawLine(prev, next);

            prev = next;
        }
    }

    private Vector3 DirFromAngle(float angle)
    {
        angle += transform.eulerAngles.y;
        float rad = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
    }
}