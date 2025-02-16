using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySussingState : EnemyBaseState
{
    public EnemySussingState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    Vector3 rotDestination;
    float sussmometer = 0;

    public override void EnterState()
    {
        Stop();

        OnEnterStateConfig(false, 0);

        sussmometer = 0;

        stateMachine.animator.SetSearching(true);

        rotDestination = stateMachine.GetPlayerStateMachine().transform.position;
    }

    public override void UpdateState()
    {
        stateMachine.animator.SetSpeed(stateMachine.navMeshAgent.velocity.magnitude);
        SussingPlayer();
    }

    public override void ExitState()
    {
        stateMachine.sightDetectionUI.ResetSightDetection();
        stateMachine.animator.SetSearching(false);
    }

    private void SussingPlayer()
    {
        Collider targetSeen = CanSeeSuspiciousTarget();

        bool canSeeSussTarget = targetSeen != null;
        bool canSeePlayer = targetSeen && targetSeen.CompareTag("Player");

        //if (!canSeeSussTarget || (canSeeSussTarget && !canSeePlayer && !barrel.IsSuspicious()))
        if (!canSeeSussTarget)
        {
            //Decrease sussometer
            sussmometer = sussmometer - CalculateSussRate();
            sussmometer = Mathf.Max(sussmometer, 0);

            //When Empty return to Patrolling. 
            if (sussmometer == 0)
            {
                ReturnToPatrol(false);
                return;
            }
        }
        else
        {
            //If Player Exits Stealth mode
            if (!stateMachine.GetPlayerStateMachine().InStealth())
            {
                PlayerSpotted();
                return;
            }

            //Increase Sussmometer 
            sussmometer = sussmometer + CalculateSussmometerIncrement();
            sussmometer = Mathf.Min(sussmometer, 1f);

            //If At Maximum, Alert Allies which to chasing.
            if (sussmometer == 1)
            {
                if (canSeePlayer)
                {
                    PlayerSpotted();
                    return;
                }
                else
                {
                    stateMachine.SwitchState(stateMachine.investigateState);
                    return;
                }
            }
        }

        stateMachine.sightDetectionUI.UpdateSightDetection(sussmometer);
    }

    protected float CalculateSussmometerIncrement()
    {
        //Require Formula that includes ViewRadius, Distance & SussRate
        float playerDistFromEnemy = Vector3.Distance(transform.position, stateMachine.GetPlayerStateMachine().transform.position);
        playerDistFromEnemy = Mathf.Max(0.1f, playerDistFromEnemy);

        //0 Being Enemy Pos; 1 being View Radius Edge.
        float percentageOfViewRadius = playerDistFromEnemy / stateMachine.fieldOfView.viewRadius;

        //Sussmometer should increase faster when player is closer.
        //If At edge of viewRadius, increases at default Rate.
        return  (CalculateSussRate() / percentageOfViewRadius) * GameManager.Instance.GetDifficultySussmometerMultiplier();
    }

    private float CalculateSussRate()
    {
        return Time.deltaTime / stateMachine.completeDetectionTime;
    }

    protected void SlowlyRotateToSusPos()
    {
        Vector3 lookDir = (rotDestination - transform.position).normalized;

        if (lookDir != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookDir);

            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, lookRotation.eulerAngles.y, ref rotationVelocity, stateMachine.sussingRotateTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
    }
}
