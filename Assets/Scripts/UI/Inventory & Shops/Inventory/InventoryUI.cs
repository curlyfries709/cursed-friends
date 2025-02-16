
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using AnotherRealm;

public class InventoryUI : MonoBehaviour, IControls
{
    [Header("Values")]
    [SerializeField] int columnCount = 5;
    [Header("Headers")]
    [SerializeField] Transform partyHeader;
    [Space(5)]
    [SerializeField] ScrollRect gridScrollRect;
    [SerializeField] ScrollRect tabScrollRect;
    [Space(5)]
    [SerializeField] Transform itemDetailHeader;
    [Header("Titles")]
    [SerializeField] TextMeshProUGUI currentInventoryTitle;
    [SerializeField] TextMeshProUGUI currentTabTitle;
    [Space(5)]
    [SerializeField] TextMeshProUGUI weightCapacity;
    [SerializeField] TextMeshProUGUI goldCount;
    [Space(5)]
    [SerializeField] TextMeshProUGUI useItemTitle;
    [Header("Areas")] 
    [SerializeField] GameObject selectInventoryArea;
    [SerializeField] GameObject selectDrinkerArea;
    [SerializeField] GameObject noItemsArea;
    [Header("Selected Menu")]
    [SerializeField] GameObject selectedMenu;
    [Header("Popup Menus")]
    [SerializeField] GameObject popupArea;
    [Space(5)]
    [SerializeField] InventoryPopupUI discardPopup;
    [SerializeField] InventoryPopupUI transferPopup;
    [Header("Colors")]
    [SerializeField] Color currentTabColor;
    [SerializeField] Color defaultTabColor;
    [Header("Prefabs")]
    [SerializeField] GameObject inventoryItemPrefab;
    [Header("Controls")]
    [SerializeField] List<Transform> controlHeaders;
    [Header("Component UI")]
    [SerializeField] HUDHealthUI[] unitHealthUIData = new HUDHealthUI[7];
    [Space(5)]
    [SerializeField] List<InventoryDetailUI> inventoryDetails;

    const string myActionMap = "Inventory";

    //Cache
    IInventorySelector activeInventorySelector = null;

    //Lists
    List<Item> currentInventory = new List<Item>();

    List<PlayerGridUnit> allPlayers = new List<PlayerGridUnit>();
    List<PlayerGridUnit> popupInventoryList = new List<PlayerGridUnit>();

    //Indices
    PlayerGridUnit inventoryOwner = null;

    int currentGridIndex = 0;
    int currentTabIndex = 0;
    int currentPlayerIndex = 0;
    int currentPopupInventoryIndex = 0;
    int currentDrinkerIndex = 0;

    int popupCount = 1;

    InventoryPopupUI activePopup = null;

    //Category Checker
    bool isCheckingItemCategory = false;
    ItemCatergory checkedItemCategory;

    private void Awake()
    {
        foreach(Transform controlHeader in controlHeaders)
        {
            ControlsManager.Instance.AddControlHeader(controlHeader);
        }

        ControlsManager.Instance.SubscribeToPlayerInput(myActionMap, this);
    }

    void Start()
    {
        
        //FixInventoryHeader();
    }

    public void Activate(bool activate)
    {
        gameObject.SetActive(activate);

        if(activate)
        {
            Setup(null, 0);
        } 
    }

    public void ActivateInventorySelection(IInventorySelector inventorySelector, ItemCatergory selectionCategory, PlayerGridUnit inventoryOwner)
    {
        gameObject.SetActive(true);
        activeInventorySelector = inventorySelector;
        Setup(inventoryOwner, GetTabIndexFromItemCategory(selectionCategory));
    }

    private void Setup(PlayerGridUnit startingInventory, int startingTabIndex)
    {
        AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

        inventoryOwner = startingInventory;
        currentInventory.Clear();

        allPlayers = PartyManager.Instance.GetAllPlayerMembersInWorld();

        currentGridIndex = 0;
        currentTabIndex = startingTabIndex;
        currentPlayerIndex = 0;
        currentDrinkerIndex = 0;

        popupCount = 1;
        currentPopupInventoryIndex = 0;

        ControlsManager.Instance.SwitchCurrentActionMap(this);

        UpdateAllUI();
    }

    private int GetTabIndexFromItemCategory(ItemCatergory itemCatergory)
    {   
        int index = 0;
        isCheckingItemCategory = true;

        foreach (Transform tab in tabScrollRect.content)
        {
            index = tab.GetSiblingIndex();

            //Invoke the event
            tab.GetComponent<Button>().onClick.Invoke();

            //Check the result
            if (itemCatergory == checkedItemCategory)
                break;
        }

        isCheckingItemCategory = false;
        return index;
    }


    //Buttons Events
    public void UpdateInventoryCategory(int category)
    {
        if(isCheckingItemCategory)
        {
            checkedItemCategory = (ItemCatergory)category;
            return;
        }

        if (!inventoryOwner) { return; }

        ItemCatergory itemCatergory = (ItemCatergory)category;
        currentInventory = InventoryManager.Instance.GetItemListFromCategory(inventoryOwner, itemCatergory, true);
    }

    private void UseOrEquip()
    {
        //Default 0 //Equip 1 //Use 2
        if (activeInventorySelector != null) { return; }
        if (selectedMenu.transform.GetChild(0).gameObject.activeInHierarchy || !selectedMenu.activeInHierarchy || selectDrinkerArea.activeInHierarchy) { return; } 

        Item selectedItem = currentInventory[currentGridIndex];

        Potion potion = selectedItem as Potion;
        Weapon weapon = selectedItem as Weapon;
        Armour armour = selectedItem as Armour;

        if (potion)
        {
            AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

            currentDrinkerIndex = currentPlayerIndex;
            selectDrinkerArea.SetActive(true);
            useItemTitle.text = potion.itemName + " x" + InventoryManager.Instance.GetItemCount(potion, inventoryOwner);

            RebuildInventoryUI(false);
        }
        else if (weapon)
        {
            AudioManager.Instance.PlaySFX(SFXType.EquipItem);

            inventoryOwner.stats.EquipWeapon(weapon);
            RebuildInventoryUI(false);
        }
        else if (armour)
        {
            AudioManager.Instance.PlaySFX(SFXType.EquipItem);

            inventoryOwner.stats.EquipArmour(armour);
            RebuildInventoryUI(false);
        }
    }

    private void OpenPopup(bool activate, bool transfer)
    {
        if (activeInventorySelector != null) { return; }
        if (selectDrinkerArea.activeInHierarchy) { return; }
        if (!selectedMenu.activeInHierarchy && !activePopup) { return; }

        popupCount = 1;
        currentPopupInventoryIndex = 0;

        popupArea.SetActive(activate);
        transferPopup.gameObject.SetActive(transfer && activate);
        discardPopup.gameObject.SetActive(!transfer && activate);

        if (activate)
        {
            AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

            UpdateSelectedItemMenu(false);

            Item selectedItem = currentInventory[currentGridIndex];

            activePopup = transfer ? transferPopup : discardPopup;
            popupInventoryList = new List<PlayerGridUnit>(allPlayers);
            popupInventoryList.Remove(inventoryOwner);

            string title = transfer ? "Transfer " + selectedItem.itemName : "Discard " + selectedItem.itemName;
            activePopup.Setup(popupInventoryList, title);

            if (transfer)
            {
                UpdatePopupInventory(0);
            }

            UpdatePopupCount(0);
        }
        else
        {
            activePopup = null;
            UpdateInventoryList();

            if (currentGridIndex >= currentInventory.Count)
                currentGridIndex = currentInventory.Count - 1;

            RebuildInventoryUI(false);
        }
    }

    private void ConfirmUse()
    {
        AudioManager.Instance.PlaySFX(SFXType.PotionPowerUp);

        Potion potion = currentInventory[currentGridIndex] as Potion;
        PlayerGridUnit drinker = allPlayers[currentDrinkerIndex];

        potion?.UseOutsideCombat(drinker);

        unitHealthUIData[currentDrinkerIndex].UpdateHealth(drinker);
        unitHealthUIData[currentDrinkerIndex].UpdateSP(drinker);
        unitHealthUIData[currentDrinkerIndex].UpdateFP(drinker);

        InventoryManager.Instance.RemoveFromInventory(inventoryOwner, currentInventory[currentGridIndex]);
        UpdateWeight();

        if (InventoryManager.Instance.GetItemCount(currentInventory[currentGridIndex], inventoryOwner) == 0)
        {
            CancelUse();
        }
        else
        {
            useItemTitle.text = potion.itemName + " x" + InventoryManager.Instance.GetItemCount(potion, inventoryOwner);
        }
    }

    private void ConfirmSelectionForInventorySelector()
    {
        if (activeInventorySelector == null) return;

        Item selectedItem = currentInventory[currentGridIndex];

        if (activeInventorySelector.CanSelectItem(selectedItem))
        {
            activeInventorySelector.OnItemSelected(selectedItem);
        }
    }

    private void CancelUse()
    {
        selectDrinkerArea.SetActive(false);
        UpdateInventoryList();
        RebuildInventoryUI(false);
        UpdateSelectedInventory(0);
    }

    private void UpdateInventoryList()
    {
        tabScrollRect.content.GetChild(currentTabIndex).GetComponent<Button>().onClick.Invoke();
    }

    private void ConfirmPopupAction()
    {
        if (!activePopup) { return; }

        bool isTransfer = activePopup == transferPopup;
        Item selectedItem = currentInventory[currentGridIndex];

        if (isTransfer)
        {
            AudioManager.Instance.PlaySFX(SFXType.InventoryTransfer);
            InventoryManager.Instance.TransferToAnotherInventory(inventoryOwner, popupInventoryList[currentPopupInventoryIndex], selectedItem, popupCount);
        }
        else
        {
            //Discarding
            AudioManager.Instance.PlaySFX(SFXType.InventoryDiscard);
            InventoryManager.Instance.RemoveMultipleFromInventory(inventoryOwner, selectedItem, popupCount);
        }

        OpenPopup(false, isTransfer);
    }

    private void UpdatePopupCount(int indexChange)
    {
        if (!activePopup) { return; }

        Item selectedItem = currentInventory[currentGridIndex];
        Weapon weapon = selectedItem as Weapon;
        Armour armour = selectedItem as Armour;

        int stock = InventoryManager.Instance.GetItemCount(selectedItem, inventoryOwner);

        if (inventoryOwner.stats.IsEquipped(weapon) || inventoryOwner.stats.IsEquipped(armour))
        {
            stock = stock - 1;
        }

        if (indexChange != 0)
        {
            if (popupCount + indexChange > stock || popupCount + indexChange < 1)
            {
                AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            }
            else
            {
                AudioManager.Instance.PlaySFX(SFXType.ScrollForward);
            }
        }
        
        popupCount = Mathf.Clamp(popupCount + indexChange, 1, stock);
        activePopup.UpdateCount(popupCount, stock);
    }

    private void UpdatePopupInventory(int indexChange)
    {
        if (!activePopup && activePopup != transferPopup) { return; }

        if(indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.TabForward);

        CombatFunctions.UpdateListIndex(indexChange, currentPopupInventoryIndex, out currentPopupInventoryIndex, popupInventoryList.Count);
        activePopup.UpdateInventoryUI(currentPopupInventoryIndex);
    }

    //UI
    private void SetHealthUI()
    {
        for (int i = 0; i < unitHealthUIData.Length; i++)
        {
            if (i >= allPlayers.Count)
            {
                partyHeader.GetChild(i).gameObject.SetActive(false);
                continue;
            }

            partyHeader.GetChild(i).gameObject.SetActive(true);
            unitHealthUIData[i].SetData(allPlayers[i]);
            unitHealthUIData[i].IsSelected(i == currentPlayerIndex);
        }
    }

    private void UpdateAllUI()
    {
        goldCount.text = InventoryManager.Instance.fantasyMoney.ToString();

        SetHealthUI();
        UpdateTab(0);
        UpdateSelectedInventory(0);
        RebuildInventoryUI(true);
    }

    private void UpdateSelectedInventory(int indexChange)
    {
        int index;

        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        if (selectDrinkerArea.activeInHierarchy)
        {
            CombatFunctions.UpdateListIndex(indexChange, currentDrinkerIndex, out currentDrinkerIndex, allPlayers.Count);
            index = currentDrinkerIndex;
        }
        else
        {
            CombatFunctions.UpdateListIndex(indexChange, currentPlayerIndex, out currentPlayerIndex, allPlayers.Count);
            index = currentPlayerIndex;
        }

        selectInventoryArea.SetActive(!inventoryOwner && !selectDrinkerArea.activeInHierarchy);
        noItemsArea.SetActive(inventoryOwner && currentInventory.Count == 0 && !selectDrinkerArea.activeInHierarchy);

        gridScrollRect.content.gameObject.SetActive(inventoryOwner && !selectDrinkerArea.activeInHierarchy);

        itemDetailHeader.gameObject.SetActive(inventoryOwner);

        PlayerGridUnit currentPlayer = allPlayers[index];

        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (i >= allPlayers.Count)
            {
                continue;
            }
            unitHealthUIData[i].IsSelected(i == index);
        }

        string appendString = selectDrinkerArea.activeInHierarchy ? "" : "'s Inventory";
        currentInventoryTitle.text = currentPlayer.unitName + appendString;

        if (selectDrinkerArea.activeInHierarchy) { return; }

        UpdateWeight();
        RebuildInventoryUI(true);
    }



    private void RebuildInventoryUI(bool resetIndex)
    {
        if (!inventoryOwner) 
        {
            UpdateSelectedItemMenu(false);
            return; 
        }

        UpdateWeight();

        if (currentInventory.Count == 0 || selectDrinkerArea.activeInHierarchy)
        {
            gridScrollRect.content.gameObject.SetActive(false);
            itemDetailHeader.gameObject.SetActive(selectDrinkerArea.activeInHierarchy);
            noItemsArea.SetActive(!selectDrinkerArea.activeInHierarchy);
            UpdateSelectedItemMenu(false);
            return;
        }

        if(resetIndex)
            currentGridIndex = 0;

        noItemsArea.SetActive(false);
        gridScrollRect.content.gameObject.SetActive(true);


        foreach (Transform child in gridScrollRect.content)
        {
            if (child.gameObject.activeInHierarchy)
            {
                child.gameObject.SetActive(false);
            }
        }

        foreach (Item item in currentInventory)
        {
            int itemIndex = currentInventory.IndexOf(item);
            InventoryItemUI ui;

            if (itemIndex >= gridScrollRect.content.childCount)
            {
                GameObject itemPrefab = Instantiate(inventoryItemPrefab, gridScrollRect.content);
                ui = itemPrefab.GetComponent<InventoryItemUI>();
            }
            else
            {
                //Grab Component
                ui = gridScrollRect.content.GetChild(itemIndex).GetComponent<InventoryItemUI>();
            }

            ui.UpdateData(item, inventoryOwner, itemIndex == currentGridIndex);
            ui.gameObject.SetActive(true);
        }

        UpdateItemDetail();
        UpdateSelectedItemMenu(true);

    }


    private void UpdateSelectedItemUI(Vector2 indexChange)
    {
        if (!inventoryOwner || currentInventory.Count == 0 || selectDrinkerArea.activeInHierarchy) 
        {
            UpdateSelectedItemMenu(false);
            itemDetailHeader.gameObject.SetActive(false);
            return; 
        }

        if (indexChange != Vector2.zero)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        CombatFunctions.UpdateGridIndex(indexChange, ref currentGridIndex, columnCount, currentInventory.Count);

        foreach (Transform child in gridScrollRect.content)
        {
            if (!child.gameObject.activeInHierarchy) { continue; }

            int itemIndex = child.GetSiblingIndex();
            InventoryItemUI ui = child.GetComponent<InventoryItemUI>();

            ui.SetSelected(currentInventory[itemIndex], itemIndex == currentGridIndex); 
        }

        CombatFunctions.VerticalScrollToHighlighted(gridScrollRect.content.GetChild(currentGridIndex) as RectTransform, gridScrollRect, currentGridIndex, currentInventory.Count);
        UpdateItemDetail();
        UpdateSelectedItemMenu(true);
    }

    private void UpdateSelectedItemMenu(bool show)
    {
        selectedMenu.SetActive(activeInventorySelector != null ? false : show);

        if (!show) { return; }

        foreach(Transform menu in selectedMenu.transform)
        {
            menu.gameObject.SetActive(false);
        }

        Item selectedItem = currentInventory[currentGridIndex];
        Transform itemTransform = gridScrollRect.content.GetChild(currentGridIndex);

        //Move to pos
        RectTransform targetTransform = itemTransform.GetComponent<InventoryItemUI>().GetMenuTransform();
        selectedMenu.transform.position = targetTransform.position;

        Potion potion = selectedItem as Potion;
        Weapon weapon = selectedItem as Weapon;
        Armour armour = selectedItem as Armour;


        if ((inventoryOwner.stats.IsEquipped(weapon) && InventoryManager.Instance.GetItemCount(weapon, inventoryOwner) == 1)
            || (inventoryOwner.stats.IsEquipped(armour) && InventoryManager.Instance.GetItemCount(armour, inventoryOwner) == 1))
        {
            selectedMenu.SetActive(false);
        }
        else if(((potion && potion.useableOutsideCombat) || (selectedItem.itemCatergory == ItemCatergory.Books)) && inventoryOwner.stats.CanUseItem(selectedItem))
        {
            selectedMenu.transform.GetChild(2).gameObject.SetActive(true);
        }
        else if ((weapon && !inventoryOwner.stats.IsEquipped(weapon) && inventoryOwner.stats.CanEquip(weapon)) || (armour && !inventoryOwner.stats.IsEquipped(armour) && inventoryOwner.stats.CanEquip(armour)))
        {
            selectedMenu.transform.GetChild(1).gameObject.SetActive(true);
        }
        else
        {
            selectedMenu.transform.GetChild(0).gameObject.SetActive(true);
        }
    }

    private void UpdateTab(int indexChange)
    {
        if (selectDrinkerArea.activeInHierarchy) { return; }

        if(indexChange != 0)
        {
            if(activeInventorySelector != null && !activeInventorySelector.CanSwitchInventoryCategory()) { return; }
            AudioManager.Instance.PlaySFX(SFXType.TabForward);
        }

        CombatFunctions.UpdateListIndex(indexChange, currentTabIndex, out currentTabIndex, tabScrollRect.content.childCount);

        foreach (Transform icon in tabScrollRect.content)
        {
            bool isSelected = icon.GetSiblingIndex() == currentTabIndex;

            Color color = isSelected ? currentTabColor : defaultTabColor;
            icon.GetComponent<Image>().color = color;
        }

        currentTabTitle.text = tabScrollRect.content.GetChild(currentTabIndex).name;
        CombatFunctions.HorizontallScrollToHighlighted(tabScrollRect.content.GetChild(currentTabIndex) as RectTransform, tabScrollRect, currentTabIndex, tabScrollRect.content.childCount);

        UpdateInventoryList();
        RebuildInventoryUI(true);
    }

    private void UpdateItemDetail()
    {
        itemDetailHeader.gameObject.SetActive(true);
        Item selectedItem = currentInventory[currentGridIndex];

        foreach(InventoryDetailUI detail in inventoryDetails)
        {
            detail.SetData(selectedItem, inventoryOwner);
        }
    }

    private void UpdateWeight()
    {
        PlayerGridUnit currentPlayer = allPlayers[currentPlayerIndex];

        float currentWeight = InventoryManager.Instance.GetCurrentWeight(currentPlayer);
        float maxWeight = InventoryManager.Instance.GetWeightCapacity(currentPlayer);


        if (currentWeight > maxWeight)
        {
            weightCapacity.text = "<color=\"red\">" + currentWeight.ToString() + "</color>" + "/" + maxWeight.ToString();
        }
        else
        {
            weightCapacity.text = currentWeight.ToString() + "/" + maxWeight.ToString();
        }
    }

    //Helper

    private void FixInventoryHeader()
    {
        foreach (Transform child in gridScrollRect.content)
        {
            Destroy(child.gameObject);
        }
    }
    //INput
    private void OnUseOrEquip(InputAction.CallbackContext context)
    {
        if (context.action.name != "Use") { return; }

        if (context.performed)
        {
            UseOrEquip();
        }
    }

    private void OnTransfer(InputAction.CallbackContext context)
    {
        if (context.action.name != "Transfer") { return; }

        if (context.performed)
        {
            OpenPopup(true, true);
        }
    }

    private void OnDiscard(InputAction.CallbackContext context)
    {
        if (context.action.name != "Discard") { return; }

        if (context.performed)
        {
            OpenPopup(true, false);
        }
    }


    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
            {
                int indexChange = context.action.name == "ScrollD" ? 1 : -1;

                if (activePopup)
                {
                    UpdatePopupCount(indexChange * -1);
                }
                else if (selectDrinkerArea.activeInHierarchy || !inventoryOwner)
                {
                    UpdateSelectedInventory(indexChange);
                }
                else
                {
                    UpdateSelectedItemUI(new Vector2(0, indexChange));
                }

            }
        }
    }

    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.performed && inventoryOwner)
        {
            if (context.action.name == "CycleR")
            {
                if (activePopup)
                {
                    UpdatePopupInventory(1);
                }
                else
                {
                    UpdateSelectedItemUI(new Vector2(1, 0));
                }
                
            }
            else if (context.action.name == "CycleL")
            {
                if (activePopup)
                {
                    UpdatePopupInventory(-1);
                }
                else
                {
                    UpdateSelectedItemUI(new Vector2(-1, 0));
                }
            }
        }
    }


    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            SelectOption();
        }
    }


    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed)
        {
            AudioManager.Instance.PlaySFX(SFXType.TabBack);

            if (activePopup)
            {
                OpenPopup(false, false);
            }
            else if (selectDrinkerArea.activeInHierarchy)
            {
                CancelUse();
            }
            else if (CanSwitchInventory())
            {
                inventoryOwner = null;
                UpdateSelectedInventory(0);
            }
            else
            {
                if (activeInventorySelector != null)
                {
                    activeInventorySelector.OnCancel();
                    return;
                }

                InventoryManager.Instance.ActivateInventoryUIFromPhone(false);
            }
        }
    }

    private void OnChangeTab(InputAction.CallbackContext context)
    {
        if (context.performed && inventoryOwner)
        {
            if (context.action.name == "TabR")
            {
                if (activePopup)
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
                if (activePopup)
                {
                    UpdatePopupInventory(-1);
                }
                else
                {
                    UpdateTab(-1);
                }
            }
        }
    }

    private bool CanSwitchInventory()
    {
        return inventoryOwner && (activeInventorySelector == null || activeInventorySelector.CanSwitchToAnotherInventory());
    }

    private void SelectOption()
    {
        if (activeInventorySelector != null)
        {
            ConfirmSelectionForInventorySelector();
            return;
        }
        else if (activePopup)
        {
            ConfirmPopupAction();
            return;
        }
        else if (selectDrinkerArea.activeInHierarchy)
        {
            ConfirmUse();
            return;
        }

        if (inventoryOwner){ return; }

        AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);
        inventoryOwner = allPlayers[currentPlayerIndex];

        UpdateInventoryList();
        UpdateSelectedInventory(0);
    }


    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnChangeTab;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnUseOrEquip;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnTransfer;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnDiscard;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnChangeTab;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnUseOrEquip;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnTransfer;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnDiscard;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }




}
