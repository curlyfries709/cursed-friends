using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

public class TheCache : MonoBehaviour
{
    public static TheCache Instance { get; private set; }

    [Title("Quests")]
    [SerializeField] List<Quest> quests = new List<Quest>();
    [Title("Objectives")]
    [SerializeField] List<Objective> objectives = new List<Objective>();
    [Title("Discussion Topics")]
    [SerializeField] List<DiscussionTopic> discussionTopics = new List<DiscussionTopic>();
    [Title("Items")]
    [SerializeField] List<Item> items = new List<Item>();
    [Title("Being Data")]
    [SerializeField] List<BeingData> beingDatas = new List<BeingData>();

    public string enchantedSeparator { get; private set; } = "$";

    List<Item> spawnedItems = new List<Item>();

    private void Awake()
    {
        Instance = this;
    }

    public List<DiscussionTopic> GetDiscussionsByName(List<string> names)
    {
        return discussionTopics.Where((topic) => names.Contains(topic.name)).ToList();
    }

    public void PopulateQuestDict(Dictionary<string, List<string>> data, ref Dictionary<Quest, List<Objective>> dictToPopulate)
    {
        dictToPopulate.Clear();
        dictToPopulate = data.ToDictionary(k => quests.First((item) => item.name == k.Key), k => GetObjectivesByID(k.Value));
    }

    public List<Objective> GetObjectivesByID(List<string> IDs)
    {
        return objectives.Where((objective) => IDs.Contains(objective.GetObjectiveID())).ToList();
    }

    public List<Item> GetItemsById(List<string> IDs)
    {
        List<Item> foundItems = new List<Item>();

        foreach(string id in IDs)
        {
            foundItems.Add(GetItemByID(id));
        }

        return foundItems;
    }



    public Item GetItemByID(string itemID)
    {
        Item foundItem = items.Concat(spawnedItems).FirstOrDefault((item) => item.GetID() == itemID);

        if(foundItem == null)
        {
            //Likely An Enchanted Equipment Needs to Be Instianted.

            List<string> splitName = itemID.Split(enchantedSeparator).ToList();
            Item baseItem = items.FirstOrDefault((item) => item.itemName == splitName[0]);

            if(!(baseItem is Weapon || baseItem is Armour))
            {
                Debug.Log("ENCHANTED ITEM BASE COULD NOT BE FOUND. ID: " + itemID);
                return null;
            }

            foundItem = Instantiate(baseItem);
            spawnedItems.Add(foundItem);

            Weapon newEnchantedWeapon = foundItem as Weapon;
            Armour newEnchantedArmour = foundItem as Armour;

            splitName.RemoveAt(0); //Remove Base Item Name

            foreach(string enchantmentName in splitName)
            {
                Enchantment enchantment = items.FirstOrDefault((item) => item.GetID() == enchantmentName) as Enchantment;

                if (newEnchantedWeapon)
                {
                    newEnchantedWeapon.embeddedEnchantments.Add(enchantment);
                }
                else
                {
                    newEnchantedArmour.embeddedEnchantments.Add(enchantment);
                }
            }

            if (newEnchantedWeapon)
            {
                newEnchantedWeapon.SetCloneData(baseItem as Weapon);
            }
            else
            {
                newEnchantedArmour.SetCloneData(baseItem as Armour);
            }
        }

        return foundItem;
    }

    public BeingData GetBeingDataByKey(string key)
    {
        return beingDatas.FirstOrDefault((item) => item.Key() == key);
    }

    public List<Item> ListOfRandomItems(int count)
    {
        List<Item> returnList = new List<Item>();

        for (int i = 0; i < count; i++)
        {
            returnList.Add(items[Random.Range(0, items.Count)]);
        }

        return returnList;
    }

}
