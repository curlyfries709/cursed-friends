
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using AnotherRealm;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class MasteryUI : MonoBehaviour , IControls
{
    [Header("Attributes")]
    [SerializeField] ScrollRect masteryScrollRect;
    [Header("Values")]
    [SerializeField] float defaultPotraitSize = 70;
    [SerializeField] float selectedPotraitSize = 100;
    [Space(5)]
    [SerializeField] float potraitAnimTime = 0.25f;
    [SerializeField] float autoScrollTime = 0.25f;
    [Header("Selected Area")]
    [SerializeField] TextMeshProUGUI selectedName;
    [Space(5)]
    [SerializeField] List<Image> potraits;
    [Header("Prefab")]
    [SerializeField] GameObject progressionPrefab;
    [Header("Badges")]
    [SerializeField] Color defaultBadgeTitle = Color.white;
    [SerializeField] Color selectedBadgeTitle;

    Dictionary<string, int> progressionLength = new Dictionary<string, int>();

    int currentSeletedUnit = 0;
    int currentMasteryIndex = 0;
    int currentProgressionIndex = 0;

    int currentMasteryProgressionLength = 0;

    ScrollRect activeProgressionScrollRect;

    private void Awake()
    {
        ControlsManager.Instance.SubscribeToPlayerInput("GridMenu", this);
    }

    public void ActivateMasteryUI(bool activate)
    {
        if (activate)
        {
            ResetData();

            BuildPotraits();
            UpdateSelectedUnit(0);
        }

        HUDManager.Instance.ActivateUIPhotoshoot(true);
        PhoneMenu.Instance.OpenApp(activate);
        gameObject.SetActive(activate);

        if (activate)
            ControlsManager.Instance.SwitchCurrentActionMap(this);
    }

    private void ResetData()
    {
        activeProgressionScrollRect = null;
        currentSeletedUnit = 0;
        currentMasteryIndex = 0;
        currentProgressionIndex = 0;
        currentMasteryProgressionLength = 0;
    }

    private void BuildPotraits()
    {
        foreach (Image potrait in potraits)
        {
            int index = potrait.transform.GetSiblingIndex();
            potrait.gameObject.SetActive(index < PartyManager.Instance.GetAllPlayerMembersInWorld().Count);

            if (!potrait.gameObject.activeSelf) { continue; }

            PlayerGridUnit selectedPlayer = PartyManager.Instance.GetAllPlayerMembersInWorld()[index];
            potrait.sprite = selectedPlayer.portrait;
        }
    }


    private void BuildUI()
    {
        PlayerGridUnit selectedPlayer = PartyManager.Instance.GetAllPlayerMembersInWorld()[currentSeletedUnit];
        selectedName.text = selectedPlayer.unitName;

        UpdateSelectedPotrait();
        HUDManager.Instance.UpdateModel(selectedPlayer);

        foreach (int attrubuteIndex in Enum.GetValues(typeof(Attribute)))
        {
            Attribute currentAttribute = (Attribute)attrubuteIndex;
            Transform currentMasteryTransform = masteryScrollRect.content.GetChild(attrubuteIndex);
            ScrollRect progressionScrollRect = currentMasteryTransform.GetComponentInChildren<ScrollRect>();

            Mastery mastery = ProgressionManager.Instance.GetAttributeMastery(currentAttribute);
            ProgressionManager.PlayerMasteryProgression currentProgression = ProgressionManager.Instance.GetPlayerCurrentAttributeProgression(selectedPlayer, currentAttribute);

            int childIndexCounter = 0;
            int progressionStartIndex = 1 + mastery.sequencedProgressions.IndexOf(currentProgression.progression);

            foreach (Transform child in progressionScrollRect.content)
            {
                child.gameObject.SetActive(false);
            }

            progressionLength[currentMasteryTransform.name] = progressionStartIndex + 1;

            for (int i = progressionStartIndex; i >= 0; i--)
            {
                GameObject masteryComponent;

                if (progressionScrollRect.content.childCount > childIndexCounter)
                {
                    masteryComponent = progressionScrollRect.content.GetChild(childIndexCounter).gameObject;
                }
                else
                {
                    masteryComponent = Instantiate(progressionPrefab, progressionScrollRect.content);
                }

                masteryComponent.SetActive(true);

                if(childIndexCounter == 0)
                {
                    //First is Always locked
                    masteryComponent.GetComponent<MasteryUIComponent>().Lock();
                }
                else
                {
                    MasteryProgression spawnedProgression = mastery.sequencedProgressions[i];
                    int count = currentProgression.progression == spawnedProgression ? currentProgression.count : spawnedProgression.requiredCountToComplete;
                    masteryComponent.GetComponent<MasteryUIComponent>().SetData(spawnedProgression, count);
                }

                childIndexCounter++;
            }
        }

        UpdateSelectedMastery(0);
    }

    private void UpdateSelectedUnit(int indexChange)
    {
        CombatFunctions.UpdateListIndex(indexChange, currentSeletedUnit, out currentSeletedUnit, PartyManager.Instance.GetAllPlayerMembersInWorld().Count);
        BuildUI();
    }


    private void UpdateSelectedMastery(int indexChange)
    {
        CombatFunctions.UpdateListIndex(indexChange, currentMasteryIndex, out currentMasteryIndex, masteryScrollRect.content.childCount);

        if (activeProgressionScrollRect)
            activeProgressionScrollRect.content.GetChild(GetActiveIndex()).GetComponent<MasteryUIComponent>().IsSelected(false); //Deselect Current Progression.

        if (activeProgressionScrollRect && activeProgressionScrollRect.verticalNormalizedPosition > 0) //Auto Scroll Back to start.
            activeProgressionScrollRect.DOVerticalNormalizedPos(0, autoScrollTime);

        Transform mastery = masteryScrollRect.content.GetChild(currentMasteryIndex);
        activeProgressionScrollRect = mastery.GetComponentInChildren<ScrollRect>();

        foreach (Transform child in masteryScrollRect.content)
        {
            bool isSelected = child.GetSiblingIndex() == currentMasteryIndex;
            child.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().color = isSelected ? selectedBadgeTitle : defaultBadgeTitle;
        }

        //CombatFunctions.VerticalScrollToHighlighted(mastery as RectTransform, masteryScrollRect, currentMasteryIndex, masteryScrollRect.content.childCount);
        CombatFunctions.VerticalScrollHighlightedInView(mastery as RectTransform, masteryScrollRect, currentMasteryIndex, masteryScrollRect.content.childCount);
        currentMasteryProgressionLength = progressionLength[mastery.name];

        UpdateSelectedProgression(0);
    }

    private void UpdateSelectedProgression(int indexChange)
    {
        CombatFunctions.UpdateListIndex(indexChange, currentProgressionIndex, out currentProgressionIndex, currentMasteryProgressionLength - 1);//Subtract 1 because locked doesn't count.
        int activeIndex = GetActiveIndex();

        Transform selectedProgression = null;

        foreach (Transform child in activeProgressionScrollRect.content)
        {
            bool isSelected = child.GetSiblingIndex() == activeIndex;
            child.GetComponent<MasteryUIComponent>().IsSelected(isSelected);

            if (isSelected)
                selectedProgression = child;
        }

        if (selectedProgression)
            CombatFunctions.HorizontallScrollToHighlighted(selectedProgression as RectTransform, activeProgressionScrollRect, activeIndex, currentMasteryProgressionLength);
    }

    private int GetActiveIndex()
    {
        return currentMasteryProgressionLength - 1 - currentProgressionIndex;
    }

    private void UpdateSelectedPotrait()
    {
        foreach (Image potrait in potraits)
        {
            if (!potrait.gameObject.activeSelf) { continue; }

            float size = potrait.transform.GetSiblingIndex() == currentSeletedUnit ? selectedPotraitSize : defaultPotraitSize;
            potrait.rectTransform.DOSizeDelta(new Vector2(size, size), potraitAnimTime);
        }
    }


    private void OnTab(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "TabR")
            {
                UpdateSelectedUnit(1);
            }
            else if (context.action.name == "TabL")
            {
                UpdateSelectedUnit(-1);
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

                UpdateSelectedMastery(indexChange);
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
                UpdateSelectedProgression(indexChange);
            }
        }
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Exit") { return; }

        if (context.performed)
        {
            ActivateMasteryUI(false);
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnTab;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCycle;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnTab;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }
}
