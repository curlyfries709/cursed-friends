using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;

public class EnemyCombatMovement : CombatAction
{
    public static EnemyCombatMovement Instance { get; private set; }

    //CACHE
    CharacterGridUnit activeEnemy = null;
    EnemyCombatActingState activeActingState = null;
    List<Vector3> currentMoveList;

    int currentPositionIndex = 0;
    float speedChangeRate;
    float rotationSmoothTime;
    float rotationVelocity;

    float currentSpeed;
    float animationBlend;

    Vector3 finalLookDirection;
    float stoppingDistance;

    bool moved = false;
    bool movementComplete = false;
    bool finishedFinalRotation = false;

    //Event
    public Action<CharacterGridUnit /*MovingEnemy */, List<GridPosition> /* Move List*/> NewEnemyTraversingPath;

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
    }

    public void BeginMovement(CharacterGridUnit enemy, EnemyCombatActingState actingState, 
        List<GridPosition> moveListInGridPos, List<Vector3> moveList, Vector3 finalLookDirection, 
        float speedChangeRate, float rotationSmoothTime, float stoppingDistance)
    {
        activeEnemy = enemy;
        activeActingState = actingState;
        currentMoveList = moveList;

        this.speedChangeRate = speedChangeRate;
        this.rotationSmoothTime = rotationSmoothTime;
        this.finalLookDirection = finalLookDirection;
        this.stoppingDistance = stoppingDistance;

        NewEnemyTraversingPath?.Invoke(enemy, moveListInGridPos);
        enabled = true;

        BeginAction();
    }

    private void Update()
    {
        MoveToGridPos();
    }

    private void MoveToGridPos()
    {
        if (movementComplete || !activeEnemy) { return; }

        Vector3 targetPosition = currentMoveList[currentPositionIndex];
        targetPosition.y = activeEnemy.transform.position.y;

        Vector3 moveDirection = (targetPosition - activeEnemy.transform.position).normalized;
        float targetSpeed = activeEnemy.moveSpeed;

        if (Vector3.Distance(activeEnemy.transform.position, targetPosition) > stoppingDistance)
        {
            moved = true;

            float targetRotation = Quaternion.LookRotation(moveDirection).eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(activeEnemy.transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime);

            activeEnemy.transform.position += moveDirection * currentSpeed * Time.deltaTime;
            activeEnemy.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
        else
        {
            targetSpeed = 0;
            currentPositionIndex++;

            if (currentPositionIndex >= currentMoveList.Count && !movementComplete)
            {      
                if (finalLookDirection != Vector3.zero && !finishedFinalRotation) //This usually occurs when unit already at destination but needs to rotate.
                {
                    //TODO: Extract this to own function which triggers OnArrivedAtDesination to avoid unnecessary multiple calls of DORotate.
                    Vector3 targetRotation = Quaternion.LookRotation(finalLookDirection).eulerAngles;
                    activeEnemy.transform.DORotate(targetRotation, rotationSmoothTime).OnComplete(() => 
                    {
                        if (activeEnemy)
                        {
                            finishedFinalRotation = true;
                        }
                    });
                }
                else
                {
                    movementComplete = true;
                    currentPositionIndex = currentMoveList.Count - 1;
                    OnArrivedAtDestination(moved, targetPosition);
                    return;
                }
            }

            currentPositionIndex = Mathf.Min(currentPositionIndex, currentMoveList.Count - 1);
        }

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * speedChangeRate);
        // round speed to 3 decimal places
        currentSpeed = Mathf.Round(currentSpeed * 1000f) / 1000f;

        animationBlend = Mathf.Lerp(animationBlend, targetSpeed, Time.deltaTime * speedChangeRate);
        if (animationBlend < 0.01f) animationBlend = 0f;


        // update unit animator
        activeEnemy.unitAnimator.SetMovementSpeed(animationBlend);

        for (int i = 0; i < currentMoveList.Count - 1; i++)
        {
            Debug.DrawLine(currentMoveList[i], currentMoveList[i + 1], Color.red, Time.deltaTime);
        }
    }

    private void OnArrivedAtDestination(bool didMove, Vector3 destination)
    {
        EnemyCombatActingState actingState = activeActingState; //Cache Before Resetting

        ResetData();

        bool didTriggerSkill = actingState.OnArrivedAtDestination(didMove, destination);

        if (!didTriggerSkill)
        {
            EndAction();
        }
    }

    protected override void ResetData()
    {
        base.ResetData();

        activeEnemy = null;
        activeActingState = null;
        currentMoveList.Clear();

        currentPositionIndex = 0;

        moved = false;
        finishedFinalRotation = false;
        movementComplete = false;

        finalLookDirection = Vector3.zero;

        enabled = false;
    }

    protected override bool ListenForUnitHealthUIComplete()
    {
        return false;
    }
}
