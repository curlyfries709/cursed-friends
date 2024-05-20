using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class RangedCounter : CounterAttack
{
    [Title("ATTACK DATA")]
    [SerializeField] Transform projectileDestination;
    [Space(10)]
    [SerializeField] float delayBeforeReturn = 0.1f;

    CharacterGridUnit target = null;

    public override void TriggerCounterAttack(CharacterGridUnit target)
    {
        this.target = target;

        //Set Times
        myUnit.returnToGridPosTime = 0;
        myUnit.delayBeforeReturn = delayBeforeReturn;

        DealDamage(target);

        //Wait to Attack
        StartCoroutine(WaitThenAttack());
    }

    IEnumerator WaitThenAttack()
    {
        if (projectileDestination)
            projectileDestination.transform.position = target.camFollowTarget.position;

        yield return new WaitForSeconds(Evade.Instance.GetCounterCanvasDisplayTime());
        PlayCounterattackAnimation();
    }

}
