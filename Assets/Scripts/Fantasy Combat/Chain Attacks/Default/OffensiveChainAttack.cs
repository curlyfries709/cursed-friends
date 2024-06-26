using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffensiveChainAttack : PlayerBaseChainAttack
{
    public override void SkillSelected()
    {
        if (!skillTriggered)
        {
            GridVisual();
        }
    }

    public override bool TryTriggerSkill()
    {
        if (CanTriggerSkill(true))
        {
            knockdownEvent.Flash();

            BeginAction(returnToGridPosTime, delayBeforeReturn, true);//Unit Position Updated here

            ActivateVisuals(true);

            //Attack
            Attack(); //Damage Target Called in here where Can Evade is false.

            return true;
        }

        return false;
    }

}
