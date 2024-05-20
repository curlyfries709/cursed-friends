using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyUnitStats : UnitStats
{
    [Header("ENEMY SPECIFIC DATA")]
    public int expGainOnDefeat;
    public List<ItemDropChance> dropItems;
}

[System.Serializable] 
public class ItemDropChance
{
    public Item item;
    [Range(1, 100)]
    public int dropChance = 1;
}