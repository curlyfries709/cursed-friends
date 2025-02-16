using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyInvestigateState : EnemyBaseState
{
    public EnemyInvestigateState(EnemyStateMachine stateMachine) : base(stateMachine) { }


    public override void EnterState()
    {
        OnEnterStateConfig(false, 0);

        stateMachine.navMeshAgent.updateRotation = true;

        stateMachine.navMeshAgent.speed = stateMachine.walkSpeed;
        stateMachine.navMeshAgent.stoppingDistance = stateMachine.defaultStoppingDistance;
        
        stateMachine.navMeshAgent.SetDestination(stateMachine.GetPlayerStateMachine().transform.position);
        stateMachine.navMeshAgent.isStopped = false;
    }

    public override void UpdateState()
    {
        stateMachine.animator.SetSpeed(stateMachine.navMeshAgent.velocity.magnitude);

        if (!stateMachine.GetPlayerStateMachine().InStealth() || stateMachine.GetPlayerStateMachine().moveValue != Vector2.zero)
        {
            PlayerSpotted();
            return;
        }

        if (stateMachine.navMeshAgent.remainingDistance <= 1f)
        {
            //Remove Barrel.
            barrel.ForcedRemove();
            UpdatePlayerLastKnownData(stateMachine.GetPlayerStateMachine().transform.position, stateMachine.GetPlayerStateMachine().transform.rotation);
            PlayerSpotted();
        }
    }

    public override void ExitState()
    {
        
    }

}
