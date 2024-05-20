
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System;
using Sirenix.Serialization;

public class InventoryManager : MonoBehaviour, ISaveable
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] SneakBarrel barrel;
    [Header("Money")]
    [Tooltip("Ratio of £1 to N Gold")]
    [SerializeField] int poundToGoldConversionConstant = 100;
    [Header("Spend Colours")]
    public Color defaultCoinColor;
    public Color spendCoinColor;
    public Color earnCoinColor;
    [Header("Rarity Colours")]
    [SerializeField] Color common;
    [SerializeField] Color uncommon;
    [SerializeField] Color rare;
    [SerializeField] Color legendary;
    [Header("UI")]
    [SerializeField] GameObject inventoryUI;
    [Header("TEST")]
    [SerializeField] bool addTestItems;
    public List<Item> everyoneTestInventory;
    [Space(10)]
    public List<Item> keenanItemsOnly;
    public List<Item> kiraItemsOnly;
    public List<Item> imaniItemsOnly;

    //Saving Data
    [SerializeField, HideInInspector]
    private InventoryState inventoryState = new InventoryState();
    public bool AutoRestoreOnNewTerritoryEntry { get; set; } = false;

    //Caches
    PlayerInput playerInput;

    //Storage
    Dictionary<PlayerGridUnit, List<Potion>> potionsUsedInCombat = new Dictionary<PlayerGridUnit, List<Potion>>();

    //INVENTORIES
    public int fantasyMoney { get; private set; }
    public float modernMoney { get; private set; }

    Dictionary<string, List<Item>> characterFantasyWorldInventories = new Dictionary<string, List<Item>>();
    List<Item> playerRealWorldInventory = new List<Item>();

    //Events
    public Action<Item, PlayerGridUnit> ItemDiscarded; //The Item & The Inventory Owner.
    public Action<Item, PlayerGridUnit, PlayerGridUnit> ItemTransfered; //The Item, Inventory Owner, Receiver;

    private void Awake()
    {
        Instance = this;
        playerInput = ControlsManager.Instance.GetPlayerInput();
    }

    private void OnEnable()
    {
        FantasyCombatManager.Instance.BattleRestarted += OnBattleRestart;
        FantasyCombatManager.Instance.CombatBegun += OnCombatBegin;
    }

    private void Start()
    {
        //TEST  METHOD!
        if (!SavingLoadingManager.Instance.LoadingEnabled)
        {
            IntializeAllInventories();
            SetTestItems();
        }    
    }

    private void SetTestItems()
    {
        if (!addTestItems) { return; }

        foreach (PlayerGridUnit player in PartyData.Instance.GetAllPlayerMembersInWorld())
        {
            foreach (Item item in everyoneTestInventory)
            {
                AddToInventory(player, item);
            }
        }


        foreach (Item item in keenanItemsOnly)
        {
            AddToInventory(PartyData.Instance.GetLeader(), item);
        }

        if (PartyData.Instance.GetPlayerUnitViaName("Imani"))
        {
            foreach (Item item in imaniItemsOnly)
            {
                AddToInventory(PartyData.Instance.GetPlayerUnitViaName("Imani"), item);
            }
        }

        if (PartyData.Instance.GetPlayerUnitViaName("Kira"))
        {
            foreach (Item item in kiraItemsOnly)
            {
                AddToInventory(PartyData.Instance.GetPlayerUnitViaName("Kira"), item);
            }

        }

        //Earn Money
        EarnCoin(20.50f, false);
        EarnCoin(250, true);
    }

    private void OnBattleRestart()
    {
        //Restore
        foreach (var item in potionsUsedInCombat)
        {
            foreach (Potion potion in item.Value)
            {
                AddToInventory(item.Key, potion);
            }
        }
    }

    private void OnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        potionsUsedInCombat.Clear();

        foreach (PlayerGridUnit player in FantasyCombatManager.Instance.GetPlayerCombatParticipants(true, true))
        {
            potionsUsedInCombat[player] = new List<Potion>();
        }
    }

    public void ActivateInventoryUI(bool activate)
    {
        PhoneMenu.Instance.OpenApp(activate);
        inventoryUI.SetActive(activate);
    }

    public void UpdateInventory(StoryInventoryStock inventoryStock) //Usually called by Cinematic Events.
    {
        foreach(var stock in inventoryStock.inventory)
        {
            PlayerGridUnit inventoryOwner = PartyData.Instance.GetPlayerUnitViaName(stock.inventoryOwner.characterName);

            if (stock.shouldRemove)
            {
                RemoveMultipleFromInventory(inventoryOwner, stock.item, stock.count);
            }
            else
            {
                AddMultipleToInventory(inventoryOwner, stock.item, stock.count);
            }
        }
    }

    //Money
    public float GetMoneyAmount(bool isFantasyCurrency)
    {
        return isFantasyCurrency ? fantasyMoney : modernMoney;
    }

    public bool CanAfford(float price, bool isFantasyCurrency)
    {
        if (isFantasyCurrency)
            return fantasyMoney >= price;

        return modernMoney >= price;
    }

    public bool TrySpendCoin(float price, bool isFantasyCurrency)
    {
        bool canAfford = CanAfford(price, isFantasyCurrency);

        if (canAfford)
        {
            if (isFantasyCurrency)
            {
                fantasyMoney = fantasyMoney - Mathf.FloorToInt(price);
            }
            else
            {
                modernMoney = modernMoney - price;
            }
            return canAfford;
        }

        return canAfford;
    }

    public void EarnCoin(float gain, bool isFantasyCurrency)
    {
        if (isFantasyCurrency)
        {
            fantasyMoney = fantasyMoney + Mathf.FloorToInt(gain);
        }
        else
        {
            modernMoney = modernMoney + gain;
        }
    }

    
    public void EarnModernCoin(float gain) //Called By Unity Events
    {
        EarnCoin(gain, false);
    }

    public void EarnFantasyCoin(int gain) //Called By Unity Events
    {
        EarnCoin(gain, true);
    }

    public int ConvertPoundToGold(int pound)
    {
        return pound * poundToGoldConversionConstant;
    }


    //Inventory

    public void UseItemFromInventory(PlayerGridUnit inventoryOwner, Item item, PlayerGridUnit user)
    {
        Potion potion = item as Potion;

        if (potion)
        {
            potion.UseOutsideCombat(user);
            RemoveFromInventory(inventoryOwner, item);
        }
    }

    public List<Item> GetItemListFromCategory(PlayerGridUnit player, ItemCatergory category, bool putEquippedFirst, bool removeDuplicates = true)
    {
        List<Item> inventory = characterFantasyWorldInventories[player.unitName];
        List<Item> returnList = new List<Item>();

        if (removeDuplicates)
        {
            returnList = inventory.Where((element) => element.itemCatergory == category).Distinct().ToList();
        }
        else
        {
            returnList = inventory.Where((element) => element.itemCatergory == category).ToList();
        }

        if (IsEquippable(category) && putEquippedFirst)
        {
            List<Item> equippedItems = returnList.Where((element) => player.stats.IsEquipped(element)).ToList();

            foreach(Item equipped in equippedItems)
            {
                returnList.Remove(equipped);
                returnList.Insert(0, equipped);
            }
        }

        return returnList;
    }


    public List<T> GetItemInventory<T>(PlayerGridUnit player, bool removeDuplicates = true) where T : Item
    {
        List<Item> inventory = characterFantasyWorldInventories[player.unitName];
        List<T> itemList = new List<T>();

        foreach (Item item in inventory)
        {
            if (item is T)
            {
                if (removeDuplicates && itemList.Contains(item))
                {
                    continue;
                }

                itemList.Add((T)(item));
            }
        }

        return itemList.OrderBy((item) => item.itemName).ToList();
    }

    public int GetItemCount(Item item, PlayerGridUnit player)
    {
        List<Item> inventory = characterFantasyWorldInventories[player.unitName];
        return inventory.Where((invItem) => invItem == item).Count();
    }

    public int GetItemCountAcrossAllInventories(Item item)
    {
        int count = 0;

        foreach(PlayerGridUnit player in PartyData.Instance.GetAllPlayerMembersInWorld())
        {
            List<Item> inventory = characterFantasyWorldInventories[player.unitName];
            count = count + inventory.Where((invItem) => invItem == item).Count();
        }

        return count;
    }

    public int GetItemCountAcrossAllInventories(Item item, List<PlayerGridUnit> availablePlayers)
    {
        int count = 0;

        foreach (PlayerGridUnit player in availablePlayers)
        {
            List<Item> inventory = characterFantasyWorldInventories[player.unitName];
            count = count + inventory.Where((invItem) => invItem == item).Count();
        }

        return count;
    }

    public void AddToInventory(PlayerGridUnit player, Item item)
    {
        characterFantasyWorldInventories[player.unitName].Add(item);
        OverburdenCheck(player);
    }

    public void AddMultipleToInventory(PlayerGridUnit player, Item item, int count)
    {
        characterFantasyWorldInventories[player.unitName].AddRange(Enumerable.Repeat(item, count));
        OverburdenCheck(player);
    }

    public void UsedPotionInCombat(PlayerGridUnit player, Potion potion)
    {
        potionsUsedInCombat[player].Add(potion);
        RemoveFromInventory(player, potion);
    }

    public void RemoveFromInventory(PlayerGridUnit player, Item item)
    {
        characterFantasyWorldInventories[player.unitName].Remove(item);
        ItemDiscarded?.Invoke(item, player);
        OverburdenCheck(player);
    }

    public void RemoveMultipleFromInventory(PlayerGridUnit player, Item item, int count)
    {
        List<Item> itemsToRemove = characterFantasyWorldInventories[player.unitName].Where((element) => element == item).Take(count).ToList();

        for (int i = itemsToRemove.Count - 1; i >= 0; i--)
        {
            Item currentItem = itemsToRemove[i];
            RemoveFromInventory(player, currentItem);
        }
    }

    public void TransferToAnotherInventory(PlayerGridUnit giver, PlayerGridUnit receiver, Item item, int count)
    {
        List<Item> itemsToTransfer = characterFantasyWorldInventories[giver.unitName].Where((element) => element == item).Take(count).ToList();

        for (int i = itemsToTransfer.Count -1; i>= 0; i--)
        {
            Item currentItem = itemsToTransfer[i];

            characterFantasyWorldInventories[giver.unitName].Remove(currentItem);
            characterFantasyWorldInventories[receiver.unitName].Add(currentItem);

            ItemTransfered?.Invoke(currentItem, giver, receiver);
        }

        OverburdenCheck(giver);
        OverburdenCheck(receiver);
    }

    //Enchantments
    public bool TryEnchantWeapon(PlayerGridUnit weaponOwner, PlayerGridUnit enchantmentOwner, Weapon weapon, Enchantment enchantment)
    {
        bool canEnchant = CanEnchantWeapon(weapon, enchantment);

        if (!canEnchant) { return false; }

        bool isEquipped = weaponOwner.stats.IsEquipped(weapon);

        RemoveFromInventory(weaponOwner, weapon);
        RemoveFromInventory(enchantmentOwner, enchantment);

        //Weapon only needs to be instantiated if doesnt Exist.
        string newID = weapon.GetNewEnchantedID(enchantment);
        Item foundItem = FindInventoryItemAcrossAllInventoriesViaID(newID);

        bool alreadyHasEnchantedItem = foundItem != null;

        Weapon newEnchantedWeapon = alreadyHasEnchantedItem ? foundItem as Weapon : Instantiate(weapon);

        //Only add Enchantment to newly Spawned Weapon.
        if (!alreadyHasEnchantedItem)
            newEnchantedWeapon.embeddedEnchantments.Add(enchantment);

        AddToInventory(weaponOwner, newEnchantedWeapon);

        if (!weapon.isClone)
        {
            newEnchantedWeapon.SetCloneData(weapon);
        }

        if (isEquipped)
        {
            weaponOwner.stats.EquipWeapon(newEnchantedWeapon);
            weaponOwner.stats.UpdateEnchantments();
        }

        return canEnchant;
    }

    public bool TryEnchantArmour(PlayerGridUnit armourOwner, PlayerGridUnit enchantmentOwner, Armour armour, Enchantment enchantment)
    {
        bool canEnchant = CanEnchantArmour(armour, enchantment);

        if (!canEnchant) { return false; }

        bool isEquipped = armourOwner.stats.IsEquipped(armour);

        RemoveFromInventory(armourOwner, armour);
        RemoveFromInventory(enchantmentOwner, enchantment);

        //Armour only needs to be instantiated if doesnt Exist.
        string newID = armour.GetNewEnchantedID(enchantment);
        Item foundItem = FindInventoryItemAcrossAllInventoriesViaID(newID);

        bool alreadyHasEnchantedItem = foundItem != null;

        Armour newEnchantedArmour = alreadyHasEnchantedItem ? foundItem as Armour: Instantiate(armour);

        //Only add Enchantment to newly Spawned Armour.
        if(!alreadyHasEnchantedItem)
            newEnchantedArmour.embeddedEnchantments.Add(enchantment);

        AddToInventory(armourOwner, newEnchantedArmour);

        if (!armour.isClone)
        {
            newEnchantedArmour.SetCloneData(armour);
        }

        if (isEquipped)
        {
            armourOwner.stats.EquipArmour(newEnchantedArmour);
            armourOwner.stats.UpdateEnchantments();
        }

        return canEnchant;
    }

    public bool TryDisenchantEquipment(PlayerGridUnit equipmentOwner, Item equipment, Enchantment enchantment)
    {
        bool canDisenchant = CanDisenchantEquipment(equipment) && !(enchantment is InfusedEnchantment);
        if (!canDisenchant) { return false; }

        Weapon weapon = equipment as Weapon;
        Armour armour = equipment as Armour;

        bool isEquipped = equipmentOwner.stats.IsEquipped(equipment);

        //Remove Enchantment From Equipment
        if (weapon)
        {
            weapon.embeddedEnchantments.Remove(enchantment);
        }
        else
        {
            armour.embeddedEnchantments.Remove(enchantment);
        }

        //Add Enchantment to Equipment Owner Inventory
        AddToInventory(equipmentOwner, enchantment);

        //Check If Embedded Enchantments are empty and gift original item to inventory.
        bool areEnchantmentsEmpty = weapon ? weapon.embeddedEnchantments.Count == 0 : armour.embeddedEnchantments.Count == 0;

        if (areEnchantmentsEmpty)
        {
            Item originalItem = weapon ? weapon.baseWeaponScriptableObject : armour.baseArmourScriptableObject;

            //Remove From Inventory
            RemoveFromInventory(equipmentOwner, equipment);

            //Add Base Copy back to inventory
            AddToInventory(equipmentOwner, originalItem);

            //If Item was equipped, equip new base copy
            if (isEquipped)
            {
                if (weapon)
                {
                    equipmentOwner.stats.EquipWeapon(originalItem as Weapon);
                }
                else
                {
                    equipmentOwner.stats.EquipArmour(originalItem as Armour);
                }
     
                equipmentOwner.stats.UpdateEnchantments();
            }
        }

        return canDisenchant;
    }

    private void OnDisable()
    {
        FantasyCombatManager.Instance.BattleRestarted -= OnBattleRestart;
        FantasyCombatManager.Instance.CombatBegun -= OnCombatBegin;
    }

    private void IntializeAllInventories()
    {
        foreach (PlayerGridUnit player in PartyData.Instance.GetAllPlayerMembersInWorld())
        {
            IntializePlayerInventory(player);
        }
    }

    private void IntializePlayerInventory(PlayerGridUnit player)
    {
        characterFantasyWorldInventories[player.unitName] = new List<Item>();
    }

    private void OverburdenCheck(PlayerGridUnit player)
    {
        bool isOverburdened = GetCurrentWeight(player) > GetWeightCapacity(player);
        StatusEffectManager.Instance.UnitOverburdened(player, isOverburdened);
    }

    public bool HasItem(Item item, PlayerGridUnit inventory)
    {
        return characterFantasyWorldInventories[inventory.unitName].Contains(item) || playerRealWorldInventory.Contains(item);
    }

    public bool HasItem(Item requiredItem)
    {
        //Check Real World Inventory First
        foreach(Item item in playerRealWorldInventory)
        {
            if(requiredItem == item)
            {
                return true;
            }
        }

        //Check All Fantasy Inventories
        foreach (var pair in characterFantasyWorldInventories)
        {
            foreach (Item item in pair.Value)
            {
                if (requiredItem == item)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool CanEnchantWeapon(Weapon weapon, Enchantment enchantment)
    {
        return weapon.numOfEnchantmentSlots > 0 && weapon.infusedEnchantments.Count + weapon.embeddedEnchantments.Count < weapon.numOfEnchantmentSlots
            && enchantment.equipRestriction != Enchantment.Restriction.Armour;
    }

    public bool CanEnchantArmour(Armour armour, Enchantment enchantment)
    {
        return armour.numOfEnchantmentSlots > 0 && armour.infusedEnchantments.Count + armour.embeddedEnchantments.Count < armour.numOfEnchantmentSlots
            && enchantment.equipRestriction != Enchantment.Restriction.Weapon;
    }

    public bool CanEnchantEquipment(Enchantment enchantment, Item equipment)
    {
        Weapon weapon = equipment as Weapon;
        Armour armour = equipment as Armour;

        if (weapon)
        {
            if (enchantment != null)
            {
                return CanEnchantWeapon(weapon, enchantment);
            }

            return weapon.numOfEnchantmentSlots > 0 && weapon.infusedEnchantments.Count + weapon.embeddedEnchantments.Count < weapon.numOfEnchantmentSlots;
        }
        else if (armour)
        {
            if (enchantment != null)
            {
                return CanEnchantArmour(armour, enchantment);
            }

            return armour.numOfEnchantmentSlots > 0 && armour.infusedEnchantments.Count + armour.embeddedEnchantments.Count < armour.numOfEnchantmentSlots;
        }

        return false;
    }

    public bool CanDisenchantEquipment(Item equipment)
    {
        Weapon weapon = equipment as Weapon;
        Armour armour = equipment as Armour;

        if (weapon)
        {
            return weapon.numOfEnchantmentSlots > 0 && weapon.infusedEnchantments.Count + weapon.embeddedEnchantments.Count >  0;
        }
        else if (armour)
        {
            return armour.numOfEnchantmentSlots > 0 && armour.infusedEnchantments.Count + armour.embeddedEnchantments.Count > 0;
        }

        return false;
    }

    public bool HasEnchantmentSlots(Item equipment)
    {
        Weapon weapon = equipment as Weapon;
        Armour armour = equipment as Armour;

        if (weapon)
        {
            return weapon.numOfEnchantmentSlots > 0;
        }
        else if (armour)
        {
            return armour.numOfEnchantmentSlots > 0;
        }

        return false;
    }


    public float GetCurrentWeight(PlayerGridUnit player)
    {
        float total = 0;

        foreach(Item item in characterFantasyWorldInventories[player.unitName])
        {
            total = total + item.weight;
        }

        return (float)Math.Round(total, 1);
    }

    public float GetWeightCapacity(PlayerGridUnit player)
    {
        return player.stats.InventoryWeight;
    }


    private bool IsEquippable(ItemCatergory category)
    {
        switch (category)
        {
            case ItemCatergory.Armour:
                return true;
            case ItemCatergory.Weapons:
                return true;
            case ItemCatergory.Enchantments:
                return true;
            case ItemCatergory.Tools:
                return true;
            default:
                return false;
        }
    }

    public Color GetRarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon:
                return uncommon;
            case Rarity.Rare:
                return rare;
            case Rarity.Legendary:
                return legendary;
            default:
                return common;
        }
    }

    //INput
    /*private void OnUseUp(InputAction.CallbackContext context)
    {
        if (context.action.name != "UseUp") { return; }

        if (context.performed)
        {
            barrel.Toggle();
        }
    }*/


    //Saving
    [System.Serializable]
    public class InventoryState
    {
        //Inventory Manager: Inventory. 
        // Also Instantiated Weapon & Armours that have been enchanted.

        //MONEY
        public int fantasyMoney;
        public float modernMoney;

        //Inventories
        public Dictionary<string, List<string>> characterFantasyWorldInventories = new Dictionary<string, List<string>>();
        public List<string> playerRealWorldInventory = new List<string>();

        //Equipment 
        public Dictionary<string, List<string>> equippedItems = new Dictionary<string, List<string>>();
    }


    public object CaptureState()
    {
        inventoryState.fantasyMoney = fantasyMoney;
        inventoryState.modernMoney = modernMoney;

        inventoryState.characterFantasyWorldInventories = characterFantasyWorldInventories.ToDictionary(k => k.Key, k => k.Value.ConvertAll((item) => item.GetID()));
        inventoryState.playerRealWorldInventory = playerRealWorldInventory.ConvertAll((item) => item.GetID());

        inventoryState.equippedItems.Clear();

        foreach (PlayerGridUnit player in PartyData.Instance.GetAllPlayerMembersInWorld())
        {
            List<string> equippedItems = new List<string>();

            equippedItems.Add(player.stats.Weapon().GetID());
            equippedItems.Add(player.stats.EquippedArmour().GetID());

            inventoryState.equippedItems[player.unitName] = equippedItems;
        }

        return SerializationUtility.SerializeValue(inventoryState, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        if (state == null)
        {
            IntializeAllInventories();
            return;
        }

        byte[] bytes = state as byte[];
        inventoryState = SerializationUtility.DeserializeValue<InventoryState>(bytes, DataFormat.Binary);

        //Restore Money
        fantasyMoney = inventoryState.fantasyMoney;
        modernMoney = inventoryState.modernMoney;

        //Restore Inventory
        characterFantasyWorldInventories = inventoryState.characterFantasyWorldInventories.ToDictionary(k => k.Key, k => TheCache.Instance.GetItemsById(k.Value));
        playerRealWorldInventory = TheCache.Instance.GetItemsById(inventoryState.playerRealWorldInventory);

        //Equip Items
        foreach (var pair in inventoryState.equippedItems)
        {
            PlayerGridUnit player = PartyData.Instance.GetPlayerUnitViaName(pair.Key);

            foreach (string equippedID in pair.Value)
            {
                Item equippedItem = FindInventoryItemViaID(player.unitName, equippedID);

                if(equippedItem == null)
                {
                    Debug.Log("FAILED TO EQUIP ITEM WITH ID: " + equippedID);
                    continue;
                }

                if(equippedItem is Weapon)
                {
                    player.stats.EquipWeapon(equippedItem as Weapon, true);
                }
                else
                {
                    player.stats.EquipArmour(equippedItem as Armour, true);
                }
            }
        }
    }
    private Item FindInventoryItemViaID(string player, string ID)
    {
        return characterFantasyWorldInventories[player].FirstOrDefault((item) => item.GetID() == ID);
    }

    private Item FindInventoryItemAcrossAllInventoriesViaID(string ID)
    {
        foreach (var inventory in characterFantasyWorldInventories)
        {
            Item foundItem = FindInventoryItemViaID(inventory.Key, ID);

            if (foundItem != null)
                return foundItem;
        }

        return null;
    }
}
