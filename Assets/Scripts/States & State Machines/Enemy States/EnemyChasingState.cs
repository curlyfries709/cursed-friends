using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EnemyChasingState : EnemyBaseState
{
    public EnemyChasingState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    float timeOfLastChaseUpdate = -1f;
    float startChaseTime;
    float minChaseTime = 3f;

    bool isReady = false;
    bool attacking = false;

    Vector3 interceptionPos = Vector3.zero;

    public override void EnterState()
    {
        Stop();

        attacking = false;
        startChaseTime = Time.time;
        stateMachine.navMeshAgent.speed = stateMachine.ChaseSpeed();
        isReady = stateMachine.IsReadyToChase();

        EnemyTacticsManager.Instance.IsChasingPlayer(stateMachine, true);

        //stateMachine.navMeshAgent.updateRotation = false;
        if (!isReady)
        {
            //UnSheathe Weapon
            stateMachine.animator.DrawWeapon(true);
        }
        else
        {
            stateMachine.animator.SetTrigger(stateMachine.animator.animIDChase);
        }
    }

    public override void UpdateState()
    {
        bool canSeePlayer = CanSeeSuspiciousTarget();

        if (canSeePlayer)
        {
            UpdatePlayerLastKnownData(stateMachine.playerStateMachine.transform.position, stateMachine.playerStateMachine.transform.rotation);
        }

        stateMachine.animator.SetSpeed(stateMachine.navMeshAgent.velocity.magnitude);

        UpdateIsReady();

        if (!isReady) { return; }

        stateMachine.navMeshAgent.isStopped = false;
        UpdateAutoBrake();

        RotateToPos(stateMachine.navMeshAgent.steeringTarget);

        if (AttackCheck())
        {
            return;
        }

        if (Time.time > startChaseTime + minChaseTime && Vector3.Distance(stateMachine.playerLastKnownPos, transform.position) > stateMachine.distanceToGiveUpChasing)
        {
            stateMachine.knowsPlayerPos = false;
            EnemyTacticsManager.Instance.IsChasingPlayer(stateMachine, false);
            ReturnToPatrol(true);
            return;
        }
        if (canSeePlayer)
        {
            stateMachine.knowsPlayerPos = true;
            stateMachine.attemptingToIntercept = false;

            //if (Vector3.Distance(stateMachine.playerStateMachine.transform.position, transform.position) <= CalculateStoppingDistance(false))
            if (Time.time > timeOfLastChaseUpdate + stateMachine.chaseDestinationUpdateTime)
            {
                timeOfLastChaseUpdate = Time.time;
                
            }
            stateMachine.navMeshAgent.destination = GetChaseDestination();
        }
        else if (stateMachine.attemptingToIntercept)
        {
            stateMachine.knowsPlayerPos = false;

            if (Time.time > timeOfLastChaseUpdate + stateMachine.chaseDestinationUpdateTime)
            {
                timeOfLastChaseUpdate = Time.time;
                
            }
            stateMachine.navMeshAgent.destination = GetChaseDestination();

            if (HasArrivedAtDestination())
            {
                stateMachine.attemptingToIntercept = false;
            }
        }
        else
        {
            stateMachine.knowsPlayerPos = false;

            stateMachine.navMeshAgent.stoppingDistance = stateMachine.defaultStoppingDistance;

            stateMachine.navMeshAgent.SetDestination(stateMachine.playerLastKnownPos);

            if (HasArrivedAtDestination())
            {
                //Go To Retreating State
                stateMachine.SwitchState(stateMachine.retreatState);
            }
        }
    }

    public override void ExitState()
    {
        if (!attacking)
        {
            stateMachine.navMeshAgent.speed = stateMachine.walkSpeed;
            stateMachine.navMeshAgent.autoBraking = true;
        }
    }

    private void UpdateIsReady()
    {
        if (!isReady)
        {
            isReady = stateMachine.animator.IsWeaponDrawn();
        }
    }

    private void UpdateAutoBrake()
    {
        stateMachine.navMeshAgent.autoBraking = !CanAttackAndMove();
    }

    private bool AttackCheck()
    {
        if (IsPredictedAttackDistanceInRange())
        {
            //Go To Attack State.
            attacking = true;
            stateMachine.SwitchState(stateMachine.attackState);
            return true;
        }

        return false;
    }



    private Vector3 GetChaseDestination()
    {
        //ChaserPosition - RunnerPosition;
        Vector3 vectorFromRunner = transform.position - stateMachine.playerLastKnownPos;
        float distanceToRunner = Vector3.Distance(stateMachine.playerLastKnownPos, transform.position);
        float runnerSpeed = stateMachine.playerStateMachine.GetSpeed();

        float mySpeed = stateMachine.ChaseSpeed();

        // Check- Is the Runner not moving? If it isn't, the calcs don't work because
        // we can't use the Law of Cosines
        if (runnerSpeed < 1)
        {
            return stateMachine.playerLastKnownPos;
        }
        
        Vector3 runnerVelocity = stateMachine.playerStateMachine.controller.velocity;

        // Now set up the quadratic formula coefficients
        float a = (mySpeed * mySpeed) - (runnerSpeed * runnerSpeed);
        float b = 2 * Vector3.Dot(vectorFromRunner, runnerVelocity);
        float c = -distanceToRunner * distanceToRunner;

        float t1, t2;

        if (!QuadraticSolver(a, b, c, out t1, out t2))
        {
            // No real-valued solution, so no interception possible
            return stateMachine.playerLastKnownPos;
        }

        if (t1 < 0 && t2 < 0)
        {
            // Both values for t are negative, so the interception would have to have
            // occured in the past
            return stateMachine.playerLastKnownPos;
        }

        float timeToInterception;

        if (t1 > 0 && t2 > 0)
        {
            // Both are positive, take the smaller one
            timeToInterception = Mathf.Min((float)t1, (float)t2);
        }
        else
        {
            // One has to be negative, so take the larger one
            timeToInterception = Mathf.Max((float)t1, (float)t2);
        }

        if( timeToInterception > stateMachine.maxTimeToIntercept)
        {
            return stateMachine.playerLastKnownPos;
        }

        interceptionPos = stateMachine.playerLastKnownPos + runnerVelocity * timeToInterception;

        return interceptionPos;
    }


}
