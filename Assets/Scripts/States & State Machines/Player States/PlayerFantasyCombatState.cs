using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFantasyCombatState : PlayerBaseState
{
    public PlayerFantasyCombatState(PlayerStateMachine stateMachine) : base(stateMachine){}

    public override void EnterState()
    {
        EnterStateConfig(false, false, true, 1);

        FantasyCombatManager.Instance.CombatEnded += OnCombatEnd;
        stateMachine.moveRestrictor.enabled = false;
        stateMachine.ActivateFreeRoamComponents(false);

        stateMachine.Sprint(false);
        stateMachine.SetProximityRadius(true);
    }

    public override void UpdateState()
    {
        
    }

    public override void ExitState()
    {
        stateMachine.SetProximityRadius(false);
        FantasyCombatManager.Instance.CombatEnded -= OnCombatEnd;

        ExitStateConfig(false);
    }

    private void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if ((battleResult == BattleResult.Victory || battleResult == BattleResult.Fled) && battleTrigger.CanPlayerReturnToFreeRoam())
        {
            //DisableMovement(false);
            stateMachine.moveRestrictor.enabled = true;
            stateMachine.SwitchState(stateMachine.fantasyRoamState);
        }
    }
}
