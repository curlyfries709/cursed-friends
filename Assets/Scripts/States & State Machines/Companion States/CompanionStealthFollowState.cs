using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompanionStealthFollowState : CompanionBaseState
{
    public CompanionStealthFollowState(CompanionStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        OnNewStateConfig(false, true, true, 0);
        stateMachine.followBehaviour.UpdateCompanionFollowBehaviour(stateMachine, true);

        randNum = Random.Range(0.8f, stateMachine.sneakSpeed);
        stateMachine.navMeshAgent.speed = stateMachine.sneakSpeed;
        
    }

    public override void UpdateState()
    {
        Vector3 destination = CalculateDestination();

        stateMachine.navMeshAgent.SetDestination(destination);

        AlterStealthSpeed(destination);

        stateMachine.animator.SetMovementSpeed(stateMachine.navMeshAgent.velocity.magnitude);

        if (HasArrivedAtDestination())
        {
            stateMachine.SwitchState(stateMachine.stealthIdleState);
        }
    }


    public override void ExitState()
    {

    }
}
