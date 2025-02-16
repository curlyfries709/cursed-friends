using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Mythical Player Skill", menuName = "Skill/Player Mythical Skill", order = 1)]
public class PlayerTreeSkillData : PlayerSkillData
{
    [Header("Tree Data")]
    public int unlockCost = 1;
    [Tooltip("Level 1 is index 0. Level 1 value should be left at 0 because skill is instantly level 1 upon purchase")]
    public List<int> upgradeCostPerLevel = new List<int>();
    [Space(10)]
    public int maxSkillLevel = 3;
    public int requiredPlayerLevelToPurchase = 1;

    public int GetCostForNextLevel(int currentLevel)
    {
        int nextLevel = currentLevel + 1;

        //if at max Level return 0
        if(nextLevel > maxSkillLevel)
        {
            return 0;
        }

        return upgradeCostPerLevel[nextLevel - 1];
    }

    public int GetTotalPointsInvestedInSkill(int currentLevel)
    {
        int total = unlockCost;

        if (upgradeCostPerLevel.Count > 1)
        {
            //Start From index 1 because we ignore level cost.
            for (int i = 1; i < currentLevel; ++i)
            {
                total = total + upgradeCostPerLevel[i];
            }
        }

        return total;
    }
}
