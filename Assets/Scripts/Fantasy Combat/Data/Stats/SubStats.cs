using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Attribute
{
    Strength,
    Finesse,
    Endurance,
    Agility,
    Intelligence,
    Wisdom,
    Charisma
}

public enum SubStats
{
    Vitality,
    Stamina,
    
    Technique,
    Evasion,
   
    Speed,
    Movement,

    Memory,
    InventoryWeight,

    HealEfficacy,
    StatusEffectDuration,
    ScrollDuration,
    
    CritChance,
    StatusEffectChance
}

[System.Serializable]
public class SubStatBonus
{
    public SubStats subStat;
    public int subStatChange;
}

[System.Serializable]
public class AttributeBonus
{
    public Attribute attribute;
    public int attributeChange;
}