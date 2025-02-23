using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BasicPhysicalAttack : PlayerOffensiveSkill
{
    public override void SkillSelected()
    {
        if (!skillTriggered)
        {
            GridVisual();
        }
    }
    public override void SkillCancelled()
    {
        //Play SFX
        AudioManager.Instance.PlaySFX(SFXType.TabBack);

        if (costType == SkillCostType.Free)
        {
            //Means it's a basic attack
            FantasyCombatManager.Instance.ShowActionMenu(true);
        }
        else
        {
            FantasyCombatCollectionManager.MenuSkillCancelled(player);
        }

        //Contact Grid Visual To Reset the grid to Movement Only. 
        HideSelectedSkillGridVisual();
    }

    //Trigger Skill Logic

    public override bool TryTriggerSkill()
    {
        if (CanTriggerSkill(true))
        {
            BeginSkill(returnToGridPosTime, delayBeforeReturn, true);//Unit Position Updated here

            ActivateVisuals(true);

            //Attack
            Attack(); //Damage Target Called in here.

            return true;
        }
        else
        {
            return false;
        }
    }


}
