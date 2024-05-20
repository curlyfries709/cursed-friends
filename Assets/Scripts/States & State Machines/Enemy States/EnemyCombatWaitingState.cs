using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombatWaitingState : EnemyBaseState
{
    public EnemyCombatWaitingState(EnemyStateMachine stateMachine) : base(stateMachine){}

    public override void EnterState()
    {
        OnEnterStateConfig(true, 1);
        DeactiveNonCombatComponents();
        
        stateMachine.myGridUnit.BeginTurn += OnTurnStart;
    }

    public override void UpdateState()
    {
        stateMachine.myGridUnit.unitAnimator.SetSpeed(0);
    }

    public override void ExitState()
    {
        stateMachine.myGridUnit.BeginTurn -= OnTurnStart;

    }

    //Turn Methods
    private void OnTurnStart()
    {
        if (!StatusEffectManager.Instance.IsUnitDisabled(stateMachine.myGridUnit))
        {
            stateMachine.StartCoroutine(BeginTurnRoutine());
        }
    }

    IEnumerator BeginTurnRoutine()
    {
        float waitTime = stateMachine.canGoAgain ? stateMachine.enemyAI.GetActDelay() : stateMachine.enemyAI.GetTurnStartDelay();

        yield return new WaitForSeconds(waitTime);
        stateMachine.SwitchState(stateMachine.enemyCombatActingState);
    }

    private void DeactiveNonCombatComponents()
    {
        stateMachine.navMeshAgent.enabled = false;
    }
}
