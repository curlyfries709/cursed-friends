

public class BasicPhysicalAttack : PlayerOffensiveSkill
{
    public override void SkillCancelled(bool showActionMenu = true)
    {
        //Means it's a basic attack
        bool isCostFree = costType == SkillCostType.Free;

        base.SkillCancelled(isCostFree);

        if (!isCostFree)
        {
            FantasyCombatCollectionManager.MenuSkillCancelled(player);
        }
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
