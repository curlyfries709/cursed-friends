using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EnemyMeleeAttack : AIOffensiveSkill
{
    public override void TriggerSkill()
    {
        if (!CanTriggerSkill()) { return; }

        BeginSkill(returnToGridPosTime, delayBeforeReturn);//Unit Position Updated here
     
        ActivateActionCamList(true);
        Attack();
    }
}
