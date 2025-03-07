using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompanionStealthIdleState : CompanionBaseState
{
    public CompanionStealthIdleState(CompanionStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        OnNewStateConfig(false, true, true, 0);
        stateMachine.navMeshAgent.ResetPath();

        stateMachine.followBehaviour.UpdateCompanionFollowBehaviour(stateMachine, true);
    }

    public override void UpdateState()
    {
        stateMachine.animator.SetMovementSpeed(stateMachine.navMeshAgent.velocity.magnitude);

        if (ShouldMove())
        {
            stateMachine.SwitchState(stateMachine.stealthFollowState);
        }
    }

    public override void ExitState()
    {

    }

    private bool ShouldMove()
    {
        return Vector3.Distance(stateMachine.player.position, transform.position) >= stateMachine.followBehaviour.GetDistanceToBeginFollow(stateMachine, true);
    }
}
