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
        
        stateMachine.navMeshAgent.SetDestination(stateMachine.playerStateMachine.transform.position);
        stateMachine.navMeshAgent.isStopped = false;
    }

    public override void UpdateState()
    {
        stateMachine.animator.SetSpeed(stateMachine.navMeshAgent.velocity.magnitude);

        if (!stateMachine.playerStateMachine.InStealth() || stateMachine.playerStateMachine.moveValue != Vector2.zero)
        {
            PlayerSpotted();
            return;
        }

        if (stateMachine.navMeshAgent.remainingDistance <= 1f)
        {
            //Remove Barrel.
            barrel.ForcedRemove();
            UpdatePlayerLastKnownData(stateMachine.playerStateMachine.transform.position, stateMachine.playerStateMachine.transform.rotation);
            PlayerSpotted();
        }
    }

    public override void ExitState()
    {
        
    }

}
