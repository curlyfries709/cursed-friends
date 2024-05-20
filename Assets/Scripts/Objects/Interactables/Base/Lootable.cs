using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public abstract class Lootable : Interact
{
    [Header("Loot")]
    [ListDrawerSettings(Expanded = true)]
    [SerializeField] protected List<Item> items = new List<Item>();

    protected List<Item> originalItems;
    protected List<int> takenItemIndices = new List<int>(); //For Saving & Loading

    public SFXType sfxOnBeginLoot { get; protected set; } = SFXType.OpenPouch;

    public abstract void OnLootEmpty();

    protected virtual void Awake()
    {
        originalItems = new List<Item>(items);
    }

    public void TakeItem(PlayerGridUnit taker, Item item)
    {
        int index = items.IndexOf(item);

        takenItemIndices.Add(index);
        items.RemoveAt(index);

        InventoryManager.Instance.AddToInventory(taker, item);
    }

    public List<Item> GetItems()
    {
        return items;
    }

    protected void BeginLoot(bool inCombat)
    {
        if (!inCombat)
        {
            LootUI.Instance.BeginLoot(this);
        }
    }
}
