using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnotherRealm;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;

public class SettingsUI : MonoBehaviour, IControls
{
    [Header("Headers")]
    [SerializeField] Transform tabsHeader;
    [SerializeField] Transform settingAreaHeader;
    [Header("Colors")]
    [SerializeField] Color tabSelectedColor;
    [SerializeField] Color tabDefaultColor = Color.white;
    [Header("Controls")]
    [SerializeField] List<Transform> controlHeaders = new List<Transform>();
    [Header("Indices")]
    [SerializeField] int tabSelectedIndex = 0;
    [SerializeField] int settingItemSelectedIndex = 0;

    Transform currentSettingArea;
    int currentTabIndex = 0;
    int currentSettingIndex = 0;

    const string myActionMap = "GridMenu";

    public static Action SaveSettings;
    public static Action GameSettingsUpdated;

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
        //Change Controls
        ControlsManager.Instance.SwitchCurrentActionMap(this);

        currentTabIndex = 0;
        currentSettingIndex = 0;
        currentSettingArea = settingAreaHeader.GetChild(0);

        foreach(Transform child in settingAreaHeader)
        {
            child.gameObject.SetActive(false);
        }

        UpdateActiveTab(0);
    }

    private void UpdateActiveTab(int indexChange)
    {
        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.TabForward);

        currentSettingIndex = 0;
        CombatFunctions.UpdateListIndex(indexChange, currentTabIndex, out currentTabIndex, tabsHeader.childCount);

        //Update Visual
        foreach (Transform tab in tabsHeader)
        {
            bool isSelected = tab.GetSiblingIndex() == currentTabIndex;

            tab.GetComponent<TextMeshProUGUI>().color = isSelected ? tabSelectedColor : tabDefaultColor;
            tab.GetChild(tabSelectedIndex).gameObject.SetActive(isSelected);
        }

        //Activate connected Area
        tabsHeader.GetChild(currentTabIndex).GetComponent<Button>().onClick?.Invoke();

        UpdateActiveSetting(0);
    }

    private void UpdateActiveSetting(int indexChange)
    {
        if(indexChange != 0 && currentSettingArea.childCount > 1)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        CombatFunctions.UpdateListIndex(indexChange, currentSettingIndex, out currentSettingIndex, currentSettingArea.childCount);

        foreach (Transform child in currentSettingArea)
        {
            bool isSelected = child.GetSiblingIndex() == currentSettingIndex;
            child.GetChild(settingItemSelectedIndex).gameObject.SetActive(isSelected);
        }
    }

    public void ActivateSettingArea(GameObject settingArea)
    {
        //Reset Index
        currentSettingIndex = 0;
        //Deactivate current Area
        currentSettingArea.gameObject.SetActive(false);
        //Activate new Area.
        currentSettingArea = settingArea.transform;
        settingArea.gameObject.SetActive(true);
    }

    private void Exit()
    {
        AudioManager.Instance.PlaySFX(SFXType.TabBack);

        SaveSettings?.Invoke();
        GameSettingsUpdated?.Invoke();
        gameObject.SetActive(false);
        GameManager.Instance.ReturnToActiveMenu();
    }

    //Inputs
    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
            {
                int indexChange = context.action.name == "ScrollD" ? 1 : -1;
                UpdateActiveSetting(indexChange);
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
                if (currentSettingArea.GetChild(currentSettingIndex).GetComponent<GameSetting>().OnCycle(indexChange))
                {
                    AudioManager.Instance.PlaySFX(SFXType.TabForward);
                }
            }
        }
    }


    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            if (currentSettingArea.GetChild(currentSettingIndex).GetComponent<GameSetting>().OnSelect())
            {
                AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);
            }
        }
    }

    private void OnChangeTab(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "TabR")
            {
                UpdateActiveTab(1);
            }
            else if (context.action.name == "TabL")
            {
                UpdateActiveTab(-1);
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
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnChangeTab;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnExit;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnExit;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnChangeTab;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }
}
