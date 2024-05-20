using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompanionIdleState : CompanionBaseState
{
    public CompanionIdleState(CompanionStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        OnNewStateConfig(false, false, true, 0);
        PartyData.Instance.UpdateCompanionFollowBehaviour(stateMachine, false);

        stateMachine.playerStateMachine.PlayerIsSprinting += IsPlayerSprinting;
    }

    public override void UpdateState()
    {
        stateMachine.animator.SetSpeed(stateMachine.navMeshAgent.velocity.magnitude);

        if (ShouldMove())
        {
            stateMachine.SwitchState(stateMachine.followState);
        }
    }

    public override void ExitState()
    {
        stateMachine.playerStateMachine.PlayerIsSprinting -= IsPlayerSprinting;
    }

    private bool ShouldMove()
    {
        return Vector3.Distance(stateMachine.player.position, transform.position) >= PartyData.Instance.GetDistanceToBeginFollow(stateMachine, false);
    }
}
