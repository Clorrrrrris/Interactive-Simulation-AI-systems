using UnityEngine;
using UnityEngine.AI;

public class OgreTreasureBehavior : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public bool treasureStolen = false;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
    }

    public void ChasePlayer()
    {
        agent.SetDestination(player.position);
    }
}