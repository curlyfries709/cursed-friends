using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRetreatState : EnemyBaseState
{
    public EnemyRetreatState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    Coroutine currentCoroutine;

    public override void EnterState()
    {
        EnemyTacticsManager.Instance.IsChasingPlayer(stateMachine, false);

        stateMachine.knowsPlayerPos = false;

        Stop();

        stateMachine.navMeshAgent.speed = stateMachine.walkSpeed;
        stateMachine.animator.SetSearching(true);
        currentCoroutine = stateMachine.StartCoroutine(IdleRoutine());
    }

    public override void UpdateState()
    {
        LookoutForSuspiciousActivity();

        stateMachine.animator.SetMovementSpeed(stateMachine.navMeshAgent.velocity.magnitude);

        InheritTargetRotation(stateMachine.playerLastKnownRot);

        if (stateMachine.HasWeapon() && stateMachine.animator.IsWeaponSheathed())
        {
            ReturnToPatrol(true);
        }
    }

    public override void ExitState()
    {
        stateMachine.navMeshAgent.isStopped = false;

        stateMachine.StopCoroutine(currentCoroutine);
        stateMachine.animator.SetSearching(false);
        stateMachine.sightDetectionUI.Looking(false);
    }

    IEnumerator IdleRoutine()
    {
        stateMachine.sightDetectionUI.Looking(true);
        float waitTime = stateMachine.GetRandomLookTime();

        yield return new WaitForSeconds(waitTime);
        stateMachine.sightDetectionUI.Looking(false);

        if (stateMachine.HasWeapon())
        {
            stateMachine.animator.DrawWeapon(false);
        }
        else
        {
            ReturnToPatrol(true);
        }
    }
}
