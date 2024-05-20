using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedLoot : Lootable
{
    public override void HandleInteraction(bool inCombat)
    {
        BeginLoot(inCombat);
    }

    public override void OnLootEmpty()
    {
        gameObject.SetActive(false);
    }

    public void Setup(List<Item> items)
    {
        this.items = items;
        gameObject.SetActive(true);
    }

    public void DiscardLoot()
    {
        items.Clear();
    }
}
