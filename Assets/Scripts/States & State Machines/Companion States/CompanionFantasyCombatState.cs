using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompanionFantasyCombatState : CompanionBaseState
{
    public CompanionFantasyCombatState(CompanionStateMachine stateMachine) : base(stateMachine){}

    public override void EnterState()
    {
        OnNewStateConfig(true, false, false, 1);
        stateMachine.animator.SetSpeed(0);
        FantasyCombatManager.Instance.CombatEnded += OnCombatEnd;
    }

    public override void UpdateState()
    {

    }

    public override void ExitState()
    {
        FantasyCombatManager.Instance.CombatEnded -= OnCombatEnd;
    }

    private void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if ((battleResult == BattleResult.Victory || battleResult == BattleResult.Fled) && battleTrigger.CanPlayerReturnToFreeRoam())
        {
            stateMachine.moveRestrictor.enabled = false;
            stateMachine.SwitchState(stateMachine.followState);
        }
    }
}
