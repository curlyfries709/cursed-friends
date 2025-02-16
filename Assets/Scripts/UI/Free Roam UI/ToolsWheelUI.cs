using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

public class ToolsWheelUI : BaseUI, IControls, IInventorySelector
{
    [Header("Components")]
    [SerializeField] Transform cursor;
    [Header("Tools")]
    [SerializeField] Transform toolsHeader;
    [Space(10)]
    [SerializeField] int toolIconSiblingIndex = 0;
    [SerializeField] int toolCountSiblingIndex = 1;
    [Header("Description Area")]
    [SerializeField] TextMeshProUGUI toolTitle;
    [SerializeField] TextMeshProUGUI toolDescription;
    [SerializeField] TextMeshProUGUI toolAction;
    [Header("Colors")]
    [SerializeField] Color selectedBGColor;
    [SerializeField] Color defaultBGColor;

    int selectedToolIndex = 0;
    bool subscribedToEvent = false;

    Tool prevAssignedTool = null;
    TriggerComponentUI currentHoveredComponent = null;

    //Controls
    Vector2 inputMoveValue = Vector2.zero;
    const string myActionMap = "Wheel";

    protected override void Awake()
    {
        base.Awake();
        ControlsManager.Instance.SubscribeToPlayerInput(myActionMap, this);
    }

    private void OnEnable()
    {
        if (FantasyCombatManager.Instance && !subscribedToEvent)
        {
            subscribedToEvent = true;
            FantasyCombatManager.Instance.BattleTriggered += OnBattleTriggered;
        }

        ControlsManager.Instance.SwitchCurrentActionMap(this);
        HUDManager.Instance.HideHUDs();

        BuildUI();
    }
    
    private void BuildUI()
    {
        foreach (Transform toolUI in toolsHeader)
        {
            int index = toolUI.GetSiblingIndex();
            Tool toolData = GetToolAtIndex(index);
            
            Transform toolContentHeader = toolUI.GetChild(0);

            //Show the tool content if tool is valid
            toolContentHeader.gameObject.SetActive(toolData);

            if (!toolData) continue;

            //Set Icon
            toolContentHeader.GetChild(toolIconSiblingIndex).GetComponent<Image>().sprite = toolData.UIIcon;
            //Set Count
            toolContentHeader.GetChild(toolCountSiblingIndex).GetComponent<TextMeshProUGUI>().text = InventoryManager.Instance.GetItemCount(toolData, PartyManager.Instance.GetLeader()).ToString();
        }

        UpdateSelectedUI();
    }

    private void UpdateSelectedUI()
    {
        Tool tool = GetHoveredTool();

        //Update Selected Tool Details
        toolTitle.text = tool ? tool.itemName : "";
        toolDescription.text = tool ? tool.description : "";

        toolAction.text = GetSelectedToolAction(tool);

        foreach (Transform toolUI in toolsHeader)
        {
            int index = toolUI.GetSiblingIndex();
            bool isSelected = currentHoveredComponent && index == currentHoveredComponent.transform.GetSiblingIndex();
            toolUI.GetComponent<Image>().color = isSelected ? selectedBGColor : defaultBGColor;
        }
    }

    private void OnBattleTriggered(BattleStarter.CombatAdvantage combatAdvantage)
    {
        //Exit if Battle happens to trigger while active.
        InventoryManager.Instance.ActivateInventoryUI(false);
        RoamToolsManager.Instance.ActivateToolsWheelUI(false);
    }

    public override void OnCursorTriggerEnter(TriggerComponentUI enteredComponent)
    {
        base.OnCursorTriggerEnter(enteredComponent);
        currentHoveredComponent = enteredComponent;
        UpdateSelectedUI();
    }

    public override void OnCursorTriggerExit(TriggerComponentUI exitedComponent)
    {
        base.OnCursorTriggerExit(exitedComponent);

        if(currentHoveredComponent == exitedComponent)
        {
            currentHoveredComponent = null;
            UpdateSelectedUI();
        }
    }

    //INVENTORY SELECTOR
    void IInventorySelector.OnItemSelected(Item selectedItem)
    {
        //Deactivate passive tool
        if (prevAssignedTool)
            RoamToolsManager.Instance.ActivatePassiveTool(prevAssignedTool, false);

        //Assign New Tool.
        Tool tool = selectedItem as Tool;
        InventoryManager.Instance.AssignToolToSlot(tool, selectedToolIndex);

        //Activate if passive
        RoamToolsManager.Instance.ActivatePassiveTool(tool, true);

        //Update UI
        EndToolAssignment();
        BuildUI();
    }

    bool IInventorySelector.CanSelectItem(Item item)
    {
        //It's a tool and not already assigned 
        Tool tool = item as Tool;
        return tool && !InventoryManager.Instance.IsToolAlreadyAssigned(tool);
    }

    bool IInventorySelector.CanSwitchToAnotherInventory()
    {
        return false;
    }

    bool IInventorySelector.CanSwitchInventoryCategory()
    {
        return false;
    }

    void IInventorySelector.OnCancel()
    {
        EndToolAssignment();
    }

    private void OnDisable()
    {
        if (FantasyCombatManager.Instance && subscribedToEvent)
        {
            subscribedToEvent = false;
            FantasyCombatManager.Instance.BattleTriggered -= OnBattleTriggered;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }

    private void EndToolAssignment()
    {
        //Update UI
        InventoryManager.Instance.ActivateInventoryUI(false);
        RoamToolsManager.Instance.ActivateToolsWheelUI(true);
    }

    //GETTERS
    private Tool GetHoveredTool()
    {
        if(!currentHoveredComponent) { return null; }

        int index = currentHoveredComponent.transform.GetSiblingIndex();
        return GetToolAtIndex(index);
    }

    private Tool GetToolAtIndex(int index)
    {
        return InventoryManager.Instance.GetAssignedToolAtSlot(index);
    }

    private int GetIndexOfHoveredTool()
    {
        if (!currentHoveredComponent) { return -1; }
        return currentHoveredComponent.transform.GetSiblingIndex();
    }

    private string GetSelectedToolAction(Tool tool)
    {
        if (!tool)
            return "Assign tool to slot";

        if (tool.isPassive)
        {
            Debug.Log("CHECK IF PASSIVE TOOL IS ENABLE OR DISABLED");
            return "Enable";
        }
        else
        {
            return "Use";
        }
    }
    //INPUT
    private void OnSelect(InputAction.CallbackContext context)
    {
        Tool selectedTool = GetHoveredTool();

        if (!selectedTool) { return; }

        RoamToolsManager.Instance.SetActiveTool(selectedTool);

        //If still active, update the UI. This means a passive tool was likely updated.
        if (gameObject.activeInHierarchy)
        {
            UpdateSelectedUI();
        }
    }

    private void OnAssign(InputAction.CallbackContext context)
    {
        if (!currentHoveredComponent) { return; }

        selectedToolIndex = GetIndexOfHoveredTool();
        prevAssignedTool = GetHoveredTool();

        gameObject.SetActive(false);

        //Freeze Game
        GameManager.Instance.FreezeGame();

        //Begin Tool Assign       
        InventoryManager.Instance.ActivateInventoryUIInSelectionMode(this, ItemCatergory.Tools, PartyManager.Instance.GetLeader());
    }

    private void OnHover(InputAction.CallbackContext context)
    {
        inputMoveValue = context.ReadValue<Vector2>();
        MoveCursor(cursor, inputMoveValue);
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        AudioManager.Instance.PlaySFX(SFXType.TabBack);
        RoamToolsManager.Instance.ActivateToolsWheelUI(false);
    }
    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            playerInput.actions.FindAction("Hover").performed += OnHover;
            playerInput.actions.FindAction("Hover").canceled += OnHover;

            playerInput.actions.FindAction("TabR").performed += OnAssign;

            playerInput.actions.FindAction("Select").performed += OnSelect;
            playerInput.actions.FindAction("Exit").performed += OnCancel;
        }
        else
        {
            playerInput.actions.FindAction("Hover").performed -= OnHover;
            playerInput.actions.FindAction("Hover").canceled -= OnHover;

            playerInput.actions.FindAction("TabR").performed -= OnAssign;

            playerInput.actions.FindAction("Select").performed -= OnSelect;
            playerInput.actions.FindAction("Exit").performed -= OnCancel;
        }
    }


}
