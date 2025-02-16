using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoamToolsManager : MonoBehaviour, ISaveable
{
    public static RoamToolsManager Instance { get; private set; }

    [SerializeField] float wheelTimeScale = 0.3f;
    [Header("Pools")]
    [SerializeField] Transform toolsPoolHeader;
    [SerializeField] Transform spawnablePoolHeader;
    [Header("Visual")]
    [SerializeField] ToolsWheelUI toolsWheelUI;
    [Space(10)]
    [SerializeField] AimTrajectoryUI aimTrajectoryUI;

    bool toolsEnabled = true;
    BaseTool activeTool = null;

    //Dict
    Dictionary<Tool, GameObject> spawnedToolsDict = new Dictionary<Tool, GameObject>();
    Dictionary<Tool, Transform> spawnablesPoolDict = new Dictionary<Tool, Transform>();

    //Saving
    bool isDataRestored = false;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    private void OnEnable()
    {
        SavingLoadingManager.Instance.NewRealmEntered += OnNewRealmEntered;
    }

    private void OnNewRealmEntered(RealmType newRealm)
    {
        toolsEnabled = newRealm == RealmType.Fantasy ? true : false;
    }


    public void UseActiveTool()
    {
        if (!activeTool || !toolsEnabled) return;

        activeTool.Use();
    }
    
    public void OnToolUseComplete(Tool tool)
    {
        if (tool.hasInfiniteUses) return;

        //Remove From Inventory. 
        InventoryManager.Instance.RemoveFromInventory(PartyManager.Instance.GetLeader(), tool);

        if (!activeTool || activeTool.GetData() != tool) { return; }

        if (!InventoryManager.Instance.IsToolAlreadyAssigned(tool)) //Means Tool Count is now 0
        {
            activeTool = null;
        }

        UpdateUI(activeTool ? activeTool.GetData() : null);
    }


    public void SetActiveTool(Tool tool)
    {
        //Simply deactivate if the same.
        if(activeTool && activeTool.GetData() == tool)
        {
            ActivateToolsWheelUI(false);
            return;
        }

        GameObject toolObject = GetToolObject(tool);
        BaseTool toolComp = toolObject.GetComponent<BaseTool>();

        if (tool.isPassive)
        {
            //Toggle it.
            toolComp.ToggleState();
            return;
        }

        if (activeTool)
            activeTool.CancelUse();

        activeTool = toolComp;
        //Deactivate wheel
        ActivateToolsWheelUI(false);

        //Update UI
        UpdateUI(tool);
    }

    public void ActivatePassiveTool(Tool tool, bool activate)
    {
        if (!tool.isPassive) { return; }

        GameObject toolObject = GetToolObject(tool);
        BaseTool toolComp = toolObject.GetComponent<BaseTool>();

        if (activate)
        {
            toolComp.Activate();
        }
        else
        {
           toolComp.Deactivate(); 
        } 
    }

    public void EnableAimTrajectoryUI(bool enable, ThrowableTool throwable)
    {
        if (enable)
        {
            aimTrajectoryUI.Begin(throwable);
        }
        else
        {
            aimTrajectoryUI.End();
        }
    }


    private GameObject GetToolObject(Tool tool)
    {
        if (spawnedToolsDict.ContainsKey(tool))
        {
            return spawnedToolsDict[tool];
        }

        //Spawn if doesn't exist.
        GameObject spawnedTool = Instantiate(tool.toolFunctionalityPrefab, toolsPoolHeader);
        spawnedTool.SetActive(false);

        spawnedToolsDict[tool] = spawnedTool;

        return spawnedTool;
    }

    public bool IsPassiveToolActivated(Tool tool)
    {
        if (!spawnedToolsDict.ContainsKey(tool))
        {
            return false;
        }

        GameObject toolObject = GetToolObject(tool);
        BaseTool toolComp = toolObject.GetComponent<BaseTool>();
        return toolComp.IsActivated();
    }

    public void EnableToolsUsage(bool enable)
    {
        toolsEnabled = enable;

        //Update UI
        if (enable)
        {
            UpdateUI(activeTool ? activeTool.GetData() : null);
        }
        else
        {
            UpdateUI(null);
        }
    }

    public void ActivateToolsWheelUI(bool active)
    {
        if(!toolsEnabled) return;

        toolsWheelUI.Activate(active);

        MMTimeManager.Instance.SetTimeScaleTo(active ? wheelTimeScale : 1);

        if (!active)
        {
            HUDManager.Instance.ShowActiveHud();
            ControlsManager.Instance.SwitchCurrentActionMap("Player");
        }
    }

    public Transform GetPoolForSpawnables(Tool tool)
    {
        if (spawnablesPoolDict.ContainsKey(tool))
        {
            return spawnablesPoolDict[tool];
        }

        GameObject poolHeaderObj = Instantiate(new GameObject(tool.itemName + " Pool Header"), spawnablePoolHeader);
        spawnablesPoolDict[tool] = poolHeaderObj.transform;

        return poolHeaderObj.transform;
    }

    private void UpdateUI(Tool tool)
    {
        FantasyRoamHUD fantasyRoamHud = HUDManager.Instance.GetActiveHUD() as FantasyRoamHUD;

        if (!fantasyRoamHud) { return; }

        int count = 0;

        if (tool)
            count = InventoryManager.Instance.GetItemCount(tool, PartyManager.Instance.GetLeader());

        fantasyRoamHud.UpdateToolArea(tool, count);
    }

    private void OnDisable()
    {
        SavingLoadingManager.Instance.NewRealmEntered -= OnNewRealmEntered;
    }

    public object CaptureState()
    {
        //Store the active tool. 
        //Store enabled/disabled state for all assigned passive tools.
        Debug.Log("SETUP CAPTURE STATE FOR ROAM TOOLS MANAGER");

        return null;
    }

    public void RestoreState(object state)
    {
        isDataRestored = true;

        Debug.Log("SETUP RESTORE STATE FOR ROAM TOOLS MANAGER");
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
