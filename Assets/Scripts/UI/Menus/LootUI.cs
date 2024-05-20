
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using AnotherRealm;

public class LootUI : MonoBehaviour, IControls
{
    [Header("Game Objects")]
    [SerializeField] GameObject lootUI;
    [SerializeField] GameObject lootItemPrefab;
    [Header("Loot Area")]
    [SerializeField] ScrollRect lootScrollRect;
    [SerializeField] GameObject emptyArea;
    [Header("Inventory")]
    [SerializeField] Image currentInventoryPotrait;
    [SerializeField] Image rightPotrait;
    [SerializeField] Image leftPortait;
    [Space(20)]
    [SerializeField] TextMeshProUGUI currentInventoryName;
    [SerializeField] TextMeshProUGUI currentWeight;
    [SerializeField] TextMeshProUGUI maxWeight;
    [Header("Colours")]
    [SerializeField] Color selectedItemColour;
    [SerializeField] Color defaultItemColor;
    [Space(10)]
    public Color overburdenedWeightColor;
    public Color defaultWeightColor;
    [Header("Indices")]
    [SerializeField] int selectedArrowIndex = 0;
    [SerializeField] int lootNameIndex = 1;
    [SerializeField] int lootIconIndex = 2;
    [SerializeField] int lootWeightIndex = 3;

    public static LootUI Instance { get; private set; }

    Lootable currentLoot = null;

    //Lists
    List<PlayerGridUnit> allPartyMembers = new List<PlayerGridUnit>();
    List<Item> itemList = new List<Item>();

    //Indices
    int selectedItemIndex = 0;
    int selectedInventoryIndex = 0;


    //Cache 
    PlayerInput playerInput;

    private void Awake()
    {
        Instance = this;

        playerInput = ControlsManager.Instance.GetPlayerInput();
    }

    private void OnEnable()
    {
        ControlsManager.Instance.SubscribeToPlayerInput("Loot", this);
    }

    private void Start()
    {
        allPartyMembers = PartyData.Instance.GetAllPlayerMembersInWorld();
    }

    public void BeginLoot(Lootable loot)
    {
        //Play SFX
        AudioManager.Instance.PlaySFX(loot.sfxOnBeginLoot);

        selectedItemIndex = 0;
        selectedInventoryIndex = 0;

        currentLoot = loot;
        itemList = loot.GetItems();

        ControlsManager.Instance.SwitchCurrentActionMap("Loot");
  
        CreateLootUI();
        ShowUI(true);

        UpdateItemListUI(0);
        UpdateInventoryUI(0);
    }

    private void ShowUI(bool show)
    {
        lootUI.SetActive(show);
    }

    private void Take()
    {
        if(itemList.Count == 0) { return; }

        //Play SFX
        AudioManager.Instance.PlaySFX(SFXType.Loot);

        currentLoot.TakeItem(GetSelectedInventory(), itemList[selectedItemIndex]);
        DeactiveLootUIItem(selectedItemIndex);
        
        UpdateItemListUI(0);
        UpdateInventoryWeight();
    }

    private void TakeAll()
    {
        if (itemList.Count == 0) { return; }

        //Play SFX
        AudioManager.Instance.PlaySFX(SFXType.Loot);

        for (int i = itemList.Count - 1; i >= 0; i--)
        {
            currentLoot.TakeItem(GetSelectedInventory(), itemList[i]);
            DeactiveLootUIItem(i);
        }

        UpdateItemListUI(0);
        UpdateInventoryWeight();
    }

    private void DeactiveLootUIItem(int index)
    {
        Transform child = lootScrollRect.content.GetChild(index);
        child.gameObject.SetActive(false);

        //Set Child as last
        child.SetAsLastSibling();
    }

    private void Exit()
    {
        ShowUI(false);
        ControlsManager.Instance.SwitchCurrentActionMap("Player");

        if(itemList.Count == 0)
        {
            currentLoot.OnLootEmpty();
        }
    }

    //UI
    private void CreateLootUI()
    {
        foreach (Transform child in lootScrollRect.content)
        {
            //Deactive Child
            child.gameObject.SetActive(false);
        }

        for (int index = 0; index < itemList.Count; index++)
        {
            Item item = itemList[index];

            GameObject ui;

            if (index < lootScrollRect.content.childCount)
            {
                ui = lootScrollRect.content.GetChild(index).gameObject;
            }
            else
            {
                ui = Instantiate(lootItemPrefab, lootScrollRect.content);
            }

            Image uiIcon = ui.transform.GetChild(lootIconIndex).GetComponent<Image>();
            uiIcon.sprite = item.UIIcon;
            uiIcon.color = InventoryManager.Instance.GetRarityColor(item.rarity);

            ui.transform.GetChild(lootNameIndex).GetComponent<TextMeshProUGUI>().text = item.itemName;
            ui.transform.GetChild(lootWeightIndex).GetComponent<TextMeshProUGUI>().text = item.weight.ToString();

            ui.gameObject.SetActive(true);
        }
    }

    private void UpdateInventoryUI(int indexChange)
    {
        CombatFunctions.UpdateListIndex(indexChange, selectedInventoryIndex, out selectedInventoryIndex, allPartyMembers.Count);

        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.InventorySwitch);

        currentInventoryName.text = GetSelectedInventory().unitName;
        currentInventoryPotrait.sprite = GetSelectedInventory().portrait;

        rightPotrait.sprite = GetNextPlayerInInventoryList(1).portrait;
        leftPortait.sprite = GetNextPlayerInInventoryList(-1).portrait;

        UpdateInventoryWeight();
    }

    private void UpdateItemListUI(int indexChange)
    {
        CombatFunctions.UpdateListIndex(indexChange, selectedItemIndex, out selectedItemIndex, itemList.Count);

        emptyArea.SetActive(itemList.Count == 0);
        Transform selectedChild = lootScrollRect.content.GetChild(0);

        //Play Audio
        if(indexChange != 0 && itemList.Count > 0)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        foreach (Transform child in lootScrollRect.content)
        {
            if (!child.gameObject.activeSelf) { continue; }

            bool selected = child.GetSiblingIndex() == selectedItemIndex;
            child.GetChild(selectedArrowIndex).gameObject.SetActive(selected);

            Image childBG = child.GetComponent<Image>();

            if (selected)
            {
                selectedChild = child;
                childBG.color = selectedItemColour;
            }
            else
            {
                childBG.color = defaultItemColor;
            }
        }

        //Scroll To Selected
        CombatFunctions.VerticalScrollHighlightedInView(selectedChild as RectTransform, lootScrollRect, selectedItemIndex, itemList.Count);
    }

    private void UpdateInventoryWeight()
    {
        float weight = InventoryManager.Instance.GetCurrentWeight(GetSelectedInventory());
        float capacity = InventoryManager.Instance.GetWeightCapacity(GetSelectedInventory());

        currentWeight.text = weight.ToString();
        maxWeight.text = "/" + capacity.ToString();

        currentWeight.color = weight > capacity ? overburdenedWeightColor : defaultWeightColor;
    }

    //Helper Methods

    private PlayerGridUnit GetNextPlayerInInventoryList(int indexChange)
    {
        int newIndex;

        if (selectedInventoryIndex + indexChange >= allPartyMembers.Count)
        {
            newIndex = 0;
        }
        else if (selectedInventoryIndex + indexChange < 0)
        {
            newIndex = allPartyMembers.Count - 1;
        }
        else
        {
            newIndex = selectedInventoryIndex + indexChange;
        }

        return allPartyMembers[newIndex];
    }


    private PlayerGridUnit GetSelectedInventory()
    {
        return allPartyMembers[selectedInventoryIndex];
    }


    //Inputs
    private void OnTake(InputAction.CallbackContext context)
    {
        if (context.action.name != "Take") { return; }

        if (context.performed)
        {
            Take();
        }
    }

    private void OnTakeAll(InputAction.CallbackContext context)
    {
        if (context.action.name != "TakeAll") { return; }

        if (context.performed)
        {
            TakeAll();
        }
    }
    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "CycleR")
            {
                UpdateInventoryUI(1);
            }
            else if (context.action.name == "CycleL")
            {
                UpdateInventoryUI(-1);
            }
        }
    }

    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU")
            {
                UpdateItemListUI(-1);
            }
            else if (context.action.name == "ScrollD")
            {
                UpdateItemListUI(1);
            }
        }
    }

    private void OnExit(InputAction.CallbackContext context)
    {
        if (context.action.name != "Exit") { return; }

        if (context.performed)
        {
            Exit();
        }
    }

    



    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            playerInput.onActionTriggered += OnTake;
            playerInput.onActionTriggered += OnTakeAll;
            playerInput.onActionTriggered += OnExit;
            playerInput.onActionTriggered += OnScroll;
            playerInput.onActionTriggered += OnCycle;
        }
        else
        {
            playerInput.onActionTriggered -= OnTake;
            playerInput.onActionTriggered -= OnTakeAll;
            playerInput.onActionTriggered -= OnExit;
            playerInput.onActionTriggered -= OnScroll;
            playerInput.onActionTriggered -= OnCycle;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }

}
