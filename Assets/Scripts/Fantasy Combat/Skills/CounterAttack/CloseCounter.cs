using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

public class CloseCounter : CounterAttack
{
    [Title("ATTACK ANIMATION DATA")]
    [SerializeField] float animationAttackDistance = 1f;
    [Space(10)]
    [SerializeField] float delayBeforeReturn = 0.1f;
    [SerializeField] float returnToGridPosTime = 0.5f;

    CharacterGridUnit target = null;

    public override void TriggerCounterAttack(CharacterGridUnit target)
    {
        base.TriggerCounterAttack(target);

        this.target = target;

        //Set Times
        myUnit.returnToGridPosTime = returnToGridPosTime;
        myUnit.delayBeforeReturn = delayBeforeReturn;

        DealDamage(target);

        //Move Unit to Target & Attack
        MoveToTargetThenAttack();
    }

    private void MoveToTargetThenAttack()
    {
        GridSystem<GridObject> gridSystem = LevelGrid.Instance.gridSystem;

        Vector3 destinationWithOffset = target.GetClosestPointOnColliderToPosition(gridSystem.GetWorldPosition(myUnit.GetCurrentGridPositions()[0])) - (myUnitTransform.forward * animationAttackDistance);
        Vector3 destination = new Vector3(destinationWithOffset.x, myUnitTransform.position.y, destinationWithOffset.z);

        myUnit.unitAnimator.SetMovementSpeed(myUnit.moveSpeed);
        myUnit.unitAnimator.ActivateSlowmo();

        myUnit.transform.DOMove(destination, Evade.Instance.GetCounterCanvasDisplayTime()).OnComplete(() => PlayCounterattackAnimation());
    }
 

}
