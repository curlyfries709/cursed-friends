using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using DG.Tweening;
using AnotherRealm;
using UnityEngine.UI;
using System.Linq;

public abstract class BaseShopUI : MonoBehaviour, IControls
{
    [Header("Main")]
    [SerializeField] protected Shop myShop;
    [SerializeField] protected GameObject currentShopActionCam;
    [Space(5)]
    [SerializeField] protected TextMeshProUGUI fantasyCoinCount;
    [Space(5)]
    [SerializeField] List<Transform> controlHeaders;

    const string myActionMap = "Shop";
    float goldAnimTime = 0.5f;

    protected PlayerGridUnit playerToCompareAgainst = null;
    protected bool canCompare = false;

    protected List<PlayerGridUnit> validCompareList = new List<PlayerGridUnit>();
    protected int compareIndex = 0;

    public Item selectedEquipment { get; protected set; } = null;

    //Infinite Scroll Behaviour
    protected bool holdingScroll = false;
    protected Coroutine currentCoroutine = null;

    protected float timeBetweenUpdateOnHold = 0.15f;

    private void Awake()
    {
        foreach (Transform controlHeader in controlHeaders)
        {
            ControlsManager.Instance.AddControlHeader(controlHeader);
        }

        ControlsManager.Instance.SubscribeToPlayerInput(myActionMap, this);
    }



    private void OnEnable()
    {
        ResetData();

        UpdateAllUI();

        ActivateUI(true);

        ControlsManager.Instance.SwitchCurrentActionMap(this);
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }

    public void ActivateUI(bool show)
    {
        gameObject.SetActive(show);
        currentShopActionCam.SetActive(show);
    }

    protected void ReturnToShopFront()
    {
        AudioManager.Instance.PlaySFX(SFXType.TabBack);
        ActivateUI(false);
        myShop.ReturnToShopFront();
    }

    protected List<Item> FilterPurchaseStockByCategory(ItemCatergory catergory)
    {
        List<Item> filteredByCategory = new List<Item>();

        foreach(var stock in myShop.AvailableStock())
        {
            if (stock.Key.itemCatergory == catergory && !filteredByCategory.Contains(stock.Key))
                filteredByCategory.Add(stock.Key);
        }

        //Sort List By New & Old
        List<Item> newItems = new List<Item>();
        List<Item> oldItems = new List<Item>();

        foreach(Item item in filteredByCategory)
        {
            if (myShop.IsItemNew(item))
            {
                newItems.Add(item);
            }
            else
            {
                oldItems.Add(item);
            }
        }

        //Sort List By Level
        newItems = newItems.OrderByDescending((item) => item.requiredLevel).ToList();
        oldItems = oldItems.OrderByDescending((item) => item.requiredLevel).ToList();

        return newItems.Concat(oldItems).ToList();
    }

    protected List<Item> FilterSellStockByCategory(ItemCatergory catergory)
    {
        List<Item> filteredByCategory = new List<Item>();

        foreach (PlayerGridUnit player in myShop.GetPlayersAtShop())
        {
            List<Item> inventory = InventoryManager.Instance.GetItemListFromCategory(player, catergory, false);

            foreach(Item item in inventory)
            {
                Weapon weapon = item as Weapon;
                Armour armour = item as Armour;

                //If Sell Price is 0, Item cannot be sold. E.G Player Uniform
                if (item.GetSellPrice() <= 0 || filteredByCategory.Contains(item) || (player.stats.IsEquipped(weapon) && InventoryManager.Instance.GetItemCount(weapon, player) == 1) 
                    || (player.stats.IsEquipped(armour) && InventoryManager.Instance.GetItemCount(armour, player) == 1))
                {
                    continue;
                }

                filteredByCategory.Add(item);
            }
        }

        //return filteredByCategory.OrderBy((item) => item.requiredLevel).ToList();
        return filteredByCategory.OrderBy((item) => item.itemName).ToList();
    }

    protected List<Item> FilterPlayerInventoryByCategory(PlayerGridUnit player, ItemCatergory category)
    {
        return InventoryManager.Instance.GetItemListFromCategory(player, category, false);
    }

    protected int GetItemStock(Item item)
    {
        return myShop.AvailableStock()[item];
    }

    protected int GetEquippedCount(Item item, PlayerGridUnit player)
    {
        return myShop.GetEquippedCount(item, player);
    }

    protected int GetSellStock(Item item)
    {
        return InventoryManager.Instance.GetItemCountAcrossAllInventories(item, myShop.GetPlayersAtShop());
    }

    protected List<PlayerGridUnit> GetAllPlayersAtShop()
    {
        return myShop.GetPlayersAtShop();
    }

    //Continious Scroll Behaviour
    protected void BeginHoldScrollRoutine(Action<int> function, int functionIndexChange)
    {
        holdingScroll = true;
        currentCoroutine = StartCoroutine(UpdateScrollMethodContinious(function, functionIndexChange));
    }

    protected void StopScrollHoldRoutine()
    {
        holdingScroll = false;

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
    }

    IEnumerator UpdateScrollMethodContinious(Action<int> scrollFunction, int change)
    {
        while (holdingScroll)
        {
            scrollFunction(change);
            yield return new WaitForSeconds(timeBetweenUpdateOnHold);
        }
    }

    //UI HELPER METHODS

    protected void SetCoinCount()
    {
        fantasyCoinCount.text = InventoryManager.Instance.fantasyMoney.ToString();
    }

    protected void PlayPopupScrollSFX(int change, int currentCount, int minCount, int maxCount)
    {
        if (change != 0)
        {
            if (currentCount + change >= minCount && currentCount + change <= maxCount)
            {
                AudioManager.Instance.PlaySFX(SFXType.ScrollForward);
            }
            else
            {
                AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            }
        }
    }

    protected void UpdateTabUI(int indexChange, ref int tabIndex, ScrollRect tabScrollRect, TextMeshProUGUI tabTitle, Color selectedTabColor, Color defaultTabColor)
    {
        if(indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.TabForward);

        CombatFunctions.UpdateListIndex(indexChange, tabIndex, out tabIndex, tabScrollRect.content.childCount);

        foreach (Transform icon in tabScrollRect.content)
        {
            bool isSelected = icon.GetSiblingIndex() == tabIndex;

            Color color = isSelected ? selectedTabColor : defaultTabColor;
            icon.GetComponent<Image>().color = color;
        }

        tabTitle.text = tabScrollRect.content.GetChild(tabIndex).name;
        CombatFunctions.HorizontallScrollToHighlighted(tabScrollRect.content.GetChild(tabIndex) as RectTransform, tabScrollRect, tabIndex, tabScrollRect.content.childCount);
    }

    protected void UpdateTabUI(int indexChange, ref int tabIndex, Transform tabHeader, TextMeshProUGUI tabTitle, Color selectedTabColor, Color defaultTabColor)
    {
        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.TabForward);

        CombatFunctions.UpdateListIndex(indexChange, tabIndex, out tabIndex, tabHeader.childCount);

        foreach (Transform icon in tabHeader)
        {
            bool isSelected = icon.GetSiblingIndex() == tabIndex;

            Color color = isSelected ? selectedTabColor : defaultTabColor;
            icon.GetComponent<Image>().color = color;
        }

        tabTitle.text = tabHeader.GetChild(tabIndex).name;
    }

    protected void UpdateShopGridUI(List<Item> shopList, GameObject shopItemPrefab, bool isBuying, ScrollRect shopScrollRect, GameObject itemDetailHeader, GameObject noItemsArea)
    {
        //Activate Areas
        shopScrollRect.content.gameObject.SetActive(shopList.Count > 0);
        itemDetailHeader.SetActive(shopList.Count > 0);
        noItemsArea.SetActive(shopList.Count == 0);

        if (shopList.Count == 0) { return; }


        foreach (Transform child in shopScrollRect.content)
        {
            if (child.gameObject.activeInHierarchy)
            {
                child.gameObject.SetActive(false);
            }
        }

        foreach (Item item in shopList)
        {
            int itemIndex = shopList.IndexOf(item);
            ShopItemUI ui;

            if (itemIndex >= shopScrollRect.content.childCount)
            {
                GameObject itemPrefab = Instantiate(shopItemPrefab, shopScrollRect.content);
                ui = itemPrefab.GetComponent<ShopItemUI>();
            }
            else
            {
                //Grab Component
                ui = shopScrollRect.content.GetChild(itemIndex).GetComponent<ShopItemUI>();
            }

            int count = isBuying ? GetItemStock(item) : GetSellStock(item);

            ui.UpdateData(item, count, false, myShop, isBuying);
            ui.gameObject.SetActive(true);
        }
    }

    protected void UpdateInventoryGridUI(PlayerGridUnit inventoryOwner, List<Item> inventoryList, GameObject inventoryItemPrefab, ScrollRect inventoryScrollRect, 
        GameObject itemDetailHeader, GameObject noItemsArea, bool enchanting)
    {
        //Activate Areas
        inventoryScrollRect.content.gameObject.SetActive(inventoryList.Count > 0);
        itemDetailHeader.SetActive(inventoryList.Count > 0);
        noItemsArea.SetActive(inventoryList.Count == 0);

        if (inventoryList.Count == 0) { return; }


        foreach (Transform child in inventoryScrollRect.content)
        {
            if (child.gameObject.activeInHierarchy)
            {
                child.gameObject.SetActive(false);
            }
        }

        foreach (Item item in inventoryList)
        {
            int itemIndex = inventoryList.IndexOf(item);
            InventoryItemUI ui;

            if (itemIndex >= inventoryScrollRect.content.childCount)
            {
                GameObject itemPrefab = Instantiate(inventoryItemPrefab, inventoryScrollRect.content);
                ui = itemPrefab.GetComponent<InventoryItemUI>();
            }
            else
            {
                //Grab Component
                ui = inventoryScrollRect.content.GetChild(itemIndex).GetComponent<InventoryItemUI>();
            }

            ui.UpdateData(item, inventoryOwner, false);
            ui.SetIneligible(myShop, item, selectedEquipment, enchanting);
            ui.gameObject.SetActive(true);
        }
    }

    protected void UpdateSelectedShopItemUI(Vector2 indexChange, ref int shopIndex, List<Item> shopList, int gridColumnCount, ScrollRect shopScrollRect)
    {
        if(indexChange != Vector2.zero)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        CombatFunctions.UpdateGridIndex(indexChange, ref shopIndex, gridColumnCount, shopList.Count);

        foreach (Transform child in shopScrollRect.content)
        {
            if (!child.gameObject.activeInHierarchy) { continue; }

            int itemIndex = child.GetSiblingIndex();
            ShopItemUI ui = child.GetComponent<ShopItemUI>();

            ui.SetSelected(shopList[itemIndex], itemIndex == shopIndex);
        }

        CombatFunctions.VerticalScrollToHighlighted(shopScrollRect.content.GetChild(shopIndex) as RectTransform, shopScrollRect, shopIndex, shopList.Count);
        playerToCompareAgainst = GetPlayerToCompareTo(shopList[shopIndex]);
    }

    protected void UpdateSelectedInventoryItemUI(Vector2 indexChange, ref int inventoryIndex, List<Item> inventoryList, int gridColumnCount, ScrollRect shopScrollRect)
    {
        if(indexChange != Vector2.zero)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        CombatFunctions.UpdateGridIndex(indexChange, ref inventoryIndex, gridColumnCount, inventoryList.Count);

        foreach (Transform child in shopScrollRect.content)
        {
            if (!child.gameObject.activeInHierarchy) { continue; }

            int itemIndex = child.GetSiblingIndex();
            InventoryItemUI ui = child.GetComponent<InventoryItemUI>();

            ui.SetSelected(inventoryList[itemIndex], itemIndex == inventoryIndex);
        }

        CombatFunctions.VerticalScrollToHighlighted(shopScrollRect.content.GetChild(inventoryIndex) as RectTransform, shopScrollRect, inventoryIndex, inventoryList.Count);
    }

    protected PlayerGridUnit UpdateInventoryOwner(int indexChange, ref int ownerIndex, TextMeshProUGUI ownerName)
    {
        if(indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.TabForward);

        CombatFunctions.UpdateListIndex(indexChange, ownerIndex, out ownerIndex, GetAllPlayersAtShop().Count);

        PlayerGridUnit inventoryOwner = GetAllPlayersAtShop()[ownerIndex];

        playerToCompareAgainst = inventoryOwner;
        ownerName.text = inventoryOwner.unitName;

        return inventoryOwner;
    }

    protected void UpdateItemDetail(GameObject itemDetailHeader, Item selectedItem, List<InventoryDetailUI> inventoryDetails)
    {
        itemDetailHeader.SetActive(true);

        foreach (InventoryDetailUI detail in inventoryDetails)
        {
            detail.SetData(selectedItem, playerToCompareAgainst);
        }
    }

    protected void SetEnchanmentNameHeader(Transform header)
    {
        Weapon weapon = selectedEquipment as Weapon;
        Armour armour = selectedEquipment as Armour;

        int numOfSlots = weapon ? weapon.numOfEnchantmentSlots : armour.numOfEnchantmentSlots;
        List<Enchantment> enchantments = weapon ? weapon.infusedEnchantments.Concat(weapon.embeddedEnchantments).ToList() : armour.infusedEnchantments.Concat(armour.embeddedEnchantments).ToList();

        foreach (Transform child in header)
        {
            int childIndex = child.GetSiblingIndex();
            child.gameObject.SetActive(childIndex < numOfSlots);

            if (childIndex >= numOfSlots) { continue; }

            bool isEmptySlot = childIndex >= enchantments.Count;

            child.GetChild(0).gameObject.SetActive(isEmptySlot); //Empty: 0
            child.GetChild(1).gameObject.SetActive(!isEmptySlot && enchantments[childIndex] is InfusedEnchantment); //Infused: 1
            child.GetChild(2).gameObject.SetActive(!isEmptySlot && !(enchantments[childIndex] is InfusedEnchantment)); // Fill: 2

            //Set Name. Index 3
            if(child.childCount >= 4)
                child.GetChild(3).GetComponent<TextMeshProUGUI>().text = isEmptySlot ? "Empty" : enchantments[childIndex].itemName;
        }
    }

    protected void UpdateEnchantmentHeader(Transform header, int selectedIndex, Color selectedColour, Color defaultColor)
    {
        foreach (Transform child in header)
        {
            if (child.gameObject.activeInHierarchy)
            {
                bool isSelected = child.GetSiblingIndex() == selectedIndex;

                child.GetChild(3).GetComponent<TextMeshProUGUI>().color = isSelected ? selectedColour: defaultColor;
                child.GetChild(4).gameObject.SetActive(isSelected);
            }  
        }
    }

    protected PlayerGridUnit GetPlayerToCompareTo(Item selectedItem)
    {
        Weapon weapon = selectedItem as Weapon;
        Armour armour = selectedItem as Armour;

        List<PlayerGridUnit> allPlayersAtShop = myShop.GetPlayersAtShop();
        validCompareList.Clear();

        canCompare = false;

        if (weapon)
        {
            canCompare = true;
            validCompareList = allPlayersAtShop.Where((player) => player.stats.data.proficientWeaponCategory == weapon.category).ToList();
        }
        else if (armour)
        {
            canCompare = true;
            validCompareList = allPlayersAtShop.Where((player) => armour.raceRestriction.Contains(player.stats.data.race)).ToList();
        }

        if (playerToCompareAgainst && validCompareList.Contains(playerToCompareAgainst))
        {
            compareIndex = validCompareList.IndexOf(playerToCompareAgainst);
            return playerToCompareAgainst;
        }
        else if (validCompareList.Count > 0)
        {
            compareIndex = 0;
            return validCompareList[0];
        }

        compareIndex = 0;
        return allPlayersAtShop[0];
    }

    protected void UpdateMessageBasedOnSelectedItem(Item selectedItem, Transform messagesHeader, PlayerGridUnit playerToCompareTo, bool confirmingPurchase, bool purchaseMade)
    {
        ShopKeeperMessage messageToDisplay = null;

        foreach (Transform child in messagesHeader)
        {
            child.gameObject.SetActive(false);
            ShopKeeperMessage message = child.GetComponent<ShopKeeperMessage>();

            if (!messageToDisplay && message.DisplayMessage(selectedItem, myShop, playerToCompareTo, this, confirmingPurchase, purchaseMade))
                messageToDisplay = message;
        }

        messageToDisplay.gameObject.SetActive(true);
    }


    protected bool UpdateCompareIndex(int indexChange)
    {
        if(!canCompare || validCompareList.Count <= 1) { return false; }

        if(indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.TabForward);

        CombatFunctions.UpdateListIndex(indexChange, compareIndex, out compareIndex, validCompareList.Count);
        playerToCompareAgainst = validCompareList[compareIndex];
        return true;
    }

    protected void UpdateCompareUI(TextMeshProUGUI comparerName, FadeUI compareArea, FadeUI compareButtons)
    {
        if(playerToCompareAgainst)
            comparerName.text = playerToCompareAgainst.unitName;

        compareArea.Fade(canCompare);
        compareButtons.Fade(validCompareList.Count > 1);
    }

    protected void UpdateCoinCount()
    {
        int newGold = InventoryManager.Instance.fantasyMoney;
        int currentGold = Int32.Parse(fantasyCoinCount.text);

        fantasyCoinCount.color = newGold >= currentGold ? InventoryManager.Instance.earnCoinColor : InventoryManager.Instance.spendCoinColor;

        DOTween.To(() => currentGold, x => currentGold = x, newGold, goldAnimTime).OnUpdate(() => fantasyCoinCount.text = currentGold.ToString()).OnComplete(() => RevertCoinToDefaultColour());
    }

    private void RevertCoinToDefaultColour()
    {
        fantasyCoinCount.color = InventoryManager.Instance.defaultCoinColor;
    }

    //POP UP
    protected void UpdateBuyPopupInventory(int indexChange, ref int popupInventoryIndex, InventoryPopupUI popup)
    {
        if(indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.TabForward);

        CombatFunctions.UpdateListIndex(indexChange, popupInventoryIndex, out popupInventoryIndex, myShop.GetPlayersAtShop().Count);
        popup.UpdateInventoryUI(popupInventoryIndex);
    }

    protected bool UpdateSellPopupInventory(int indexChange, ref int popupInventoryIndex, Item selectedItem, InventoryPopupUI popup)
    {
        List<PlayerGridUnit> inventoryList = GetSellInventories(selectedItem);

        if (inventoryList.Count == 0)
            return false;

        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.TabForward);

        CombatFunctions.UpdateListIndex(indexChange, popupInventoryIndex, out popupInventoryIndex, inventoryList.Count);
        popup.Setup(inventoryList, "Sell " + selectedItem.itemName);
        popup.UpdateInventoryUI(popupInventoryIndex);

        return true;
    }

    protected List<PlayerGridUnit> GetSellInventories(Item selectedItem)
    {
        return myShop.GetPlayersAtShop().Where(
            (player) => InventoryManager.Instance.HasItem(selectedItem, player) && InventoryManager.Instance.GetItemCount(selectedItem, player) - GetEquippedCount(selectedItem, player) > 0).ToList();
    }

    protected abstract void UpdateAllUI();

    protected abstract void ResetData();

    public abstract void ListenToInput(bool listen);
}

