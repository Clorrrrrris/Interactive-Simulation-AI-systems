using UnityEngine;
using UnityEngine.AI;

public class OgreIdleBehavior : MonoBehaviour
{
    public NavMeshAgent agent;
    private Vector3 startPoint;

    // stationary
    public enum IdleStatus
    {
        None,
        Stationary, 
    }
    public IdleStatus idleStatus = IdleStatus.None;
    private float stationaryTimer = 0f;

    // walking around two points
    public Transform patrolA;
    public Transform patrolB;
    private enum PatrolState { None, GoingToTarget, ReturningToStart }
    private PatrolState patrolState = PatrolState.None;
    private Transform currentTarget;

    // turn around
    public float turnSpeed;
    private float turnRemaining;
    public bool TurnStarted;

    // forage mushrooms
    //public OgrePerception perception;
    public float detectRadius;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        startPoint = transform.position;
        //detectRadius = perception.viewRadius;
    }

    // Stationary
    public void BeginStationary(float duration)
    {
        idleStatus = IdleStatus.Stationary;
        stationaryTimer = duration;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    public void UpdateStationary()
    {
        if (idleStatus != IdleStatus.Stationary)
            return;

        stationaryTimer -= Time.deltaTime;

        if (stationaryTimer <= 0f)
        {
            idleStatus = IdleStatus.None;
        }
    }

    public bool StationaryFinished()
    {
        return idleStatus == IdleStatus.None;
    }

    // Patrol
    public void BeginFullPatrol()
    {
        // randomly choose target A or B
        currentTarget = (Random.value < 0.5f ? patrolA : patrolB);
        patrolState = PatrolState.GoingToTarget;
        agent.isStopped = false;
        agent.SetDestination(currentTarget.position);
    }

    public bool FullPatrolFinished()
    {
        if (patrolState == PatrolState.GoingToTarget)
        {
            if (!Arrived()) return false;

            patrolState = PatrolState.ReturningToStart;
            agent.SetDestination(startPoint);
            return false;
        }

        if (patrolState == PatrolState.ReturningToStart)
        {
            if (!Arrived()) return false;
            patrolState = PatrolState.None;
            return true;
        }

        return false;
    }

    private bool Arrived()
    {
        if (agent.pathPending) return false;
        if (agent.remainingDistance > agent.stoppingDistance + 0.05f) return false;
        return !agent.isStopped && agent.velocity.sqrMagnitude < 0.05f;
    }


    // Turn in place
    public void BeginTurn(float degrees)
    {
        turnRemaining = degrees;
    }
    public void UpdateTurn()
    {
        if (turnRemaining <= 0) return;

        float step = turnSpeed * Time.deltaTime;
        float amount = Mathf.Min(step, turnRemaining);

        transform.Rotate(Vector3.up * amount);
        turnRemaining -= amount;
    }
    public bool TurnFinished()
    {
        return turnRemaining <= 0;
    }


    // Foraging for mushrooms
    public Transform FindNearestMushroom()
    {
        GameObject[] mush = GameObject.FindGameObjectsWithTag("Mushroom");
        Transform best = null;
        float bestDist = Mathf.Infinity;

        foreach (var m in mush)
        {
            float d = Vector3.Distance(transform.position, m.transform.position);
            if (d < bestDist && d <= detectRadius)
            {
                best = m.transform;
                bestDist = d;
            }
        }
        return best;
    }

    public void BeginFullPatrolTo(Transform target)
    {
        if (target == null) return;

        currentTarget = target;
        patrolState = PatrolState.GoingToTarget;

        agent.isStopped = false;
        agent.SetDestination(currentTarget.position);
    }

    public void BeginFullReturn()
    {
        patrolState = PatrolState.ReturningToStart;
        agent.isStopped = false;
        agent.SetDestination(startPoint);
    }

    public bool ArrivedTarget()
    {
        return patrolState == PatrolState.GoingToTarget && Arrived();
    }
}