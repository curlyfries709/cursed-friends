using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnotherRealm;

public abstract class EnemyBaseState : State
{
    protected EnemyStateMachine stateMachine;
    protected Transform transform;
    protected SneakBarrel barrel;


    protected float rotationVelocity;


    protected EnemyBaseState(EnemyStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        transform = stateMachine.transform;
        barrel = stateMachine.barrel;
    }

    protected void OnEnterStateConfig(bool showWeapon, int animatorLayer)
    {
        stateMachine.ShowWeapon(showWeapon);
        stateMachine.animator.ChangeLayers(animatorLayer);
    }



    protected void LookoutForSuspiciousActivity()
    {
        if (!stateMachine.IsHostile()) { return; }

        Collider sussTarget = CanSeeSuspiciousTarget();

        if (sussTarget && sussTarget.CompareTag("Player"))
        {
            if (stateMachine.GetPlayerStateMachine().InStealth() && !stateMachine.ignorePlayerStealth)
            {
                stateMachine.SwitchState(stateMachine.sussState);
            }
            else
            {
                PlayerSpotted();
            }
        }
        else if (sussTarget && sussTarget.CompareTag("Barrel"))
        {
            if (barrel.IsSuspicious())
            {
                stateMachine.SwitchState(stateMachine.sussState);
            }
        }
    }

    protected void PlayerSpotted()
    {
        if (FantasyCombatManager.Instance.InCombat())
        {
            //Debug.Log("Enemy Joined Battle By Spotting Player");
            //stateMachine.JoinBattle();
            return;
        }

        AlertNearbyAllies(stateMachine.GetPlayerStateMachine().transform.position, stateMachine.GetPlayerStateMachine().transform.rotation);
        stateMachine.SwitchState(stateMachine.chasingState);
    }

    protected bool CanAttackAndMove()
    {
        if (!stateMachine.canMoveWhilstAttacking || stateMachine.GetPlayerStateMachine().controller.velocity.magnitude == 0)
        {
            return false;
        }

        //If Behind Player Always Move & Attack
        Vector3 toTarget = (stateMachine.GetPlayerStateMachine().transform.position - transform.position).normalized;
        float result = Vector3.Dot(toTarget, stateMachine.GetPlayerStateMachine().transform.forward);

        if (result > 0)
        {
            return true;
        }

        //If Ahead, Only if slower than player
        return stateMachine.navMeshAgent.velocity.magnitude < stateMachine.GetPlayerStateMachine().controller.velocity.magnitude;
    }
    

    protected void AlertNearbyAllies(Vector3 targetPos, Quaternion targetRot)
    {
        //When Should Not Contact: Going from attacking to chasing. Just been alerted by an ally.
        //When SHould Contact: During Retreating State and regained location of player. Spotted Player during Idle, Patrolling, Suss, Or Retreating.
        stateMachine.sightDetectionUI.Alert();

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, stateMachine.alertAlliesRadius);

        foreach (Collider collider in hitColliders)
        {
            if(collider.TryGetComponent(out EnemyStateMachine ally))
            {
                ally.Alert(targetPos, targetRot);
            }
        }
    }

    protected Collider CanSeeSuspiciousTarget()
    {
        return stateMachine.fieldOfView.CanSeeSupiciousTarget(stateMachine.GetPlayerStateMachine().controller);
    }
    //Guard Point States
    protected void OnEnterGuardPointState()
    {
        OnEnterStateConfig(false, 0);
        stateMachine.SetHostileComponents(stateMachine.isHostile);

        stateMachine.navMeshAgent.enabled = true;
        stateMachine.navMeshAgent.speed = stateMachine.walkSpeed;
        stateMachine.navMeshAgent.isStopped = false;
        stateMachine.navMeshAgent.stoppingDistance = stateMachine.defaultStoppingDistance;

        if (!stateMachine.patrolRoute) { return; }

        Vector3 destination = stateMachine.patrolRoute.childCount > 0 ? stateMachine.patrolRoute.GetChild(0).position : transform.position;
        GoToGuardPoint(destination);
    }

    protected void GoToGuardPoint(Vector3 destination)
    {
        /*if (Vector3.Distance(transform.position, destination) > stateMachine.navMeshAgent.stoppingDistance)
        {
            
        }*/

        stateMachine.navMeshAgent.SetDestination(destination);
    }

    protected void MoveToGuardPoint()
    {
        stateMachine.animator.SetMovementSpeed(stateMachine.navMeshAgent.velocity.magnitude);

        if (!stateMachine.patrolRoute) { return; }

        if (HasArrivedAtDestination())
        {
            //Rotate to target
            stateMachine.navMeshAgent.updateRotation = false;

            if(stateMachine.patrolRoute)
                InheritTargetRotation(stateMachine.patrolRoute.GetChild(0));

            if (stateMachine.idleStateType != EnemyStateMachine.EnemyIdleStateType.Idle)
                SwitchToGuardPointIdleState();
        }
        else
        {
            stateMachine.navMeshAgent.updateRotation = true;
        }
    }

    public void SwitchToGuardPointIdleState()
    {
        switch (stateMachine.idleStateType)
        {
            case EnemyStateMachine.EnemyIdleStateType.Idle:
                stateMachine.SwitchState(stateMachine.guardPointState);
                break;
            case EnemyStateMachine.EnemyIdleStateType.Sleeping:
                stateMachine.SwitchState(stateMachine.sleepingState);
                break;
            default:
                stateMachine.SwitchState(stateMachine.guardPointState);
                break;
        }
    }

    //Others
    protected bool QuadraticSolver(float a, float b, float c, out float t1, out float t2)
    {
        float sqrtpart = (b * b) - (4 * a * c);

        if (sqrtpart < 0)
        {
            t1 = 0;
            t2 = 0;

            return false;
        }

        t1 = ((-1) * b + Mathf.Sqrt(sqrtpart)) / (2 * a);
        t2 = ((-1) * b - Mathf.Sqrt(sqrtpart)) / (2 * a);

        return true;
    }

    protected float CalculateStoppingDistance(bool setStoppingDistanceToo, bool clampStoppingDistance = true)
    {
       float playerSpeed = stateMachine.GetPlayerStateMachine().GetSpeed();

        /*Vector3 playerVelocity = stateMachine.playerStateMachine.controller.velocity;
        Vector3 playerPredictedPos = stateMachine.playerStateMachine.transform.position + (playerVelocity * stateMachine.attackHitBoxActivationTime);

        Vector3 currentDir = stateMachine.navMeshAgent.velocity.normalized;
        Vector3 destination = playerPredictedPos - currentDir * stateMachine.maxAttackRange;



        //Vector3 destination = transform.position + (myDir * pointOnMyDir);
        float stoppingDistance = Vector3.Distance(destination, transform.position);*/
        

        //Speed * Time = Distance
        float distancePlayerHasMovedFromAttackStart = playerSpeed * stateMachine.attackHitBoxActivationTime; //8 * 0.35 = 2.8f 4*0.35 = 1.4f
        float angle = Vector3.Angle(transform.forward, stateMachine.GetPlayerStateMachine().transform.forward);

        float attackRange = stateMachine.maxAttackRange; //(stateMachine.minAttackRange + stateMachine.maxAttackRange) * 0.5f;
        float stoppingDistance;

        stoppingDistance = attackRange - (distancePlayerHasMovedFromAttackStart * Mathf.Cos(angle * Mathf.Deg2Rad));

        if (clampStoppingDistance)
        {
            stoppingDistance = Mathf.Max(stoppingDistance, 0.85f);
        }
        
        if (setStoppingDistanceToo)
        {
            stateMachine.navMeshAgent.stoppingDistance = stoppingDistance;
        }
        return stoppingDistance;
    }

    protected bool IsPredictedAttackDistanceInRange()
    {
        if (CanAttackAndMove())
        {
            return Vector3.Distance(transform.position, stateMachine.GetPlayerStateMachine().transform.position) <= stateMachine.maxAttackRange;
        }

        Vector3 pointOnController = stateMachine.GetPlayerStateMachine().controller.ClosestPointOnBounds(transform.position);
        Vector3 playerVelocity = stateMachine.GetPlayerStateMachine().controller.velocity;
        Vector3 playerPredictedPos = pointOnController + (playerVelocity * stateMachine.attackHitBoxActivationTime);

        return Vector3.Distance(transform.position, playerPredictedPos) <= stateMachine.maxAttackRange;
    }

    protected bool ShouldMoveToAttack()
    {
        float clampedStoppingDistance = 0.85f;
        float stoppingDistance = CalculateStoppingDistance(false, false);

        return clampedStoppingDistance > stoppingDistance;
    }

    protected void ReturnToPatrol(bool startAtClosestWaypoint)
    {
        stateMachine.continuePatrol = !startAtClosestWaypoint;

        if (stateMachine.patrolRoute.childCount == 1)
        {
            stateMachine.SwitchState(stateMachine.guardPointState);
        }
        else
        {
            stateMachine.SwitchState(stateMachine.patrollingState);
        }
    }



    protected bool HasArrivedAtDestination()
    {
        return CombatFunctions.HasAgentArrivedAtDestination(stateMachine.navMeshAgent);
    }

    protected void UpdatePlayerLastKnownData(Vector3 pos, Quaternion rot)
    {
        stateMachine.playerLastKnownPos = pos;
        stateMachine.playerLastKnownRot = rot;
    }

    //Rotation Methods
    protected void InheritTargetRotation(Transform target)
    {
        float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, target.eulerAngles.y, ref rotationVelocity, stateMachine.RotationSmoothTime);
        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
    }

    protected void InheritTargetRotation(Quaternion target)
    {
        float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, target.eulerAngles.y, ref rotationVelocity, stateMachine.RotationSmoothTime);
        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
    }

    protected void RotateToPos(Vector3 targetPos)
    {
        Vector3 lookDir = (targetPos - transform.position).normalized;

        if (lookDir != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookDir);

            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, lookRotation.eulerAngles.y, ref rotationVelocity, stateMachine.RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
    }

    protected void Stop()
    {
        stateMachine.navMeshAgent.ResetPath();
        stateMachine.navMeshAgent.isStopped = true;
    }


}
