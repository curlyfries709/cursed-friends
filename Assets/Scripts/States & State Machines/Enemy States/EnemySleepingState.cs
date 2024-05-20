using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySleepingState : EnemyBaseState
{
    public EnemySleepingState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        stateMachine.hostileInteraction.ActivateSleepMode(true);
        stateMachine.animator.SetBool(stateMachine.animator.animIDSleeping, true);
    }

    public override void UpdateState()
    {
    }

    public override void ExitState()
    {
        stateMachine.hostileInteraction.ActivateSleepMode(false);
        stateMachine.animator.SetBool(stateMachine.animator.animIDSleeping, false);
        stateMachine.navMeshAgent.updateRotation = true;
    }

}
