
using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    public bool TryPurchaseSkill(SkillTreeNode skill)
    {
        PlayerTreeSkillData skillData = skill.GetSkillData();
        int skillunlockCost = skillData.unlockCost;

        bool canPurchaseSkill = CanAffordSkill(skill.GetTreeOwnerSkillset(), skillunlockCost) 
            && HasEnoughMemory(skill.GetTreeOwnerSkillset())  
            && skill.CanPurchaseSkill();

        if (!canPurchaseSkill) { return false; }

        skill.GetTreeOwnerSkillset().UnlockSkill(skillData);
        //Spend Skill Points.
        skill.GetTreeOwnerSkillset().SpendSkillPoint(skillunlockCost);
        return true;
    }

    public bool TryLevelUpSkill(SkillTreeNode skill)
    {
        PlayerTreeSkillData skillData = skill.GetSkillData();
        int skillLevelCost = skillData.GetCostForNextLevel(skill.GetSkillLevel());

        bool canLevelUp = CanAffordSkill(skill.GetTreeOwnerSkillset(), skillLevelCost) && skill.CanLevelUpSkill();

        if(!canLevelUp) { return false; }

        skill.GetTreeOwnerSkillset().LevelUpSkill(skillData);
        //Spend Skill Points.
        skill.GetTreeOwnerSkillset().SpendSkillPoint(skillLevelCost);
        return true;
    }

    public bool TryForgetSkill(SkillTreeNode skill)
    {
        PlayerTreeSkillData skillData = skill.GetSkillData();
        bool canForget = skill.CanForgetSkill();

        if (canForget)
        {
            int refundCost = skillData.GetTotalPointsInvestedInSkill(skill.GetSkillLevel());
            //Forget skill 
            skill.GetTreeOwnerSkillset().ForgetSkill(skillData);
            //Refund points.
            skill.GetTreeOwnerSkillset().EarnSkillPoint(refundCost);
        }

        return canForget;
    }

    private bool CanAffordSkill(PlayerSkillset skillOwnerSkillset, int cost)
    {
        return skillOwnerSkillset.GetSkillPointCount() >= cost;
    }

    private bool HasEnoughMemory(PlayerSkillset skillOwnerSkillset)
    {
        return skillOwnerSkillset.HasAvailableMemory();
    }
}
