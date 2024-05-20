using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyIdleState : EnemyBaseState
{
    public EnemyIdleState(EnemyStateMachine stateMachine) : base(stateMachine) { }
    Coroutine currentCoroutine;

    public override void EnterState()
    {
        OnEnterStateConfig(false, 0);
        currentCoroutine = stateMachine.StartCoroutine(IdleRoutine());

    }

    public override void UpdateState()
    {
        LookoutForSuspiciousActivity();
        stateMachine.animator.SetSpeed(stateMachine.navMeshAgent.velocity.magnitude);
    }

    public override void ExitState()
    {
        stateMachine.StopCoroutine(currentCoroutine);
    }

    IEnumerator IdleRoutine()
    {
        float waitTime = stateMachine.GetRandomIdleTime();

        yield return new WaitForSeconds(waitTime);

        ReturnToPatrol(false);
    }

}
