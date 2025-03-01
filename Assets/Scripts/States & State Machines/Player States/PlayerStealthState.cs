using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStealthState : PlayerBaseState
{
    public PlayerStealthState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        EnterStateConfig(true, true, false, 0);
        stateMachine.SwitchToStealth?.Invoke(true);
        stateMachine.animator.SetBool(stateMachine.animator.animIDStealth, true);

        stateMachine.SetStealthControllerConfigValues();
    }

    public override void UpdateState()
    {
        JumpGravityAndGrounded();
        Move(false, stateMachine.sneakSpeed, stateMachine.sneakSpeed);

        UpdateAnimator();

        if (stateMachine.inputSprint && stateMachine.moveValue != Vector2.zero)
        {
            stateMachine.SwitchState(stateMachine.fantasyRoamState);
        }
    }

    public override void ExitState()
    {
        ExitStateConfig(true);

        stateMachine.SwitchToStealth?.Invoke(false);
        stateMachine.animator.SetBool(stateMachine.animator.animIDStealth, false);
        stateMachine.SetControllerConfig(stateMachine.defaultCCCenter, stateMachine.defaultCCRadius, stateMachine.defaultCCHeight);
    }

    private void UpdateAnimator()
    {
        stateMachine.animator.SetMovementSpeed(stateMachine.animationBlend);
        stateMachine.animator.SetMotionSpeed(inputMagnitude);
    }
}
