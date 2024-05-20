using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using AnotherRealm;
using System.Linq;

public class ShopSellUI : BaseShopUI
{
    [Header("Sell UI")]
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
    [SerializeField] InventoryPopupUI sellPopup;
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
    List<Item> currentSellList = new List<Item>();
    Item currentSellItem = null;

    //Indices
    int currentTabIndex = 0;
    int currentSellIndex = 0;

    int currentPopupInventoryIndex = 0;
    int popupCount = 0;

    bool confirmingSell = false;


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
        currentSellList = FilterSellStockByCategory(itemCatergory);
    }

    //Shop Functionality
    private void UpdateShopUIByCategory()
    {
        tabScrollRect.content.GetChild(currentTabIndex).GetComponent<Button>().onClick.Invoke();
    }

    private void UpdateTab(int indexChange, bool resetSelectedIndex = true)
    {
        UpdateTabUI(indexChange, ref currentTabIndex, tabScrollRect, currentTabTitle, currentTabColor, defaultTabColor);

        UpdateShopUIByCategory();
        RebuildShopUI(resetSelectedIndex);
    }

    //UI
    private void RebuildShopUI(bool resetIndex)
    {
        if (resetIndex)
            currentSellIndex = 0;

        UpdateShopGridUI(currentSellList, shopBuyItemPrefab, false, gridScrollRect, itemDetailHeader, noItemsArea);
        UpdateSelected(Vector2.zero);
    }

    private void UpdateSelected(Vector2 indexChange)
    {
        if (currentSellList.Count == 0)
        {
            canCompare = false;
            UpdateMessageBasedOnSelectedItem();
            UpdateCompareUI(comparisonName, comparisonArea, comparisonButtons);
            return;
        }

        UpdateSelectedShopItemUI(indexChange, ref currentSellIndex, currentSellList, columnCount, gridScrollRect);
        UpdateCompare(0);
    }


    private void UpdateMessageBasedOnSelectedItem()
    {
        Item selectedItem;

        if (confirmingSell)
        {
            selectedItem = currentSellItem;
        }
        else
        {
            selectedItem = currentSellList.Count > 0 ? currentSellList[currentSellIndex] : null;
        }

        UpdateMessageBasedOnSelectedItem(selectedItem, messagesHeader, playerToCompareAgainst, confirmingSell, false);
    }

    private void UpdateCompare(int indexChange)
    {
        UpdateCompareIndex(indexChange);
        UpdateCompareUI(comparisonName, comparisonArea, comparisonButtons);
        UpdateItemDetail(itemDetailHeader, currentSellList[currentSellIndex], inventoryDetails);

        if (!confirmingSell)
            UpdateMessageBasedOnSelectedItem();
    }

    //Popup
    private void DisplayPopUp(bool show)
    {
        if (show)
        {
            if(currentSellList.Count == 0) { return; }

            Item selectedItem = currentSellList[currentSellIndex];
            currentSellItem = selectedItem;

            popupCount = 0;
            currentPopupInventoryIndex = 0;

            if (!UpdateSellPopupInventory(0, ref currentPopupInventoryIndex, selectedItem, sellPopup)) { return; }

            AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);
            UpdatePopupCount(0);
        }

        confimationArea.SetActive(show);
        confirmingSell = show;

        UpdateMessageBasedOnSelectedItem();
    }

    private void UpdatePopupCount(int countChange)
    {
        //Item selectedItem = currentSellList[currentSellIndex];
        Item selectedItem = currentSellItem;
        PlayerGridUnit currentSellInvetory = GetSellInventories(selectedItem)[currentPopupInventoryIndex];

        int change = countChange * -1;
        int stock = InventoryManager.Instance.GetItemCount(selectedItem, currentSellInvetory) - GetEquippedCount(selectedItem, currentSellInvetory);

        PlayPopupScrollSFX(change, popupCount, 1, stock);

        popupCount = Mathf.Clamp(popupCount + change, 1, stock);
        sellPopup.UpdateCount(popupCount, stock, selectedItem.GetSellPrice() * popupCount);
    }

    private void UpdatePopupInventory(int indexChange)
    {
        UpdateSellPopupInventory(indexChange, ref currentPopupInventoryIndex, currentSellList[currentSellIndex], sellPopup);
        UpdatePopupCount(0);
    }


    private void Sell()
    {
        //Item selectedItem = currentSellList[currentSellIndex];
        Item selectedItem = currentSellItem;
        PlayerGridUnit selectedPlayer = GetSellInventories(selectedItem)[currentPopupInventoryIndex];

        myShop.SellItem(selectedItem, popupCount, selectedPlayer);

        UpdateCoinCount();

        //UpdateTab(0, false);

        if (!UpdateSellPopupInventory(0, ref currentPopupInventoryIndex, selectedItem, sellPopup))
        {
            UpdateTab(0, false);
            DisplayPopUp(false);
        }
        else
        {
            popupCount = 0;
            UpdatePopupCount(0);
            UpdateMessageBasedOnSelectedItem();
        }
    }

    private void SellAll()
    {
        //Item selectedItem = currentSellList[currentSellIndex];

        myShop.SellAll(currentSellItem);

        UpdateCoinCount();

        UpdateTab(0, false);

        DisplayPopUp(false);
    }


    protected override void ResetData()
    {
        myShop.visitedBuySection = true;

        currentSellList.Clear();
        validCompareList.Clear();

        currentTabIndex = 0;
        currentSellIndex = 0;
        compareIndex = 0;

        popupCount = 0;
        currentPopupInventoryIndex = 0;

        confirmingSell = false;

        playerToCompareAgainst = myShop.GetPlayersAtShop()[0];
    }

    //INput

    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            if (confirmingSell)
            {
                Sell();
            }
            else
            {
                DisplayPopUp(true);
            }
        }
    }

    private void OnSellAll(InputAction.CallbackContext context)
    {
        if (context.action.name != "SellAll") { return; }

        if (context.performed && confirmingSell)
        {
            SellAll();
        }
    }

    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
            {
                int indexChange = context.action.name == "ScrollD" ? 1 : -1;

                if (confirmingSell)
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
        if (!confirmingSell) { return; }

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

                if (confirmingSell)
                {
                    UpdatePopupInventory(indexChange);
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
                if (confirmingSell)
                {
                    UpdatePopupInventory(1);
                }
                else
                {
                    UpdateTab(1);
                }
            }
            else if (context.action.name == "TabL")
            {
                if (confirmingSell)
                {
                    UpdatePopupInventory(1);
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
            if (confirmingSell)
            {
                //Play SFX
                AudioManager.Instance.PlaySFX(SFXType.TabBack);

                UpdateTab(0, false);
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
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSellAll;

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
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSellAll;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCompareChange;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnChangeTab;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
        }
    }


}
