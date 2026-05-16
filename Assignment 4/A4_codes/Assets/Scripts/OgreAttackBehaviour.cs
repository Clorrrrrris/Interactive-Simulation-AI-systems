using UnityEngine;

public class OgreAttackBehavior : MonoBehaviour
{
    public OgreIdleBehavior idle;
    public PlayerController playerControl;
    public Transform player;

    public GameObject thrownRockPrefab;

    public bool goingForRock = false;
    public Transform targetRock;
    public float throwSpeed;
    public bool hasThrownRock = false;

    public Transform FindNearestRock()
    {
        GameObject[] rocks = GameObject.FindGameObjectsWithTag("Rock");
        Transform best = null;
        float bestDist = Mathf.Infinity;

        foreach (var r in rocks)
        {
            float d = Vector3.Distance(transform.position, r.transform.position);
            if (d < bestDist)
            {
                best = r.transform;
                bestDist = d;
            }
        }

        return best;
    }

    public void BeginMoveToRock(Transform rock)
    {
        goingForRock = true;
        targetRock = rock;
        idle.BeginFullPatrolTo(rock);
    }

    public bool ArrivedRock()
    {
        return goingForRock && idle.ArrivedTarget();
    }

    public void PickupAndThrowRock()
    {
        if (!targetRock) return;
        if (hasThrownRock) return; 
        hasThrownRock = true;

        player = GameObject.Find("Player").transform; 
        //Debug.Log($"[Throw] Picking up rock at {targetRock.position}");
        
        Vector3 start = targetRock.position;
        start.y = 12f;
        Vector3 end = player.transform.position;
        end.y = 3f;
        //Debug.Log($"[Throw] Start = {start}, End = {end}");

        //Debug.Log($"[Throw] Throw from {start} to {end}");
        GameObject proj = Instantiate(thrownRockPrefab, start, Quaternion.identity);
        proj.AddComponent<ThrownRock>();

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dir = (end - start).normalized;
            Vector3 vel = dir * throwSpeed;
            rb.linearVelocity = vel;
            rb.AddForce(vel, ForceMode.VelocityChange);

            //Debug.Log($"[Throw] Applied velocity = {vel}");
        }

        Destroy(targetRock.gameObject);
        goingForRock = false;
        targetRock = null;
    }
}