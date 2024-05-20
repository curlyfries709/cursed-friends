using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAmbushState : EnemyBaseState
{
    public EnemyAmbushState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    Coroutine currentCoroutine = null;
    bool waiting = false;

    public override void EnterState()
    {
        stateMachine.AttemptAmbush += AttemptAmbush;
        EnemyTacticsManager.Instance.IsChasingPlayer(stateMachine, false);

        Stop();

        OnEnterStateConfig(true, 0);

        stateMachine.navMeshAgent.speed = stateMachine.ChaseSpeed();
        stateMachine.navMeshAgent.stoppingDistance = stateMachine.defaultStoppingDistance;

        stateMachine.knowsPlayerPos = true;
        waiting = false;
    }

    public override void UpdateState()
    {
        if (!stateMachine.IsReadyToChase()) { return; }

        stateMachine.navMeshAgent.isStopped = false;
        stateMachine.navMeshAgent.SetDestination(stateMachine.ambushPos);

        stateMachine.animator.SetSpeed(stateMachine.navMeshAgent.velocity.magnitude);

        if (HasArrivedAtDestination())
        {
            //Rotate to target
            stateMachine.navMeshAgent.updateRotation = false;
            RotateToPos(stateMachine.playerStateMachine.transform.position);
            LookoutForSuspiciousActivity();

            if (!waiting)
            {
                currentCoroutine = stateMachine.StartCoroutine(WaitRoutine());
            }
        }
        else
        {
            if (CanSeeSuspiciousTarget() && CanInterceptPlayer())
            {
                stateMachine.SwitchState(stateMachine.chasingState);
            }
            stateMachine.navMeshAgent.updateRotation = true;
        }
    }

    public override void ExitState()
    {
        if (currentCoroutine != null)
        {
            stateMachine.StopCoroutine(currentCoroutine);
        }

        EnemyTacticsManager.Instance.AbadoningAmbushPoint(stateMachine.assignedAmbushPoint, stateMachine);

        stateMachine.AttemptAmbush -= AttemptAmbush;
        stateMachine.navMeshAgent.updateRotation = true;
    }

    private void AttemptAmbush()
    {
        if (CanInterceptPlayer())
        {
            stateMachine.attemptingToIntercept = true;
            stateMachine.SwitchState(stateMachine.chasingState);
        }
    }

    private bool CanInterceptPlayer()
    {
        //Is Enemy In Front Of Player
        Vector3 dir = (transform.position - stateMachine.playerStateMachine.transform.position).normalized;
        float result = Vector3.Dot(stateMachine.playerStateMachine.transform.TransformDirection(Vector3.forward), dir);

        //Means We are behind Player.
        if (result < 0)
        {
            return false;
        }

        //ChaserPosition - RunnerPosition;
        Vector3 vectorFromRunner = transform.position - stateMachine.playerLastKnownPos;
        float distanceToRunner = Vector3.Distance(stateMachine.playerLastKnownPos, transform.position);
        float runnerSpeed = stateMachine.playerStateMachine.GetSpeed();

        float mySpeed = stateMachine.ChaseSpeed();

        // Check- Is the Runner not moving? If it isn't, the calcs don't work because
        // we can't use the Law of Cosines
        if (runnerSpeed < 1)
        {
            return false;
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
            return false;
        }

        if (t1 < 0 && t2 < 0)
        {
            // Both values for t are negative, so the interception would have to have
            // occured in the past
            return false;
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

        if (timeToInterception > EnemyTacticsManager.Instance.maxInterceptionTime)
        {
            //Too Long
            return false;
        }

        return true;
    }

    IEnumerator WaitRoutine()
    {
        waiting = true;

        yield return new WaitForSeconds(EnemyTacticsManager.Instance.maxAmbushWaitTime);

        stateMachine.SwitchState(stateMachine.retreatState);
    }

}
