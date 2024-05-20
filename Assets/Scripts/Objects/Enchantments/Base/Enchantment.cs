using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


[CreateAssetMenu(fileName = "New Enchantment", menuName = "Item/Enchantment/Free", order = 0)]
public class Enchantment : Item
{
    [Title("Enchantment Data")]
    public Restriction equipRestriction = Restriction.Both;
    [Space(5)]
    public List<Race> raceRestriction;
    [Header("Weapon Alteration")]
    public List<ChanceOfInflictingStatusEffect> inflictedStatusEffects;
    [Header("Armour Alterations")]
    public List<StatusEffectData> statusEffectsNulled;
    [Space(5)]
    public bool knockbackImmunity = false;
    [Title("Passive Abilities")]
    public List<EnchantmentPassiveData> passiveAbilities;

    public enum Restriction
    {
        Both,
        Weapon,
        Armour
    }
}


[System.Serializable]
public class EnchantmentPassiveData
{
    [Range(0, 100)]
    public int passivePercentageValue;
    public int passiveNumberValue;
    public GameObject passiveAbility;
}