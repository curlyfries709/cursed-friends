using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AnotherRealm;

public class PatrollingActor : MonoBehaviour
{
    [Header("Times")]
    [SerializeField] float minIdleTime = 1f;
    [SerializeField] float maxIdleTime = 3f;
    [Header("Components")]
    [SerializeField] PatrolPathVisual patrolPath;
    [Space(10)]
    [SerializeField] Animator animator;
    [SerializeField] NavMeshAgent navMeshAgent;

    Transform patrolRoute;
    private int currentWaypointIndex = 0;
    private bool idleAtWaypoint = true;

    bool continuePatrol = false;
    bool idling = false;

    private void Awake()
    {
        if (patrolPath)
            patrolRoute = patrolPath.transform;
    }

    private void OnEnable()
    {
        idling = false;
        CombatFunctions.SetPatrolDestination(patrolRoute, transform.position, ref idleAtWaypoint, ref currentWaypointIndex, navMeshAgent, false);
    }

    private void Update()
    {
        animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);

        if (idling){ return; }

        

        if (idleAtWaypoint)
        {
            if (CombatFunctions.HasAgentArrivedAtDestination(navMeshAgent) && !idling)
            {
                StartCoroutine(IdleRoutine());
            }
        }
        else if (!idleAtWaypoint && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.3f)
        {
            //CombatFunctions.GoToNextWaypoint(ref currentWaypointIndex, stateMachine.patrolRoute, ref idleAtWaypoint, stateMachine.navMeshAgent);
            CombatFunctions.SetPatrolDestination(patrolRoute, transform.position, ref idleAtWaypoint, ref currentWaypointIndex, navMeshAgent, true);
        }
    }

    IEnumerator IdleRoutine()
    {
        idling = true;
        float waitTime = GetRandomIdleTime();

        yield return new WaitForSeconds(waitTime);

        CombatFunctions.SetPatrolDestination(patrolRoute, transform.position, ref idleAtWaypoint, ref currentWaypointIndex, navMeshAgent, true);
        idling = false;
    }


    private float GetRandomIdleTime()
    {
        return Random.Range(minIdleTime, maxIdleTime);
    }
}
