using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Linq;

public class ShopEnchantUI : BaseShopUI
{
    [Header("Enchant UI")]
    [SerializeField] int columnCount = 3;
    [Header("Headers")]
    [SerializeField] ScrollRect gridScrollRect;
    [SerializeField] Transform equipmentTabHeader;
    [Space(5)]
    [SerializeField] Transform defaultMessagesHeader;
    [SerializeField] Transform enchantmentMessagesHeader;
    [Space(5)]
    [SerializeField] GameObject defaultMessageArea;
    [Header("Titles")]
    [SerializeField] TextMeshProUGUI currentTabTitle;
    [Header("Tabs")]
    [SerializeField] GameObject selectEquipmentTab;
    [SerializeField] GameObject selectEnchantmentTab;
    [Header("Inventory Area")]
    [SerializeField] TextMeshProUGUI inventoryName;
    [Space(5)]
    [SerializeField] FadeUI inventoryArea;
    [Header("Selected Equipment Area")]
    [SerializeField] FadeUI selectedEquipmentArea;
    [SerializeField] TextMeshProUGUI selectedEquipmentAreaTitle;
    [Space(5)]
    [SerializeField] Image equipmentIcon;
    [SerializeField] Transform equipmentEnchantmentHeader;
    [Space(5)]
    [SerializeField] TextMeshProUGUI selectedEnchantmentPrice;
    [Header("No Item")]
    [SerializeField] GameObject noItemsArea;
    [Header("EnchantmentArea")]
    [SerializeField] GameObject enchantmentArea;
    [Space(5)]
    [SerializeField] TextMeshProUGUI selectedEnchantmentName;
    [SerializeField] TextMeshProUGUI selectedEnchantmentEffect;
    [SerializeField] Image selectedEnchantmentIcon;
    [Space(5)]
    [SerializeField] Transform selectedEquipmentEnchantmentHeader;
    [SerializeField] TextMeshProUGUI selectedEquipmentName;
    [SerializeField] TextMeshProUGUI selectedEquipmentSummary;
    [SerializeField] Image selectedEquipmentIcon;
    [Space(5)]
    [SerializeField] TextMeshProUGUI enchantPrice;
    [Header("Colors")]
    [SerializeField] Color currentTabColor;
    [SerializeField] Color defaultTabColor;
    [Header("Prefabs")]
    [SerializeField] GameObject inventoryItemPrefab;
    [Header("Item detail")]
    [SerializeField] GameObject itemDetailHeader;
    [Space(5)]
    [SerializeField] List<InventoryDetailUI> inventoryDetails;

    //Player Data
    PlayerGridUnit currentInventory = null;

    PlayerGridUnit equipmentOwner = null;
    PlayerGridUnit enchantmentOwner = null;

    //Indices
    int playerIndex = 0;
    int currentTabIndex = 0;
    int itemIndex = 0;

    //Bools
    bool selectingEnchantment = false;

    bool confirmingPurchase = false;
    bool purchaseMade = false;

    Enchantment selectedEnchantment = null;
    List<Item> currentInventoryList = new List<Item>();

    protected override void UpdateAllUI()
    {
        SetCoinCount();
        UpdateInventory(0);
        UpdateTab(0);
    }

    public void UpdateInventoryCategory(int category)
    {
        ItemCatergory itemCatergory = (ItemCatergory)category;
        currentInventoryList = FilterPlayerInventoryByCategory(currentInventory, itemCatergory);
    }

    //Shop Functionality
    private void SelectEquipment(bool select, bool resetItemIndex = true)
    {
        purchaseMade = false;
        
        if(select && currentInventoryList.Count == 0)
        {
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            return;
        }

        Item selectedItem = null;

        if(currentInventoryList.Count > 0)
            selectedItem = currentInventoryList[itemIndex];

        //Check if Selected Equipment is Enchantable
        if (select && !selectedEquipment && !InventoryManager.Instance.CanEnchantEquipment(null, selectedItem))
        {
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            return; 
        }

        selectingEnchantment = select;

        selectEnchantmentTab.SetActive(select);
        selectEquipmentTab.SetActive(!select);

        if (select && !selectedEquipment)
        {
            //Play SFX
            AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

            currentTabTitle.text = "Enchantments";
            selectedEquipment = selectedItem;
            equipmentOwner = currentInventory;

            //Set Data
            selectedEquipmentAreaTitle.text = "Enchant " + selectedEquipment.itemName;
            equipmentIcon.sprite = selectedEquipment.UIIcon;
            equipmentIcon.color = InventoryManager.Instance.GetRarityColor(selectedEquipment.rarity);

            SetEnchanmentNameHeader(equipmentEnchantmentHeader);
        }
        else if (!select)
        {
            selectedEquipment = null;
            equipmentOwner = null;
        }

        defaultMessageArea.SetActive(!select);
        selectedEquipmentArea.Fade(select);

        UpdateInventory(0, resetItemIndex);
    }

    private void ActivateEnchantmentArea(bool activate, bool calledViaInput)
    {
        if(activate && currentInventoryList.Count == 0) { return; }

        //Check If Can Afford enchantment 
        Item selectedItem = currentInventoryList[itemIndex];
        float price = myShop.CalculateEnchantmentPrice(selectedItem as Enchantment);

        if (activate && (!InventoryManager.Instance.CanAfford(price, true) || !InventoryManager.Instance.CanEnchantEquipment(selectedItem as Enchantment, selectedEquipment))) 
        {
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            return;  
        }

        confirmingPurchase = activate;
        enchantmentArea.SetActive(activate);

        if(!activate && calledViaInput)
        {
            Debug.Log("Going Back to select Enchantment");
            SelectEquipment(true, false);
            return;
        }
        else if (!activate)
        {
            selectEnchantmentTab.SetActive(false);
            selectEquipmentTab.SetActive(true);
            selectedEquipment = null;
        }

        selectingEnchantment = false;
        selectedEquipmentArea.Fade(false);
        defaultMessageArea.SetActive(true);

        selectedEnchantment = activate ? selectedItem as Enchantment : null;

        if (!activate) { return; }

        //Play SFX
        AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

        enchantmentOwner = currentInventory;

        //Set Data
        selectedEnchantmentName.text = selectedEnchantment.itemName;
        selectedEnchantmentEffect.text = selectedEnchantment.description;
        selectedEnchantmentIcon.sprite = selectedEnchantment.UIIcon;
        selectedEnchantmentIcon.color = InventoryManager.Instance.GetRarityColor(selectedEnchantment.rarity);

        selectedEquipmentName.text = selectedEquipment.itemName;

        string separator = "   ";

        Weapon weapon = selectedEquipment as Weapon;
        Armour armour = selectedEquipment as Armour;

        selectedEquipmentSummary.text = weapon ? "Lvl: " + weapon.requiredLevel + separator + "Phy: " + weapon.basePhysAttack + separator + "Mag: " + weapon.baseMagAttack
            : "Lvl: " + armour.requiredLevel + separator + "Armour: " + armour.armour;

        SetEnchanmentNameHeader(selectedEquipmentEnchantmentHeader);

        selectedEquipmentIcon.sprite = selectedEquipment.UIIcon;
        selectedEquipmentIcon.color = InventoryManager.Instance.GetRarityColor(selectedEquipment.rarity);

        enchantPrice.text = price.ToString();

        UpdateMessageBasedOnSelectedItem();
    }

    private void Purchase()
    {
        float price = myShop.CalculateEnchantmentPrice(selectedEnchantment);

        if (InventoryManager.Instance.TrySpendCoin(price, true))
        {
            //Play SFX
            AudioManager.Instance.PlaySFX(SFXType.ShopCoin);

            purchaseMade = true;

            Weapon weapon = selectedEquipment as Weapon;
            Armour armour = selectedEquipment as Armour;

            if (weapon)
            {
                InventoryManager.Instance.TryEnchantWeapon(equipmentOwner, enchantmentOwner, weapon, selectedEnchantment);
            }
            else
            {
                InventoryManager.Instance.TryEnchantArmour(equipmentOwner, enchantmentOwner, armour, selectedEnchantment);
            }

            selectingEnchantment = false;

            ActivateEnchantmentArea(false, false);
            UpdateCoinCount();
            UpdateInventory(0);
        }
        else
        {
            //Play SFX
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
        }
    }


    private void UpdateShopUIByCategory()
    {
        equipmentTabHeader.GetChild(currentTabIndex).GetComponent<Button>().onClick.Invoke();
    }

    private void UpdateTab(int indexChange)
    {
        UpdateTabUI(indexChange, ref currentTabIndex, equipmentTabHeader, currentTabTitle, currentTabColor, defaultTabColor);

        UpdateShopUIByCategory();
        RebuildShopUI(true);
    }

    //UI
    private void RebuildShopUI(bool resetIndex)
    {
        if (resetIndex)
            itemIndex = 0;

        UpdateInventoryGridUI(currentInventory, currentInventoryList, inventoryItemPrefab, gridScrollRect, itemDetailHeader, noItemsArea, true);
        UpdateSelected(Vector2.zero);
    }

    private void UpdateSelected(Vector2 indexChange)
    {
        if(purchaseMade && indexChange != Vector2.zero) { purchaseMade = false; }

        if (currentInventoryList.Count == 0)
        {
            UpdateMessageBasedOnSelectedItem();
            return;
        }

        UpdateSelectedInventoryItemUI(indexChange, ref itemIndex, currentInventoryList, columnCount, gridScrollRect);

        Item selectedItem = currentInventoryList[itemIndex];
        UpdateItemDetail(itemDetailHeader, selectedItem, inventoryDetails);

        if (selectingEnchantment)
            selectedEnchantmentPrice.text = myShop.CalculateEnchantmentPrice(selectedItem as Enchantment).ToString();

        if (!confirmingPurchase)
            UpdateMessageBasedOnSelectedItem();
    }

    private void UpdateInventory(int indexChange, bool resetItemIndex = true)
    {
        currentInventory = UpdateInventoryOwner(indexChange, ref playerIndex, inventoryName);

        if (selectingEnchantment)
        {
            currentInventoryList = InventoryManager.Instance.GetItemInventory<Enchantment>(currentInventory).ToList<Item>();
        }
        else
        {
            UpdateShopUIByCategory();
        }
        

        RebuildShopUI(resetItemIndex);
    }

    private void UpdateMessageBasedOnSelectedItem()
    {
        Item selectedItem = currentInventoryList.Count > 0 ? currentInventoryList[itemIndex] : null;
        Transform messagesHeader = selectingEnchantment ? enchantmentMessagesHeader : defaultMessagesHeader;

        UpdateMessageBasedOnSelectedItem(selectedItem, messagesHeader, playerToCompareAgainst, confirmingPurchase, purchaseMade);
    }

    protected override void ResetData()
    {
        currentInventoryList.Clear();

        currentTabIndex = 0;
        itemIndex = 0;
        playerIndex = 0;

        selectingEnchantment = false;
        equipmentOwner = null;
        enchantmentOwner = null;

        confirmingPurchase = false;
        purchaseMade = false;

        selectedEnchantment = null;
        selectedEquipment = null;

        currentInventory = GetAllPlayersAtShop()[playerIndex];
        playerToCompareAgainst = currentInventory;

        selectEquipmentTab.SetActive(true);
        selectEnchantmentTab.SetActive(false);

        selectedEquipmentArea.Fade(false);
        defaultMessageArea.SetActive(true);
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
            else if(selectingEnchantment)
            {
                ActivateEnchantmentArea(true, true);
            }
            else
            {
                SelectEquipment(true);
            }
        }
    }

    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed && !confirmingPurchase)
        {
            if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
            {
                int indexChange = context.action.name == "ScrollD" ? 1 : -1;

                UpdateSelected(new Vector2(0, indexChange));
            }
        }
    }

    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.performed && !confirmingPurchase)
        {
            if (context.action.name == "CycleR" || context.action.name == "CycleL")
            {
                int indexChange = context.action.name == "CycleR" ? 1 : -1;

                UpdateSelected(new Vector2(indexChange, 0));
            }
        }
    }

    private void OnInventoryChange(InputAction.CallbackContext context)
    {
        if (context.performed && !confirmingPurchase)
        {
            if (context.action.name == "CompareR" || context.action.name == "CompareL")
            {
                int indexChange = context.action.name == "CompareR" ? 1 : -1;
                UpdateInventory(indexChange);
            }
        }
    }



    private void OnChangeTab(InputAction.CallbackContext context)
    {
        if (context.performed && !confirmingPurchase && !selectingEnchantment)
        {
            if (context.action.name == "TabR")
            {
                UpdateTab(1);
            }
            else if (context.action.name == "TabL")
            {
                UpdateTab(-1);
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
                //Play SFX
                AudioManager.Instance.PlaySFX(SFXType.TabBack);
                ActivateEnchantmentArea(false, true);
            }
            else if (selectingEnchantment)
            {
                //Play SFX
                AudioManager.Instance.PlaySFX(SFXType.TabBack);
                SelectEquipment(false);
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
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnInventoryChange;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnChangeTab;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnInventoryChange;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnChangeTab;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
        }
    }



}
