using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ItemDropChance
{
    public Item item;
    [Range(1, 100)]
    public int dropChance = 1;
}

public class EnemyUnitStats : UnitStats
{
    [Header("ENEMY SPECIFIC DATA")]
    [SerializeField] int naturalArmour;
    [Space(10)]
    public int expGainOnDefeat;
    public List<ItemDropChance> dropItems;
    [Header("Skills")]
    [SerializeField] List<SkillData> enemySkillSet = new List<SkillData>();

    public override int GetArmour()
    {
        int armour = equipment.Armour() ? equipment.Armour().armour : naturalArmour;
        return Mathf.RoundToInt(armour * buffARMmultiplier);
    }

    public List<SkillData> GetEnemySkillSet()
    {
        return new List<SkillData>(enemySkillSet);
    }
}

