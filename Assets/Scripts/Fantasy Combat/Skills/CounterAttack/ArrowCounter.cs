using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ArrowCounter : CounterAttack
{
    [Header("ATTACK ANIMATION DATA")]
    [Space(10)]
    [Header("Timers")]
    [SerializeField] float arrowFlightTime = 0.06f;
    [SerializeField] float delayBeforeReturn = 0.1f;
    [SerializeField] float returnToGridPosTime = 0.5f;
    [Header("Arrow")]
    [SerializeField] GameObject arrowInHand;
    [SerializeField] GameObject arrowPrefab;

    CharacterGridUnit target = null;
    GridSystem<GridObject> gridSystem;

    protected override void Awake()
    {
        base.Awake();
        gridSystem = LevelGrid.Instance.gridSystem;
    }

    public override void TriggerCounterAttack(CharacterGridUnit target)
    {
        this.target = target;

        //Set Times
        myUnit.returnToGridPosTime = returnToGridPosTime;
        myUnit.delayBeforeReturn = delayBeforeReturn;

        DealDamage(target);

        //Begin Counter
        myUnit.unitAnimator.SetupArrowAttackEvent(EnableArrowInHand, ShootArrow);
        //StartCoroutine(CounterRoutine());
        myUnit.unitAnimator.Counter();
    }

    IEnumerator CounterRoutine()
    {
        myUnit.unitAnimator.Counter();
        //myUnit.unitAnimator.ActivateSlowmo();
        yield return new WaitForSeconds(Evade.Instance.GetCounterCanvasDisplayTime());
        //myUnit.unitAnimator.ReturnToNormalSpeed();
    }

    private void EnableArrowInHand()
    {
        arrowInHand.SetActive(true);
    }

    private void ShootArrow()
    {
        //Vector3 center = target.GetColliderCenter();
        Vector3 center = arrowInHand.transform.position;
        center.x = 0;
        center.z = 0;

        Vector3 destinationWithOffset = target.GetClosestPointOnColliderToPosition(gridSystem.GetWorldPosition(myUnit.GetCurrentGridPositions()[0])) + center;

        arrowInHand.SetActive(false);
        GameObject arrow = Instantiate(arrowPrefab, arrowInHand.transform.position, arrowInHand.transform.rotation);
        arrow.transform.DOMove(destinationWithOffset, arrowFlightTime).OnComplete(() => DestroyArrow(arrow));
    }

    private void DestroyArrow(GameObject arrow)
    {
        //Spawn Arrow Hit Effect.
        arrow.transform.GetChild(0).parent = null;
        Destroy(arrow);
    }
}
