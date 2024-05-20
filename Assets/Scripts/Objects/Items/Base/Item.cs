using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Item : ScriptableObject
{
    public string itemName;
    public Sprite UIIcon;
    public ItemCatergory itemCatergory;
    [Space(5)]
    public float buyPrice;
    [SerializeField] protected float sellPrice;
    [Range(1, 99)]
    public int requiredLevel = 1;
    public float weight;
    public Rarity rarity;
    [Space(5)]
    [TextArea(5, 20)]
    public string description;

    public virtual float GetSellPrice()
    {
        return sellPrice;
    }

    public virtual string GetID()
    {
        return itemName;
    }
    
}


public enum Rarity
{
    Common, //No Color
    Uncommon, //Blue
    Rare, //Purple
    Legendary //Orange
}

public enum ItemCatergory
{
    Weapons,
    Armour,
    Consumables,
    Orbs,
    Ingredients,
    Enchantments,
    Books,
    Tools,
    Cards
}