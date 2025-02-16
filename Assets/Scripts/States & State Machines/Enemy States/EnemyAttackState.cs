using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackState : EnemyBaseState
{
    public EnemyAttackState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    float timeOfLastAttack = -1f;

    public override void EnterState()
    {
        stateMachine.navMeshAgent.stoppingDistance = stateMachine.defaultStoppingDistance;
        stateMachine.navMeshAgent.updateRotation = false;
        stateMachine.knowsPlayerPos = true;

        if (!CanAttackAndMove())
        {
            Stop();
            stateMachine.navMeshAgent.velocity = Vector3.zero;
        }
        else
        {
            stateMachine.navMeshAgent.autoBraking = false;
        }

        Attack();
    }

    public override void UpdateState()
    {
        stateMachine.navMeshAgent.isStopped = false;
        bool canMoveAndAttack = CanAttackAndMove();

        if (canMoveAndAttack)
        {
            stateMachine.navMeshAgent.speed = stateMachine.ChaseSpeed();
            stateMachine.navMeshAgent.destination = stateMachine.GetPlayerStateMachine().transform.position;
        }
        else if (IsTooClose())
        {
            stateMachine.navMeshAgent.velocity = Vector3.Lerp(stateMachine.navMeshAgent.velocity, -transform.forward * stateMachine.walkSpeed * 0.5f, stateMachine.SpeedChangeRate * Time.deltaTime);
        }
        else if(!canMoveAndAttack)
        {
            stateMachine.navMeshAgent.velocity = Vector3.zero;
        }

        stateMachine.animator.SetSpeed(stateMachine.navMeshAgent.velocity.magnitude);

        bool canSeePlayer = CanSeeSuspiciousTarget();

        RotateToPos(stateMachine.GetPlayerStateMachine().transform.position);

        if (canSeePlayer)
        {
            UpdatePlayerLastKnownData(stateMachine.GetPlayerStateMachine().transform.position, stateMachine.GetPlayerStateMachine().transform.rotation);
        }

        if (Time.time >= timeOfLastAttack + stateMachine.timeBetweenAttacks)
        {
            if (IsPredictedAttackDistanceInRange() && canSeePlayer)
            {
                Attack();
            }
            else
            {
                stateMachine.SwitchState(stateMachine.chasingState);
            }
        }

    }

    public override void ExitState()
    {
        //stateMachine.navMeshAgent.speed = stateMachine.walkSpeed;
        stateMachine.navMeshAgent.updateRotation = true;
    }

    private bool IsTooClose()
    {
        float dist = Vector3.Distance(stateMachine.GetPlayerStateMachine().transform.position, transform.position);
        bool tooClose = dist < stateMachine.minAttackRange;

        return tooClose && !ShouldMoveToAttack();
    }

   

    private void Attack()
    {
        if (!IsTooClose())
        {
            timeOfLastAttack = Time.time;
            stateMachine.animator.AttackIntruder();
        }

    }

}
