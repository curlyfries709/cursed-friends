using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyEmptyState : EnemyBaseState
{
    public EnemyEmptyState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        //Stop();
        stateMachine.navMeshAgent.enabled = false;
    }

    public override void UpdateState()
    {
        stateMachine.myGridUnit.unitAnimator.SetMovementSpeed(0);
    }

    public override void ExitState()
    {

    }

}
