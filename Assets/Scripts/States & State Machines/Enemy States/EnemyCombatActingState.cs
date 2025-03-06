using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyCombatActingState : EnemyBaseState
{
    public EnemyCombatActingState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        stateMachine.enemyAI.BeginTurn(OnActionReady);
    }

    public override void UpdateState(){}

    public override void ExitState()
    {
        stateMachine.canGoAgain = false;
    }

    //Methods
    private void OnActionReady(List<GridPosition> moveListInGridPos, List<Vector3> movementPath)
    {
        //Begin movement
        EnemyCombatMovement.Instance.BeginMovement(stateMachine.myGridUnit, this, moveListInGridPos, movementPath,
            stateMachine.enemyAI.finalLookDirection, stateMachine.SpeedChangeRate, stateMachine.RotationSmoothTime, stateMachine.navMeshAgent.stoppingDistance);
    }

    public bool OnArrivedAtDestination(bool moved, Vector3 destination)
    {
        if (stateMachine.enemyAI.selectedSkill)
        {
            //Trigger Skill.
            if (moved)
            {
                TriggerSkill();
            }
            else
            {
                stateMachine.StartCoroutine(ActDelay());
            }

            return true;
        }
        else 
        {
            transform.position = destination;

            stateMachine.myGridUnit.MovedToNewGridPos();

            stateMachine.EnemyFantasyCombatActionComplete();//Must Be Called before OnActionComplete.

            return false;
        }
    }

    private void TriggerSkill()
    {
        stateMachine.enemyAI.selectedSkill.TriggerSkill();
    }

    IEnumerator ActDelay()
    {
        yield return new WaitForSeconds(stateMachine.enemyAI.GetActDelay());
        TriggerSkill();
    }
}
