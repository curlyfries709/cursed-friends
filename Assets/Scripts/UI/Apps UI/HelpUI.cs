using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using AnotherRealm;
using TMPro;

public class HelpUI : MonoBehaviour, IControls
{
    [SerializeField] GameObject battleArea;
    [SerializeField] GameObject roamArea;
    [Space(10)]
    [SerializeField] ScrollRect battleScrollRect;
    [SerializeField] ScrollRect roamScrollRect;
    [SerializeField] Transform topicHeader;
    [Space(10)]
    [SerializeField] TextMeshProUGUI battleTitle;
    [SerializeField] TextMeshProUGUI roamTitle;
    [Space(10)]
    [SerializeField] Color defaultColor = Color.white;
    [SerializeField] Color selectedTitleColor;

    ScrollRect activeScrollRect;
    GameObject activeTutorial;

    int topicIndex = 0;
    int tutorialIndex = 0;

    bool isBattleAreaActive = true;

    private void Awake()
    {
        ControlsManager.Instance.SubscribeToPlayerInput("GridMenu", this);
    }

    public void ActivateUI(bool activate)
    {
        if (activate)
        {
            isBattleAreaActive = true;

            battleArea.SetActive(isBattleAreaActive);
            roamArea.SetActive(!isBattleAreaActive);

            activeScrollRect = battleScrollRect;
            topicIndex = 0;
            tutorialIndex = 0;

            UpdateTabVisual();

            UpdateUI(0);
        }

        PhoneMenu.Instance.OpenApp(activate);
        gameObject.SetActive(activate);

        if (activate)
            ControlsManager.Instance.SwitchCurrentActionMap(this);
    }

    private void UpdateActiveTab()
    {
        AudioManager.Instance.PlaySFX(SFXType.TabForward);

        isBattleAreaActive = !isBattleAreaActive;

        battleArea.SetActive(isBattleAreaActive);
        roamArea.SetActive(!isBattleAreaActive);

        activeScrollRect = isBattleAreaActive ? battleScrollRect : roamScrollRect;
        topicIndex = 0;
        tutorialIndex = 0;

        UpdateTabVisual();

        UpdateUI(0);
    }

    private void UpdateTabVisual()
    {
        battleTitle.color = isBattleAreaActive ? selectedTitleColor : defaultColor;
        roamTitle.color = isBattleAreaActive ? defaultColor : selectedTitleColor;

        battleTitle.transform.GetChild(0).gameObject.SetActive(isBattleAreaActive);
        roamTitle.transform.GetChild(0).gameObject.SetActive(!isBattleAreaActive);
    }

    private void UpdateUI(int indexChange)
    {
        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        tutorialIndex = 0;
        CombatFunctions.UpdateListIndex(indexChange, topicIndex, out topicIndex, activeScrollRect.content.childCount);

        foreach (Transform child in activeScrollRect.content)
        {
            child.GetChild(0).gameObject.SetActive(child.GetSiblingIndex() == topicIndex);
        }

        CombatFunctions.VerticalScrollHighlightedInView(activeScrollRect.content.GetChild(topicIndex) as RectTransform, activeScrollRect, topicIndex, activeScrollRect.content.childCount);

        activeScrollRect.content.GetChild(topicIndex).GetComponent<Button>()?.onClick.Invoke();

        UpdateActiveTutorial(0);
    }

    private void UpdateActiveTutorial(int indexChange)
    {
        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.TabForward);

        CombatFunctions.UpdateListIndex(indexChange, tutorialIndex, out tutorialIndex, activeTutorial.transform.childCount);

        foreach (Transform child in activeTutorial.transform)
        {
            child.gameObject.SetActive(child.GetSiblingIndex() == tutorialIndex);
        }
    }

    public void ActivateTopic(GameObject topic)
    {
        activeTutorial = topic;

        foreach(Transform child in topicHeader)
        {
            child.gameObject.SetActive(child.gameObject == topic);
        }
    }

    //Input
    private void OnTab(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "TabR" || context.action.name == "TabL")
            {
                UpdateActiveTab();
            }
        }
    }


    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.action.name == "CycleR" || context.action.name == "CycleL")
        {
            if (context.performed)
            {
                int indexChange = context.action.name == "CycleR" ? 1 : -1;
                UpdateActiveTutorial(indexChange);
            } 
        }
    }

    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
        {
            if (context.performed)
            {
                int indexChange = context.action.name == "ScrollD" ? 1: -1;
                UpdateUI(indexChange);
            }
        }
    }

    private void OnExit(InputAction.CallbackContext context)
    {
        if (context.action.name != "Exit") { return; }

        if (context.performed)
        {
            AudioManager.Instance.PlaySFX(SFXType.TabBack);
            ActivateUI(false);
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnTab;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnExit;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnTab;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnExit;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }
}
