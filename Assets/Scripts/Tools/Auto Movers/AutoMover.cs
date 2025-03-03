using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using System;

public class AutoMover : MonoBehaviour
{
    [Title("Notification")]
    [Tooltip("If false, callback will be triggered immediately")]
    [SerializeField] bool triggerCallbackOnComplete = false;
    [Title("Movement")]
    [SerializeField] float movementDuration = 0.5f;
    [Space(10)]
    [SerializeField] Ease movementEaseType = Ease.Linear;
    [Title("Rotate")]
    [SerializeField] bool doRotate = false;
    [ShowIf("doRotate")]
    [SerializeField] float rotationDuration = 0.5f;
    [Space(10)]
    [ShowIf("doRotate")]
    [SerializeField] Ease rotationEaseType = Ease.Linear;
    [Title("Jumping")]
    [Tooltip("If true, then movement will become jumping and will use movement duration and ease type")]
    [SerializeField] bool doJump = false;
    [Space(10)]
    [ShowIf("doJump")]
    [Tooltip("Power of the jump (the max height of the jump is represented by this plus the final Y offset).")]
    [SerializeField] float jumpPower = 1;
    [ShowIf("doJump")]
    [Tooltip("Total number of jumps.")]
    [SerializeField] int numOfJumps = 1;
    [Title("Sequence")]
    [SerializeField] AutoMover nextInSequenceMover;

    public void PlayMovement(CharacterGridUnit character, Transform transformToMove, Action OnCompleteCallback)
    {
        Vector3 destinationPosition = transform.position;
        Vector3 destinationRotation = transform.rotation.eulerAngles;

        if (doJump)
        {
            DoJump(transformToMove, destinationPosition);
        }
        else
        {
            DoMovement(transformToMove, destinationPosition);
        }

        if (doRotate)
        {
            DoRotation(transformToMove, destinationRotation);
        }


        if (!triggerCallbackOnComplete)
        {
            OnCompleteCallback?.Invoke();
            return;
        }

        //Otherwise, trigger callback after max tween duration.
        if (!doJump)
        {
            character?.unitAnimator.SetMovementSpeed(character.moveSpeed);
        }

        float waitTime = MathF.Max(doRotate ? rotationDuration : 0, movementDuration);
        StartCoroutine(TriggerCallback(character, transformToMove, waitTime, OnCompleteCallback));
    }

    private void DoMovement(Transform transformToMove, Vector3 destinationPosition)
    {
        transformToMove.DOMove(destinationPosition, movementDuration).SetEase(movementEaseType);
    }

    private void DoJump(Transform transformToMove, Vector3 destinationPosition)
    {
        transformToMove.DOJump(destinationPosition, jumpPower, numOfJumps, movementDuration).SetEase(movementEaseType);
    }

    private void DoRotation(Transform transformToMove, Vector3 destinationRotation)
    {
        transformToMove.DORotate(destinationRotation, rotationDuration).SetEase(rotationEaseType);
    }

    IEnumerator TriggerCallback(CharacterGridUnit character, Transform transformToMove, float waitTime, Action OnCompleteCallback)
    {
        yield return new WaitForSeconds(waitTime);

        character?.unitAnimator.SetMovementSpeed(0);

        if (nextInSequenceMover)
        {
            nextInSequenceMover.PlayMovement(character, transformToMove, OnCompleteCallback);
        }
        else
        {
            OnCompleteCallback();
        }      
    }

    public void SetCallbackOnComplete(bool newValue)
    {
        triggerCallbackOnComplete = newValue;
    }

    public void SetMovementDuration(float newDuration)
    {
        movementDuration = newDuration;
    }
}
