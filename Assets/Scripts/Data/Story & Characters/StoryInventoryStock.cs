using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Inventory Stock", menuName = "Configs/Inventory Stock", order = 10)]
public class StoryInventoryStock : ScriptableObject
{
    public List<InventoryStock> inventory;


    [System.Serializable]
    public class InventoryStock
    {
        public StoryCharacter inventoryOwner;
        public Item item;
        public int count = 1;
        public bool shouldRemove = false;
    }
}
