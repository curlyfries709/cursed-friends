using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ArrowAttack : PlayerOffensiveSkill
{
    [Header("ARROW DATA")]
    [SerializeField] float arrowFlightTime = 0.06f;
    [Header("Prefabs")]
    [SerializeField] GameObject arrowInHand;
    [SerializeField] GameObject arrowPrefab;


    //Trigger Skill Logic

    public override bool TryTriggerSkill()
    {
        if (CanTriggerSkill(true))
        {
            BeginSkill(returnToGridPosTime, delayBeforeReturn, true);//Unit Position Updated here

            //Since this only targets one unit
            DamageTarget(skillTargets[0]);

            //Begin Attack
            myCharacter.unitAnimator.SetupArrowAttackEvent(EnableArrowInHand, ShootArrow);
            ActivateVisuals(true);
            myCharacter.unitAnimator.TriggerSkill(skillName);
            

            return true;
        }
        else
        {
            return false;
        }
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

        Vector3 destinationWithOffset = skillTargets[0].GetClosestPointOnColliderToPosition(LevelGrid.Instance.gridSystem.GetWorldPosition(myUnit.GetCurrentGridPositions()[0])) + center;

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
