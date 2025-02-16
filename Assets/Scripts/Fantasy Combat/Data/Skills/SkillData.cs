using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkillCategory
{
    //Comes under skill menu
    Skill,
    //Comes Under tactic menu
    Tactic,
    Passive
}

public enum SkillEffectType
{
    Offense,
    Tactical,
    Recovery,
    Support
}

[CreateAssetMenu(fileName = "New AI Skill", menuName = "Skill/AI Skill", order = 1)]
public class SkillData : ScriptableObject
{
    public string skillName;
    [Header("Categories")]
    public SkillCategory category = SkillCategory.Skill;
    public SkillEffectType skillEffectType = SkillEffectType.Offense;
    [Header("Prefab")]
    public GameObject skillPrefab;
}
