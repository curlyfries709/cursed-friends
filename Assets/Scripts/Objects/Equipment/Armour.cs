using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Armour", menuName = "Item/Armour", order = 0)]
public class Armour : Item
{
    [Header("Armour Data")]
    public int armour;
    public List<Race> raceRestriction;
    [Range(0, 5)]
    public int numOfEnchantmentSlots;
    [Header("Alterations")]
    public List<AttributeBonus> bonusAttributes;
    [Space(10)]
    public List<InfusedEnchantment> infusedEnchantments;
    public List<Enchantment> embeddedEnchantments;

    
    public bool isClone { get; private set; }
    public Armour baseArmourScriptableObject { get; private set; }


    public void SetCloneData(Armour armourClonedFrom)
    {
        isClone = true;
        baseArmourScriptableObject = armourClonedFrom;
    }

    public override float GetSellPrice()
    {
        float price = sellPrice;

        foreach (Enchantment enchantment in embeddedEnchantments)
        {
            price = price + enchantment.GetSellPrice();
        }

        return price;
    }

    public override string GetID()
    {
        string IDName = itemName;

        foreach (Enchantment enchantment in embeddedEnchantments)
        {
            IDName = IDName + TheCache.Instance.enchantedSeparator + enchantment.GetID();
        }

        return IDName;
    }

    public string GetNewEnchantedID(Enchantment enchantment)
    {
        return GetID() + TheCache.Instance.enchantedSeparator + enchantment.GetID();
    }
}
