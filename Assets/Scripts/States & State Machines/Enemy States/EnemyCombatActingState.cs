using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyCombatActingState : EnemyBaseState
{
    public EnemyCombatActingState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    private List<Vector3> positionList = new List<Vector3>();
    private int currentPositionIndex = 0;
  
    private float currentSpeed;
    private float animationBlend;

    bool finishedMove = false;
    bool finishedFinalRotation = false;
    bool moved = false;
    bool isReadyToAct = false;

    public override void EnterState()
    {
        ResetData();
        stateMachine.enemyAI.BeginTurn(OnActionReady);
    }

    public override void UpdateState()
    {
        if (!isReadyToAct) return;

        if (!finishedMove)
        {
            MoveToGridPos();
        }
    }

    public override void ExitState()
    {
        stateMachine.canGoAgain = false;
    }

    //Methods
    private void OnActionReady(List<Vector3> movementPath)
    {
        positionList = movementPath;
        isReadyToAct = true;
    }

    private void MoveToGridPos()
    {
        Vector3 targetPosition = positionList[currentPositionIndex];
        targetPosition.y = stateMachine.myGridUnit.transform.position.y;

        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        float targetSpeed = stateMachine.myGridUnit.moveSpeed;

        if (Vector3.Distance(transform.position, targetPosition) > stateMachine.navMeshAgent.stoppingDistance)
        {
            moved = true;

            float targetRotation = Quaternion.LookRotation(moveDirection).eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, stateMachine.RotationSmoothTime);

            transform.position += moveDirection * currentSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
        else
        {
            targetSpeed = 0;
            currentPositionIndex++;

            if (currentPositionIndex >= positionList.Count && !finishedMove)
            {
                if (stateMachine.enemyAI.finalLookDirection != Vector3.zero && !finishedFinalRotation)
                {
                    Vector3 targetRotation = Quaternion.LookRotation(stateMachine.enemyAI.finalLookDirection).eulerAngles;
                    transform.DORotate(targetRotation, stateMachine.RotationSmoothTime).OnComplete(() => finishedFinalRotation = true);
                }
                else
                {
                    OnArrivedAtDestination(targetPosition);
                    currentPositionIndex = positionList.Count - 1;
                }
            }

            currentPositionIndex = Mathf.Min(currentPositionIndex, positionList.Count - 1);
        }

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * stateMachine.SpeedChangeRate);
        // round speed to 3 decimal places
        currentSpeed = Mathf.Round(currentSpeed * 1000f) / 1000f;

        animationBlend = Mathf.Lerp(animationBlend, targetSpeed, Time.deltaTime * stateMachine.SpeedChangeRate);
        if (animationBlend < 0.01f) animationBlend = 0f;


        // update unit animator
        stateMachine.myGridUnit.unitAnimator.SetMovementSpeed(animationBlend);

        for (int i = 0; i < positionList.Count - 1; i++)
        {
            Debug.DrawLine(positionList[i], positionList[i + 1], Color.red, Time.deltaTime);
        }
    }

    private void OnArrivedAtDestination(Vector3 destination)
    {
        if (finishedMove) { return; }

        if (stateMachine.enemyAI.selectedSkill)
        {
            //Trigger Skill.
            finishedMove = true;

            if (moved)
            {
                TriggerSkill();
            }
            else
            {
                stateMachine.StartCoroutine(ActDelay());
            }
        }
        else if(animationBlend < 0.01f)
        {
            transform.position = destination;

            finishedMove = true;
            stateMachine.myGridUnit.MovedToNewGridPos();

            stateMachine.EnemyFantasyCombatActionComplete();//Must Be Called before OnActionComplete.

            FantasyCombatManager.Instance.ActionComplete();
        }
    }

    private void TriggerSkill()
    {
        stateMachine.enemyAI.selectedSkill.TriggerSkill();
    }

    private void ResetData()
    {
        isReadyToAct = false;
        moved = false;
        finishedMove = false;
        finishedFinalRotation = false;

        currentPositionIndex = 0;
        currentSpeed = 0;
        animationBlend = 0;
    }

    IEnumerator ActDelay()
    {
        yield return new WaitForSeconds(stateMachine.enemyAI.GetActDelay());
        TriggerSkill();
    }
}
