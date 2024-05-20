using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyGuardPointState : EnemyBaseState
{
    public EnemyGuardPointState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        OnEnterGuardPointState();
    }

    public override void UpdateState()
    {
        LookoutForSuspiciousActivity();
        MoveToGuardPoint();
    }

    public override void ExitState()
    {
        stateMachine.navMeshAgent.updateRotation = true;
    }
    
}
