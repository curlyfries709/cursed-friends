using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillTreeNode : MonoBehaviour
{
    [SerializeField] PlayerTreeSkillData skillData;
    [Header("Associated Tree")]
    [Tooltip("Some skills are tied to multiple paths, thus this is a list.")]
    [SerializeField] List<SkillTree> associatedTrees;
    [SerializeField] int treeLevel = 1;
    [Header("Parent Nodes")]
    [Tooltip("At least one of the parent nodes must be unlocked for this node to be unlocked. Leave null if node is at start of tree")]
    [SerializeField] List<SkillTreeNode> parentSkillsNodes = new List<SkillTreeNode>();

    List<SkillTreeNode> childrenSkillNodes = new List<SkillTreeNode>();

    private void Start()
    {
        SetSelfAsChildForParent();
    }
    public bool CanPurchaseSkill()
    {
        //Check if already purchased
        if (IsSkillPurchased())
        {
            return false;
        }

        //Check player Level meets requirements
        if (ProgressionManager.Instance.GetLevel(GetTreeOwner().memberName) < skillData.requiredPlayerLevelToPurchase)
        {
            return false;
        }

        bool isOneParentPurchased = false;
        bool meetsRequirementForOneAssociatedTree = false;

        foreach(SkillTreeNode parent in parentSkillsNodes)
        {
            if (parent.IsSkillPurchased())
            {
                isOneParentPurchased = true;
                break;
            }
        }

        foreach(SkillTree tree in associatedTrees)
        {
            if (tree.DoesPlayerMeetRequirementsToBuySkill(treeLevel))
            {
                meetsRequirementForOneAssociatedTree = true;
                break;
            }
        }

        return meetsRequirementForOneAssociatedTree && isOneParentPurchased;
    }

    public bool CanLevelUpSkill()
    {
        if (!IsSkillPurchased()) { return false; }

        int currentLevel = GetSkillLevel();
        int nextLevel = currentLevel + 1;

        //Can't upgrade if already at max
        return skillData.maxSkillLevel > nextLevel;
    }

    public void SetChildNode(SkillTreeNode node)
    {
        if (!node) { return; }

        if (!childrenSkillNodes.Contains(node))
        {
            childrenSkillNodes.Add(node);
        }       
    }

    public bool CanForgetSkill()
    {
        //If all children are locked, then skill can be forgetton
        bool hasUnlockedChild = false;

        foreach (SkillTreeNode child in childrenSkillNodes)
        {
            if (child.IsSkillPurchased())
            {
                hasUnlockedChild = true;
                break;
            }
        }

        return hasUnlockedChild;
    }

    private void SetSelfAsChildForParent()
    {
        foreach(SkillTreeNode node in parentSkillsNodes)
        {
            node.SetChildNode(this);
        }
    }

    //GETTERS
    public bool IsSkillPurchased()
    {
        return GetTreeOwnerSkillset().IsSkillUnlocked(skillData);
    }

    public int GetSkillLevel()
    {
        return GetTreeOwnerSkillset().GetSkillLevel(skillData);
    }

    public PartyMemberData GetTreeOwner()
    {
        return associatedTrees[0].GetTreeOwner();
    }

    public PlayerSkillset GetTreeOwnerSkillset()
    {
        return PartyManager.Instance.GetPartyMemberLearnedSkill(GetTreeOwner());
    }

    public PlayerTreeSkillData GetSkillData()
    {
        return skillData;
    }

}
