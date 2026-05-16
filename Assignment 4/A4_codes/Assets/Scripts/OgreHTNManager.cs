using UnityEngine;
using System.Collections.Generic;

public class OgreHTNManager : MonoBehaviour
{
    public OgreIdleBehavior idle;
    public OgreAttackBehavior attack;
    public OgreTreasureBehavior treasure;
    public OgrePerception perception;
    

    public PlayerController player;
    private int idleRepeatRemaining = 0;
    

    public enum TaskType
    {
        Stationary, Patrol, Turn, Forage,
        AttackPlayer, ChasePlayer
    }

    private Queue<TaskType> plan = new Queue<TaskType>();
    private TaskType? currentTask = null;
    private bool patrolStarted = false;
    public float stationaryDuration;

    // UI display
    public OgrePlanUI ui;
    public string ogreName;


    void Start()
    {
        BuildIdlePlan();
        ui.SetOgreName(ogreName);
    }

    void Update()
    {
        if (treasure.treasureStolen && player.isInvisible && currentTask == TaskType.AttackPlayer)
            ForceSwitchTo(TaskType.ChasePlayer);

        if (treasure.treasureStolen)
        {
            if (currentTask != TaskType.ChasePlayer && currentTask != TaskType.AttackPlayer)
            ForceSwitchTo(TaskType.ChasePlayer);

            if (perception.CanSeePlayer() && !player.isInvisible && currentTask != TaskType.AttackPlayer)
                ForceSwitchTo(TaskType.AttackPlayer);
        }
        else
        {
            if (perception.CanSeePlayer() && !player.isInvisible)
                ForceSwitchTo(TaskType.AttackPlayer);
        }

        ExecutePlan();

        /*
        if (perception.CanSeePlayer() && !player.isInvisible)
            ForceSwitchTo(TaskType.AttackPlayer);

        ExecutePlan();
        */
    }

    public void ForceSwitchTo(TaskType newTask)
    {
        plan.Clear(); 
        currentTask = newTask;

        patrolStarted = false;
        idle.TurnStarted = false;
    }


    void BuildIdlePlan()
    {
        plan.Clear();
        plan.Enqueue(TaskType.Patrol);
        plan.Enqueue(TaskType.Stationary);
        plan.Enqueue(TaskType.Turn);
        plan.Enqueue(TaskType.Stationary);
        plan.Enqueue(TaskType.Forage);
        plan.Enqueue(TaskType.Stationary);
    }


    void ExecutePlan()
    {
        if (ui != null && currentTask != null)
        {
            ui.UpdateCurrentTask(currentTask.ToString());
            ui.UpdatePlanList(GetRemainingPlan());
        }
        if (currentTask == null)
        {
            if (plan.Count == 0)
            {
                BuildIdlePlan();
                return;
            }
            currentTask = plan.Dequeue();
            if (currentTask == TaskType.Patrol)
            {
                idleRepeatRemaining = Random.Range(2, 5);   // randomly do 2-4 times for patrol
            }
            else if (currentTask == TaskType.Turn)
            {
                idleRepeatRemaining = 2;   // turn 2 times
            }
            else
            {
                idleRepeatRemaining = 1;
            }
        }

        switch (currentTask)
        {
            case TaskType.Stationary:
                RunStationary();
                break;

            case TaskType.Patrol:
                RunPatrol();
                break;
            
            case TaskType.Turn:
                RunTurn();
                break;
            
            case TaskType.Forage:
                RunForage();
                break;
            
            case TaskType.AttackPlayer:
                RunAttack();
                break;

            case TaskType.ChasePlayer:
                RunChase();
                break;
        }
    }

    // plan list
    private IEnumerable<string> GetRemainingPlan()
    {
        foreach (var t in plan)
            yield return t.ToString();
    }


    // Idle behaviours
    void RunStationary()
    {
        if (idle.idleStatus != OgreIdleBehavior.IdleStatus.Stationary)
        {
            idle.BeginStationary(stationaryDuration); // wait for x sec
            return;
        }

        idle.UpdateStationary();

        if (!idle.StationaryFinished())   
            return;

        idleRepeatRemaining--;
        if (idleRepeatRemaining <= 0)
        {
            currentTask = null;
        }
    }

    void RunPatrol()
    {
        // start walking around
        if (!patrolStarted)
        {
            idle.BeginFullPatrol();
            patrolStarted = true;
            return;
        }

        if (!idle.FullPatrolFinished())
            return;

        patrolStarted = false;
        idleRepeatRemaining--;

        // switch idle behaviour
        if (idleRepeatRemaining <= 0)
        {
            currentTask = null;
        }

    }

    void RunTurn()
    {
        // start turn around
        if (!idle.TurnStarted)
        {
            idle.BeginTurn(360f); 
            idle.TurnStarted = true;
            return;
        }

        // update
        if (!idle.TurnFinished())
        {
            idle.UpdateTurn();
            return;
        }

        idle.TurnStarted = false;
        idleRepeatRemaining--;

        // switch idle behaviour
        if (idleRepeatRemaining <= 0)
        {
            currentTask = null;
        }
    }

    void RunForage()
    {
        Transform mush = idle.FindNearestMushroom();
        if (!mush)
        {
            currentTask = null;
            return;
        }

        if (!patrolStarted)
        {
            idle.BeginFullPatrolTo(mush);
            patrolStarted = true;
            return;
        }

        if (idle.ArrivedTarget())
        {
            Destroy(mush.gameObject);
            idle.BeginFullReturn();
            return;
        }

        if (!idle.FullPatrolFinished())
            return;

        patrolStarted = false;
        idleRepeatRemaining--;

        // switch idle behaviour
        if (idleRepeatRemaining <= 0)
        {
            currentTask = null;
        }
    }



    // Attack player
    void RunAttack()
    {
        // look for rock once
        if (!attack.goingForRock) 
        {
            Transform rock = attack.FindNearestRock();
            //Debug.Log("[Attack] Going to rock at " + rock.position);
            attack.BeginMoveToRock(rock);
            patrolStarted = true;
            return;
        }

        // pick up and throw rock
        if (attack.ArrivedRock())
        {
            //Debug.Log("[Attack] Reached rock → picking up + throwing.");
            attack.PickupAndThrowRock();
            attack.hasThrownRock = false;

            idle.BeginFullReturn();
            patrolStarted = false;
            currentTask = null;
            return;
        }

        if (!idle.FullPatrolFinished())
            return;
    }

    // Treasure stolen -> Chase player
    void RunChase()
    {
        treasure.ChasePlayer();

        if (perception.CanSeePlayer())
        {
            ForceSwitchTo(TaskType.AttackPlayer);
            return;
        }
    }

    public void OnTreasureStolen()
    {
        treasure.treasureStolen = true;
        ForceSwitchTo(TaskType.ChasePlayer);
    }
}
