using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffensiveChainAttack : PlayerBaseChainAttack
{
    public override bool TryTriggerSkill()
    {
        if (CanTriggerSkill(true))
        {
            knockdownEvent.Flash();

            BeginSkill(offensiveSkillData.returnToGridPosTime, offensiveSkillData.delayBeforeReturn, true);//Unit Position Updated here

            ActivateVisuals(true);

            //Attack
            Attack(); //Damage Target Called in here where Can Evade is false.

            return true;
        }

        return false;
    }

}
