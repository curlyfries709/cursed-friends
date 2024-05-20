using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Item/Weapon", order = 4)]
public class Weapon : Item
{
    [Header("Weapon Data")]
    public Category category;
    public GameObject modelPrefab;
    [Space(10)]
    public int basePhysAttack;
    public int baseMagAttack;
    [Header("Scaling")]
    [Range(1, 200)]
    public int scalingPercentage = 1;
    public Attribute scalingAttribute;
    [Header("Enchantments")]
    [Range(0, 5)]
    public int numOfEnchantmentSlots;
    [Header("Material & Element")]
    public WeaponMaterial material;
    public Element element;
    [Header("Alterations")]
    public List<AttributeBonus> bonusAttributes;
    [Space(10)]
    public List<InfusedEnchantment> infusedEnchantments;
    public List<Enchantment> embeddedEnchantments;

    public bool isClone { get; private set; }
    public Weapon baseWeaponScriptableObject { get; private set; }

    public enum Category
    {
        Sword,
        Bow,
        Staff,
        Ring,
        Axe
    }

    public void SetCloneData(Weapon weaponClonedFrom)
    {
        isClone = true;
        baseWeaponScriptableObject = weaponClonedFrom;
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
