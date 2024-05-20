using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnotherRealm;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Linq;

public class WeaponSwitchUI : MonoBehaviour, IControls
{
    [Header("Values")]
    [SerializeField] int columnCount = 5;
    [Header("Headers")]
    [SerializeField] ScrollRect gridScrollRect;
    [SerializeField] Transform itemDetailHeader;
    [Header("Areas")]
    [SerializeField] GameObject noItemsArea;
    [Header("Selected Menu")]
    [SerializeField] GameObject selectedMenu;
    [Header("Prefabs")]
    [SerializeField] GameObject inventoryItemPrefab;
    [Header("Controls")]
    [SerializeField] List<Transform> controlHeaders;
    [Header("Component UI")]
    [SerializeField] List<InventoryDetailUI> inventoryDetails;

    const string myActionMap = "Inventory";

    //Lists
    List<Item> currentInventory = new List<Item>();

    //Indices
    PlayerGridUnit inventoryOwner = null;
    FantasyCombatCollectionManager collectionManager;

    int currentGridIndex = 0;

    private void Awake()
    {
        foreach (Transform controlHeader in controlHeaders)
        {
            ControlsManager.Instance.AddControlHeader(controlHeader);
        }

        ControlsManager.Instance.SubscribeToPlayerInput(myActionMap, this);
    }


    public void ActivateUI(PlayerGridUnit player, FantasyCombatCollectionManager collectionManager)
    {
        gameObject.SetActive(true);
        this.collectionManager = collectionManager;

        inventoryOwner = player;
        currentGridIndex = 0;

        List<Item> allWeapons = InventoryManager.Instance.GetItemListFromCategory(inventoryOwner, ItemCatergory.Weapons, true);
        currentInventory = allWeapons.Where((weapon) => player.stats.CanEquip(weapon as Weapon)).ToList();

        ControlsManager.Instance.SwitchCurrentActionMap(this);
        UpdateAllUI();
    }

    private void Equip()
    {
        //Default 0 //Equip 1 //Use 2
        if (!selectedMenu.activeInHierarchy || noItemsArea.activeInHierarchy) { return; }

        AudioManager.Instance.PlaySFX(SFXType.EquipItem);

        Weapon selectedItem = currentInventory[currentGridIndex] as Weapon;

        inventoryOwner.stats.EquipWeapon(selectedItem);

        //Exit;
        collectionManager.OnExitWeaponSwitch(true); 
    }

    //UI
    private void UpdateAllUI()
    {
        RebuildInventoryUI(true);
        UpdateSelectedItemUI(Vector2.zero);
    }


    private void RebuildInventoryUI(bool resetIndex)
    {
        if (currentInventory.Count == 0)
        {
            gridScrollRect.content.gameObject.SetActive(false);
            itemDetailHeader.gameObject.SetActive(false);
            noItemsArea.SetActive(true);
            UpdateSelectedItemMenu(false);
            return;
        }

        if (resetIndex)
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
        if (currentInventory.Count == 0){ return; }

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
        selectedMenu.SetActive(show);

        if (!show) { return; }

        Item selectedItem = currentInventory[currentGridIndex];
        Transform itemTransform = gridScrollRect.content.GetChild(currentGridIndex);

        //Move to pos
        RectTransform targetTransform = itemTransform.GetComponent<InventoryItemUI>().GetMenuTransform();
        selectedMenu.transform.position = targetTransform.position;

        Weapon weapon = selectedItem as Weapon;

        selectedMenu.SetActive(!inventoryOwner.stats.IsEquipped(weapon));
    }

    private void UpdateItemDetail()
    {
        itemDetailHeader.gameObject.SetActive(true);
        Item selectedItem = currentInventory[currentGridIndex];

        foreach (InventoryDetailUI detail in inventoryDetails)
        {
            detail.SetData(selectedItem, inventoryOwner);
        }
    }


    //INput
    private void OnEquip(InputAction.CallbackContext context)
    {
        if (context.action.name != "Use") { return; }

        if (context.performed)
        {
            Equip();
        }
    }



    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
            {
                int indexChange = context.action.name == "ScrollD" ? 1 : -1;
                UpdateSelectedItemUI(new Vector2(0, indexChange));
            }
        }
    }

    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.performed && inventoryOwner)
        {
            if (context.action.name == "CycleR")
            {
                UpdateSelectedItemUI(new Vector2(1, 0));

            }
            else if (context.action.name == "CycleL")
            {
                UpdateSelectedItemUI(new Vector2(-1, 0));
            }
        }
    }


    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed)
        {
            AudioManager.Instance.PlaySFX(SFXType.TabBack);
            collectionManager.OnExitWeaponSwitch(false);
        }
    }



    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnEquip;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnEquip;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }

}
