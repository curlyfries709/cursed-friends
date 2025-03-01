using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompanionFollowState : CompanionBaseState
{
    public CompanionFollowState(CompanionStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        OnNewStateConfig(false, false, true, 0);
        stateMachine.followBehaviour.UpdateCompanionFollowBehaviour(stateMachine, false);


        randNum = Random.Range(0.8f, stateMachine.walkSpeed);
        stateMachine.playerStateMachine.PlayerIsSprinting += IsPlayerSprinting;
    }

    public override void UpdateState()
    {
        Vector3 destination = CalculateDestination();

        stateMachine.navMeshAgent.SetDestination(destination);

        AlterRoamSpeed(destination);

        stateMachine.animator.SetMovementSpeed(stateMachine.navMeshAgent.velocity.magnitude);

        if (HasArrivedAtDestination())
        {
            stateMachine.SwitchState(stateMachine.idleState);
        }
    }


    public override void ExitState()
    {
        stateMachine.playerStateMachine.PlayerIsSprinting -= IsPlayerSprinting;
    }

   





}
