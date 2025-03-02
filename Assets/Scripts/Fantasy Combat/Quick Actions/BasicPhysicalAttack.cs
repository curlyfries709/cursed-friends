

public class BasicPhysicalAttack : PlayerOffensiveSkill
{
    //Trigger Skill Logic

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


}
