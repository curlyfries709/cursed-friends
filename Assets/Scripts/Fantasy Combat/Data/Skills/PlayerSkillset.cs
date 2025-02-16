using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct UnlockedSkillStat
{
    public UnlockedSkillStat(SkillData data, int level)
    {
        skillData = data;
        skillLevel = level;
    }

    public SkillData skillData;
    public int skillLevel;

    public void LevelUp(int maxLevel)
    {
        skillLevel = Mathf.Min(skillLevel + 1, maxLevel);
    }
}

public class PlayerSkillset : MonoBehaviour, ISaveable
{
    [SerializeField] PartyMemberData memberData;
    [Space(10)]
    [Header("TEST")]
    [SerializeField] List<SkillData> testSkills;

    //Data
    Dictionary<string, UnlockedSkillStat> allLearnedSkills = new Dictionary<string, UnlockedSkillStat>();
    List<SkillData> activeSkillSet = new List<SkillData>();

    int skillPoints = 0;
    PlayerGridUnit myPlayerGridUnit;

    //Saving 
    bool isDataRestored = false;

    private void OnEnable()
    {
        PartyManager.Instance.PlayerPartyDataSet += Setup;
    }

    public void Setup()
    {
        myPlayerGridUnit = PartyManager.Instance.GetPlayerUnitViaName(memberData.memberName);
        myPlayerGridUnit.SetSkillData(this);
    }

    private void OnDisable()
    {
        PartyManager.Instance.PlayerPartyDataSet -= Setup;
    }

    public void UpdateActiveSkillset(List<SkillData> newActiveSkillSet) //Called by form changes to change player's active skillset
    {
        activeSkillSet.Clear();
        activeSkillSet = newActiveSkillSet;

        //If in Combat spawn new Skills 
        if(myPlayerGridUnit && FantasyCombatManager.Instance.InCombat())
        {
            CombatSkillManager.Instance.SpawnSkills(myPlayerGridUnit);
        }
    }

    public List<SkillData> GetActiveSkillSet() //This could change depending on player's current form
    {
        return activeSkillSet;
    }

    public void EarnSkillPoint(int amount)
    {
        skillPoints = skillPoints + amount;
    }

    public void SpendSkillPoint(int amount)
    {
        //This shouldn't be called if amount > stored skill points
        skillPoints = skillPoints - amount;
    }

    public void UnlockSkill(PlayerTreeSkillData skill)
    {
        string skillNameKey = skill.skillName;
        if(allLearnedSkills.ContainsKey(skillNameKey)) { return; }


        UnlockedSkillStat skillStat = new UnlockedSkillStat(skill, 1);
        allLearnedSkills[skillNameKey] = skillStat;
    }

    public void LevelUpSkill(PlayerTreeSkillData skill)
    {
        string skillNameKey = skill.skillName;
        if (allLearnedSkills.ContainsKey(skillNameKey)) { return; }

        allLearnedSkills[skillNameKey].LevelUp(skill.maxSkillLevel);
    }

    public void ForgetSkill(SkillData skillToForget)
    {
        allLearnedSkills.Remove(skillToForget.skillName);
    }

    public int GetSkillLevel(PlayerTreeSkillData skill)
    {
        UnlockedSkillStat stat = allLearnedSkills[skill.skillName];
        return stat.skillLevel;
    }

    public bool IsSkillUnlocked(PlayerTreeSkillData skill)
    {
        return allLearnedSkills.ContainsKey(skill.skillName);
    }

    public int GetSkillPointCount()
    {
        return skillPoints;
    }

    public bool HasAvailableMemory()
    {
        PlayerUnitStats playerStats = PartyManager.Instance.GetPartyMemberStats(memberData);

        int memory = playerStats.Memory;
        int skillCount = allLearnedSkills.Count;

        return memory > skillCount;
    }

    public void SetActiveSkillSetToDefault()
    {
        activeSkillSet.Clear();

        foreach(KeyValuePair<string, UnlockedSkillStat> pair in allLearnedSkills)
        {
            activeSkillSet.Add(pair.Value.skillData);
        }

        foreach(SkillData skill in testSkills)
        {
            if (!activeSkillSet.Contains(skill))
            {
                activeSkillSet.Add(skill);
            }
        }
    }
    //GETTERS
    public PartyMemberData GetPartyMemberData()
    {
        return memberData;
    }

    //SAVING
    public object CaptureState()
    {
        throw new System.NotImplementedException();
    }

    public void RestoreState(object state)
    {
        isDataRestored = true;

        if(state == null)
        {
            SetActiveSkillSetToDefault();
            return;
        }

        //STORE LEARNED SKILLS & SKILL POINT

        SetActiveSkillSetToDefault();
        throw new System.NotImplementedException();
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
