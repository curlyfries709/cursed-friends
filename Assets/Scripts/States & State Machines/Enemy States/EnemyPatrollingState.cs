using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnotherRealm;

public class EnemyPatrollingState : EnemyBaseState
{
    public EnemyPatrollingState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    private int currentWaypointIndex = 0;
    private bool idleAtWaypoint = true;

    public override void EnterState()
    {
        OnEnterStateConfig(false, 0);
        stateMachine.SetHostileComponents(stateMachine.isHostile);

        stateMachine.navMeshAgent.enabled = true;
        stateMachine.navMeshAgent.isStopped = false;
        stateMachine.navMeshAgent.speed = stateMachine.walkSpeed;
        stateMachine.navMeshAgent.stoppingDistance = stateMachine.defaultStoppingDistance;

        CombatFunctions.SetPatrolDestination(stateMachine.patrolRoute, transform.position, ref idleAtWaypoint, ref currentWaypointIndex, stateMachine.navMeshAgent, stateMachine.continuePatrol);
    }

    public override void UpdateState()
    {
        LookoutForSuspiciousActivity();

        stateMachine.animator.SetSpeed(stateMachine.navMeshAgent.velocity.magnitude);

        if (idleAtWaypoint)
        {
            if (HasArrivedAtDestination())
            {
                stateMachine.SwitchState(stateMachine.idleState);
            }
        }
        else if(!idleAtWaypoint && stateMachine.navMeshAgent.remainingDistance <= stateMachine.navMeshAgent.stoppingDistance + 0.3f)
        {
            //CombatFunctions.GoToNextWaypoint(ref currentWaypointIndex, stateMachine.patrolRoute, ref idleAtWaypoint, stateMachine.navMeshAgent);
            CombatFunctions.SetPatrolDestination(stateMachine.patrolRoute, transform.position, ref idleAtWaypoint, ref currentWaypointIndex, stateMachine.navMeshAgent, true);

        }

    }

    public override void ExitState()
    {
        
    }







    
}
