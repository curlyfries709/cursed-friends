using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Race Profile", menuName = "Configs/Race Profile", order = 1)]
public class BeingData : ScriptableObject
{
    [Header("Character Profile")]
    public string databaseName;
    public Race race;
    public RaceType raceType;
    [Space(5)]
    [Tooltip("This is only used by players to tie their weapon based on race")]
    public Weapon.Category proficientWeaponCategory;
    [Space(10)]
    [Tooltip("For monsters only")]
    public string lockedName;
    [Space(10)]
    [TextArea(10, 20)]
    public string description;
    [Header("Affinities")]
    public List<ElementAffinity> elementAffinities = new List<ElementAffinity>();
    public List<ItemAffinity> itemAffinities = new List<ItemAffinity>();
    [Space(10)]
    public List<StatusEffectData> statusEffectsNullified = new List<StatusEffectData>();
    [Header("Dropped Ingridients")]
    public List<ItemDropChance> droppedIngridients;


    public string Key()
    {
        return raceType.ToString() + race.ToString();
    }
}
