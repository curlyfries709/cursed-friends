using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillTree : MonoBehaviour
{
    [SerializeField] string treeName;
    [SerializeField] PartyMemberData treeOwner; 
    [Tooltip("Is this tree locked by default and unlocked later via story event")]
    [SerializeField] bool isLocked = false;
    [Space(10)]
    [Tooltip("Requirements to meet to unlock a tree level. Level 1 is index 0")]
    [SerializeField] List<List<AttributeBonus>> requirementsPerTreeLevel = new List<List<AttributeBonus>>();


    public bool DoesPlayerMeetRequirementsToBuySkill(int treeLevelOfSkill)
    {
        if(isLocked) return false;

        List<AttributeBonus> requirementList = requirementsPerTreeLevel[treeLevelOfSkill - 1];
        UnitStats playerStats = PartyManager.Instance.GetPartyMemberStats(treeOwner);

        foreach(AttributeBonus requirement in requirementList)
        {
            if (!playerStats.HasRequiredAttributeValue(requirement.attribute, requirement.attributeChange, false))
            {
                return false;
            }
        }

        return true;
    }

    public PartyMemberData GetTreeOwner()
    {
        return treeOwner;
    }
}
