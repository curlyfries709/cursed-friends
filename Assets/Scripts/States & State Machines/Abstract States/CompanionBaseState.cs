using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CompanionBaseState : State
{
    protected CompanionStateMachine stateMachine;
    protected Transform transform;
    
    protected float randNum;
    

    protected static bool isPlayerSprinting = false;


    protected CompanionBaseState(CompanionStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        transform = stateMachine.transform;
    }

    protected void OnNewStateConfig(bool showWeapon, bool isSneaking, bool useNavMeshAgent, int animatorLayer)
    {
        //Objects
        stateMachine.ShowWeapon(showWeapon);

        CharacterAnimator animator = stateMachine.animator;

        //Animations
        animator.ChangeLayers(animatorLayer);
        animator.SetBool(animator.animIDStealth, isSneaking);

        //NavMeshAgent
        stateMachine.controller.enabled = !useNavMeshAgent;
        stateMachine.navMeshAgent.enabled = useNavMeshAgent;
        stateMachine.navMeshAgent.acceleration = stateMachine.followBehaviour.GetAcceleration(isSneaking);
    }

    protected void IsPlayerSprinting(bool isSprinting)
    {
        isPlayerSprinting = isSprinting;
    }

    protected void AlterRoamSpeed(Vector3 destination)
    {
        if (Vector3.Distance(destination, transform.position) >= stateMachine.followBehaviour.GetLaggingBehindDistance(false))
            {
                stateMachine.navMeshAgent.speed = isPlayerSprinting ? stateMachine.followBehaviour.GetLagSpeed(false) : stateMachine.runSpeed;
        }
        else if (Vector3.Distance(destination, transform.position) < 1f && stateMachine.playerStateMachine.moveValue == Vector2.zero)
        {
            stateMachine.navMeshAgent.speed = randNum;
        }
        else
        {
            stateMachine.navMeshAgent.speed = isPlayerSprinting ? stateMachine.runSpeed : stateMachine.walkSpeed;
        }
    }
    protected void AlterStealthSpeed(Vector3 destination)
    {
        if (Vector3.Distance(destination, transform.position) >= stateMachine.followBehaviour.GetLaggingBehindDistance(true))
        {
            stateMachine.navMeshAgent.speed = stateMachine.followBehaviour.GetLagSpeed(true);
        }
        else if (Vector3.Distance(destination, transform.position) < 1f && stateMachine.playerStateMachine.moveValue == Vector2.zero)
        {
            stateMachine.navMeshAgent.speed = randNum;
        }
        else
        {
            stateMachine.navMeshAgent.speed = stateMachine.sneakSpeed;
        }
    }

    protected Vector3 CalculateDestination()
    {
        Vector3 ccDir = stateMachine.playerStateMachine.controller.velocity.normalized;

        if(stateMachine.raiseSwapPosEventDesignee && stateMachine.previousCCDir != Vector3.zero && ccDir != Vector3.zero && Vector3.Angle(ccDir, stateMachine.previousCCDir) >= 100)
        {
            PlayerSpawnerManager.Instance.SwapCompanionPositionsEvent?.Invoke();
        }

        if (ccDir != Vector3.zero)
        {
            stateMachine.previousCCDir = ccDir;
        }

        return stateMachine.player.position + (stateMachine.player.right.normalized * stateMachine.horizontalFollowOffset)
            + (stateMachine.player.forward.normalized * stateMachine.verticalFollowOffset);
    }

    protected bool HasArrivedAtDestination()
    {

        if (!stateMachine.navMeshAgent.pathPending)
        {
            if (stateMachine.navMeshAgent.remainingDistance <= stateMachine.navMeshAgent.stoppingDistance)
            {
                if (!stateMachine.navMeshAgent.hasPath || stateMachine.navMeshAgent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
