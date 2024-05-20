using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using AnotherRealm;
using System.Linq;

public class ShopBuyUI : BaseShopUI
{
    [Header("Buy UI")]
    [SerializeField] int columnCount = 5;
    [Header("Headers")]
    [SerializeField] ScrollRect gridScrollRect;
    [SerializeField] ScrollRect tabScrollRect;
    [Space(5)]
    [SerializeField] Transform messagesHeader;
    [Header("Titles")]
    [SerializeField] TextMeshProUGUI currentTabTitle;
    [Header("Comparison Area")]
    [SerializeField] TextMeshProUGUI comparisonName;
    [Space(5)]
    [SerializeField] FadeUI comparisonArea;
    [SerializeField] FadeUI comparisonButtons;
    [Header("No Item")]
    [SerializeField] GameObject noItemsArea;
    [Header("Popup Menus")]
    [SerializeField] GameObject confimationArea;
    [Space(5)]
    [SerializeField] InventoryPopupUI purchasePopup;
    [Header("Colors")]
    [SerializeField] Color currentTabColor;
    [SerializeField] Color defaultTabColor;
    [Header("Prefabs")]
    [SerializeField] GameObject shopBuyItemPrefab;
    [Header("Item detail")]
    [SerializeField] GameObject itemDetailHeader;
    [Space(5)]
    [SerializeField] List<InventoryDetailUI> inventoryDetails;

    //Cache
    List<Item> currentShopList = new List<Item>();

    //Indices
    int currentTabIndex = 0;
    int currentShopIndex = 0;

    int currentPopupInventoryIndex = 0;
    int popupCount = 0;

    bool confirmingPurchase = false;
    bool purchaseMade = false;

 
    protected override void UpdateAllUI()
    {
        SetCoinCount();
        UpdateTab(0);
        RebuildShopUI(true);
        UpdateSelected(Vector2.zero);
    }

    //Buttons Events
    public void UpdateInventoryCategory(int category)
    {
        ItemCatergory itemCatergory = (ItemCatergory)category;
        currentShopList = FilterPurchaseStockByCategory(itemCatergory);
    }

    //Shop Functionality
    private void UpdateShopUIByCategory()
    {
        tabScrollRect.content.GetChild(currentTabIndex).GetComponent<Button>().onClick.Invoke();
    }

    private void UpdateTab(int indexChange)
    {
        UpdateTabUI(indexChange, ref currentTabIndex, tabScrollRect, currentTabTitle, currentTabColor, defaultTabColor);

        UpdateShopUIByCategory();
        RebuildShopUI(true);
    }

    //UI
   private void RebuildShopUI(bool resetIndex)
    {
        if (resetIndex)
            currentShopIndex = 0;

        UpdateShopGridUI(currentShopList, shopBuyItemPrefab, true, gridScrollRect, itemDetailHeader, noItemsArea);
        UpdateSelected(Vector2.zero);
    }

    private void UpdateSelected(Vector2 indexChange)
    {
        if (currentShopList.Count == 0)
        {
            canCompare = false;
            UpdateMessageBasedOnSelectedItem();
            UpdateCompareUI(comparisonName, comparisonArea, comparisonButtons);
            return;
        }

        UpdateSelectedShopItemUI(indexChange, ref currentShopIndex, currentShopList, columnCount, gridScrollRect);
        UpdateCompare(0);
    }


    private void UpdateMessageBasedOnSelectedItem()
    {
        Item selectedItem = currentShopList.Count > 0 ? currentShopList[currentShopIndex] : null;
        UpdateMessageBasedOnSelectedItem(selectedItem, messagesHeader, playerToCompareAgainst, confirmingPurchase, purchaseMade);
    }

    private void UpdateCompare(int indexChange)
    {
        UpdateCompareIndex(indexChange);
        UpdateCompareUI(comparisonName, comparisonArea, comparisonButtons);
        UpdateItemDetail(itemDetailHeader, currentShopList[currentShopIndex], inventoryDetails);

        if(!confirmingPurchase)
            UpdateMessageBasedOnSelectedItem();
    }

    //Popup
    private void DisplayPopUp(bool show)
    {
        if (show)
        {
            if (currentShopList.Count == 0) { return; }

            Item selectedItem = currentShopList[currentShopIndex];
            if (!InventoryManager.Instance.CanAfford(selectedItem.buyPrice, myShop.isFantasyShop) || GetItemStock(selectedItem) == 0) 
            {
                AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
                return; 
            }

            AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

            popupCount = 0;
            currentPopupInventoryIndex = 0;

            string title = "Buy " + selectedItem.itemName;
            purchasePopup.Setup(myShop.GetPlayersAtShop(), title);

            UpdateBuyPopupInventory(0, ref currentPopupInventoryIndex, purchasePopup);
            UpdatePopupCount(0);
        }
        else
        {
            purchaseMade = false;
        }

        confimationArea.SetActive(show);
        confirmingPurchase = show;

        UpdateMessageBasedOnSelectedItem();
    }

    private void UpdatePopupCount(int countChange)
    {
        Item selectedItem = currentShopList[currentShopIndex];
        float playerFunds = InventoryManager.Instance.GetMoneyAmount(myShop.isFantasyShop);

        int change = countChange * -1;
        float price = selectedItem.buyPrice <= 0 ? 1 : selectedItem.buyPrice;

        int stock = Mathf.Min(GetItemStock(selectedItem), Mathf.FloorToInt(playerFunds / price));

        PlayPopupScrollSFX(change, popupCount, 1, stock);

        popupCount = Mathf.Clamp(popupCount + change, 1, stock);

        purchasePopup.UpdateCount(popupCount, stock, selectedItem.buyPrice * popupCount);
    }



    private void Purchase()
    {
        Item selectedItem = currentShopList[currentShopIndex];

        if(myShop.PurchaseItem(selectedItem, popupCount, myShop.GetPlayersAtShop()[currentPopupInventoryIndex]))
        {
            AudioManager.Instance.PlaySFX(SFXType.ShopCoin);

            purchaseMade = true;

            float playerFunds = InventoryManager.Instance.GetMoneyAmount(myShop.isFantasyShop);
            int stock = Mathf.Min(GetItemStock(selectedItem), Mathf.FloorToInt(playerFunds / selectedItem.buyPrice));

            UpdateCoinCount();

            RebuildShopUI(false);

            if (stock <= 0)
            {
                DisplayPopUp(false);
            }
            else
            {
                popupCount = 0;
                UpdateBuyPopupInventory(0, ref currentPopupInventoryIndex, purchasePopup);

                UpdatePopupCount(0);
                UpdateMessageBasedOnSelectedItem();
            }
        }
        else
        {
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
        }
    }


    protected override void ResetData()
    {
        myShop.visitedBuySection = true;

        currentShopList.Clear();
        validCompareList.Clear();

        currentTabIndex = 0;
        currentShopIndex = 0;
        compareIndex = 0;

        popupCount = 0;
        currentPopupInventoryIndex = 0;

        confirmingPurchase = false;
        purchaseMade = false;

        playerToCompareAgainst = myShop.GetPlayersAtShop()[0];
    }

    //INput

    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            if (confirmingPurchase)
            {
                Purchase();
            }
            else
            {
                DisplayPopUp(true);
            }
        }
    }

    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
            {
                int indexChange = context.action.name == "ScrollD" ? 1 : -1;

                if (confirmingPurchase)
                {
                    UpdatePopupCount(indexChange);
                }
                else
                {
                    UpdateSelected(new Vector2(0, indexChange));
                }

            }
        }
    }

    private void OnScrollHold(InputAction.CallbackContext context)
    {
        if (!confirmingPurchase) { return; }

        if (context.action.name == "HoldU" || context.action.name == "HoldD")
        {
            if (context.performed)
            {
                int indexChange = context.action.name == "HoldD" ? 1 : -1;
                BeginHoldScrollRoutine(UpdatePopupCount, indexChange);
            }
            else if (context.canceled)
            {
                StopScrollHoldRoutine();
            }
        }
    }

    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "CycleR" || context.action.name == "CycleL")
            {
                int indexChange = context.action.name == "CycleR" ? 1 : -1;

                if (confirmingPurchase)
                {
                    UpdateBuyPopupInventory(indexChange, ref currentPopupInventoryIndex, purchasePopup);
                }
                else
                {
                    UpdateSelected(new Vector2(indexChange, 0));
                }
            }
        }
    }

    private void OnCompareChange(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "CompareR" || context.action.name == "CompareL")
            {
                int indexChange = context.action.name == "CompareR" ? 1 : -1;
                UpdateCompare(indexChange);
            }
        }
    }



    private void OnChangeTab(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "TabR")
            {
                if (confirmingPurchase)
                {
                    UpdateBuyPopupInventory(1, ref currentPopupInventoryIndex, purchasePopup);
                }
                else
                {
                    UpdateTab(1);
                }
            }
            else if (context.action.name == "TabL")
            {
                if (confirmingPurchase)
                {
                    UpdateBuyPopupInventory(1, ref currentPopupInventoryIndex, purchasePopup);
                }
                else
                {
                    UpdateTab(-1);
                }
            }
        }
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed)
        {
            if (confirmingPurchase)
            {
                AudioManager.Instance.PlaySFX(SFXType.TabBack);
                DisplayPopUp(false);
            }
            else
            {
                ReturnToShopFront();
            }
            
        }
    }

    public override void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScrollHold;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCompareChange;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnChangeTab;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScrollHold;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCompareChange;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnChangeTab;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
        }
    }


}
