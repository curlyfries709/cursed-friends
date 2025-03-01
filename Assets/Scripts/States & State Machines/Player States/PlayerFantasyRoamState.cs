using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFantasyRoamState : PlayerBaseState
{
    public PlayerFantasyRoamState(PlayerStateMachine stateMachine) : base(stateMachine){}

    public override void EnterState()
    {
        EnterStateConfig(true, true, false, 0);
        stateMachine.ActivateFreeRoamComponents(true);
    }

    public override void UpdateState()
    {
        JumpGravityAndGrounded();
        Move(stateMachine.inputSprint, stateMachine.walkSpeed, stateMachine.runSpeed);

        UpdateAnimator();
    }

    public override void ExitState()
    {
        ExitStateConfig(true);
    }

    private void UpdateAnimator()
    {
        stateMachine.animator.SetMovementSpeed(stateMachine.animationBlend);
        stateMachine.animator.SetMotionSpeed(inputMagnitude);
    }

}
