using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Linq;
using AnotherRealm;

public class ShopDisenchantUI : BaseShopUI
{
    [Header("Disenchant UI")]
    [SerializeField] int columnCount = 5;
    [Header("Headers")]
    [SerializeField] ScrollRect gridScrollRect;
    [SerializeField] Transform equipmentTabHeader;
    [Space(5)]
    [SerializeField] Transform messagesHeader;
    [Header("Titles")]
    [SerializeField] TextMeshProUGUI currentTabTitle;
    [Header("Inventory Area")]
    [SerializeField] TextMeshProUGUI inventoryName;
    [SerializeField] FadeUI inventoryArea;
    [Header("No Item")]
    [SerializeField] GameObject noItemsArea;
    [Header("DisenchantmentArea")]
    [SerializeField] GameObject disenchantmentArea;
    [SerializeField] Transform enchantmentHeader;
    [Space(5)]
    [SerializeField] TextMeshProUGUI disenchantAreaTitle;
    [SerializeField] Image selectedEquipmentIcon;
    [Space(5)]
    [SerializeField] TextMeshProUGUI disenchantPrice;
    [Header("Colors")]
    [SerializeField] Color currentTabColor;
    [SerializeField] Color defaultTabColor;
    [Space(10)]
    [SerializeField] Color currentEnchantmentColor;
    [SerializeField] Color defaultEnchantmentColor = Color.white;
    [Header("Prefabs")]
    [SerializeField] GameObject inventoryItemPrefab;
    [Header("Item detail")]
    [SerializeField] GameObject itemDetailHeader;
    [Space(5)]
    [SerializeField] List<InventoryDetailUI> inventoryDetails;


    //Player Data
    PlayerGridUnit currentInventory = null;

    PlayerGridUnit equipmentOwner = null;

    //Indices
    int playerIndex = 0;
    int currentTabIndex = 0;
    int equipmentIndex = 0;
    int enchantmentIndex = 0;

    //Bools
    bool confirmingPurchase = false;
    bool purchaseMade = false;

    List<Item> currentInventoryList = new List<Item>();
    List<Enchantment> currentEnchantmentList = new List<Enchantment>();

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
    private void SelectEquipment(bool select)
    {
        if (select)
        {
            purchaseMade = false;

            if (currentInventoryList.Count == 0)
            {
                AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
                return;
            }

            //Check if Selected Equipment is Disenchantable
            Item selectedItem = currentInventoryList[equipmentIndex];
            if (!InventoryManager.Instance.CanDisenchantEquipment(selectedItem)) 
            {
                AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
                return; 
            }

            AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

            confirmingPurchase = true;
            selectedEquipment = selectedItem;

            enchantmentIndex = 0;
            equipmentOwner = currentInventory;

            Weapon weapon = selectedEquipment as Weapon;
            Armour armour = selectedEquipment as Armour;

            if (weapon)
            {
                currentEnchantmentList = weapon.infusedEnchantments.Concat(weapon.embeddedEnchantments).ToList();
            }
            else
            {
                currentEnchantmentList = armour.infusedEnchantments.Concat(armour.embeddedEnchantments).ToList();
            }

            //Set Title
            disenchantAreaTitle.text = "Disenchant " + selectedEquipment.itemName;

            //Set Equipment
            selectedEquipmentIcon.sprite = selectedEquipment.UIIcon;
            selectedEquipmentIcon.color = InventoryManager.Instance.GetRarityColor(selectedEquipment.rarity);

            //Set Enchantment
            SetEnchantmentUI();
        }
        else
        {
            confirmingPurchase = false;
            equipmentOwner = null;
            UpdateInventory(0, false);
        }

        //Set Areas
        disenchantmentArea.SetActive(select);
        inventoryArea.Fade(!select); 
    }

    private void Purchase()
    {
        purchaseMade = false;
        Enchantment selectedEnchantment = currentEnchantmentList[enchantmentIndex];
        
        //Check if not infused 
        if(selectedEnchantment is InfusedEnchantment) 
        {
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            return; 
        }
        float price = myShop.CalculateDisenchantmentPrice(selectedEquipment);

        if (InventoryManager.Instance.TrySpendCoin(price, myShop.isFantasyShop))
        {
            AudioManager.Instance.PlaySFX(SFXType.ShopCoin);

            purchaseMade = true;

            int newEnchantmentCount = currentEnchantmentList.Where((item) => !(item is InfusedEnchantment)).Count() - 1;

            InventoryManager.Instance.TryDisenchantEquipment(equipmentOwner, selectedEquipment, selectedEnchantment);
            UpdateCoinCount();

            enchantmentIndex = 0;

            if (newEnchantmentCount == 0)
            {
                SelectEquipment(false);
            }
            else
            {
                SetEnchantmentUI();
            }
        }
        else
        {
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
        }
    }

    private void SetEnchantmentUI()
    {
        SetEnchanmentNameHeader(enchantmentHeader);
        UpdateSelectedEnchantment(0);
        UpdateMessageBasedOnSelectedItem();
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
            equipmentIndex = 0;

        UpdateInventoryGridUI(currentInventory, currentInventoryList, inventoryItemPrefab, gridScrollRect, itemDetailHeader, noItemsArea, false);
        UpdateSelected(Vector2.zero);
    }

    private void UpdateSelected(Vector2 indexChange)
    {
        if (purchaseMade && indexChange != Vector2.zero) { purchaseMade = false; }

        if (currentInventoryList.Count == 0)
        {
            UpdateMessageBasedOnSelectedItem();
            return;
        }

        UpdateSelectedInventoryItemUI(indexChange, ref equipmentIndex, currentInventoryList, columnCount, gridScrollRect);

        Item selectedItem = currentInventoryList[equipmentIndex];
        UpdateItemDetail(itemDetailHeader, selectedItem, inventoryDetails);

        UpdateMessageBasedOnSelectedItem();
    }

    private void UpdateSelectedEnchantment(int indexChange)
    {
        if (purchaseMade && indexChange > 0) { purchaseMade = false; }

        if(indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        CombatFunctions.UpdateListIndex(indexChange, enchantmentIndex, out enchantmentIndex, currentEnchantmentList.Count);

        UpdateEnchantmentHeader(enchantmentHeader, enchantmentIndex, currentEnchantmentColor, defaultEnchantmentColor);

        Item selectedItem = currentEnchantmentList[enchantmentIndex];

        //Set Disenchanmtent Price
        disenchantPrice.text = myShop.CalculateDisenchantmentPrice(selectedEquipment).ToString();

        UpdateItemDetail(itemDetailHeader, selectedItem, inventoryDetails);
        UpdateMessageBasedOnSelectedItem();
    }

    private void UpdateInventory(int indexChange, bool resetItemIndex = true)
    {
        currentInventory = UpdateInventoryOwner(indexChange, ref playerIndex, inventoryName);
        UpdateShopUIByCategory();
        RebuildShopUI(resetItemIndex);
    }

    private void UpdateMessageBasedOnSelectedItem()
    {
        Item selectedItem = currentInventoryList.Count > 0 ? currentInventoryList[equipmentIndex] : null;
        UpdateMessageBasedOnSelectedItem(selectedItem, messagesHeader, playerToCompareAgainst, confirmingPurchase, purchaseMade);
    }

    protected override void ResetData()
    {
        currentInventoryList.Clear();
        currentEnchantmentList.Clear();

        currentTabIndex = 0;
        equipmentIndex = 0;
        playerIndex = 0;

        equipmentOwner = null;

        confirmingPurchase = false;
        purchaseMade = false;

        selectedEquipment = null;

        currentInventory = GetAllPlayersAtShop()[playerIndex];
        playerToCompareAgainst = currentInventory;
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
                SelectEquipment(true);
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
                    UpdateSelectedEnchantment(indexChange);
                }
                else
                {
                    UpdateSelected(new Vector2(0, indexChange));
                }
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
        if (context.performed && !confirmingPurchase)
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
