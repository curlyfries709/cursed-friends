using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using AnotherRealm;
using TMPro;
using UnityEngine.UI;

public class SkillsAppUI : MonoBehaviour, IControls
{
    [Header("Headers")]
    [SerializeField] Transform partyHeader;
    [Space(5)]
    [SerializeField] ScrollRect skillScrollRect;
    [Header("Titles")]
    [SerializeField] TextMeshProUGUI currentSkillOwnerTitle;
    [Header("Areas")]
    [SerializeField] GameObject selectCharacterArea;
    [SerializeField] GameObject skillsArea;
    [Header("Skill UI")]
    [SerializeField] TextMeshProUGUI skillQuickData;
    [SerializeField] TextMeshProUGUI skillDescription;
    [SerializeField] Transform skillAOEDiagramHeader;
    [Header("Prefabs")]
    [SerializeField] GameObject skillPrefab;
    [Header("Component UI")]
    [SerializeField] HUDHealthUI[] unitHealthUIData = new HUDHealthUI[7];
    [Header("Indices")]
    [SerializeField] int skillHighlightedIndex = 0;
    [SerializeField] int skillIconIndex = 1;
    [SerializeField] int skillNameIndex = 2;
    [SerializeField] int skillCostIndex = 3;

    //Lists
    List<PlayerBaseSkill> currentSkillList = new List<PlayerBaseSkill>();

    List<PlayerGridUnit> allPlayers = new List<PlayerGridUnit>();

    //Indices
    PlayerGridUnit selectedCharacter = null;

    int currentSkillIndex = 0;
    int currentPlayerIndex = 0;
    int currentUseSkillIndex = 0;

    bool usingSkill = false;

    const string myActionMap = "InteractiveMenu";


    private void Awake()
    {
        ControlsManager.Instance.SubscribeToPlayerInput(myActionMap, this); 
    }

    public void ActivateUI(bool activate)
    {
        if (activate)
        {
            AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

            ResetData();
            SetHealthUI();
            UpdateSelectedCharacter(0);
        }
        else
        {
            AudioManager.Instance.PlaySFX(SFXType.TabBack);
        }

        PhoneMenu.Instance.OpenApp(activate);
        gameObject.SetActive(activate);

        if (activate)
            ControlsManager.Instance.SwitchCurrentActionMap(this);
    }

    private void ResetData()
    {
        selectedCharacter = null;
        usingSkill = false;

        currentSkillIndex = 0;
        currentPlayerIndex = 0;
        currentUseSkillIndex = 0;

        allPlayers = PartyData.Instance.GetAllPlayerMembersInWorld();

        ResetDescription();
    }

    private void ResetDescription()
    {
        skillQuickData.text = "";
        skillDescription.text = "";

        //Clean AOE Diagram
        if (skillAOEDiagramHeader.childCount > 0)
            Destroy(skillAOEDiagramHeader.GetChild(0).gameObject);
    }

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

    private void UpdateSelectedCharacter(int indexChange)
    {
        int index;

        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        if (usingSkill)
        {
            CombatFunctions.UpdateListIndex(indexChange, currentUseSkillIndex, out currentUseSkillIndex, allPlayers.Count);
            index = currentUseSkillIndex;
        }
        else
        {
            CombatFunctions.UpdateListIndex(indexChange, currentPlayerIndex, out currentPlayerIndex, allPlayers.Count);
            index = currentPlayerIndex;
        }

        selectCharacterArea.SetActive(!selectedCharacter);
        skillsArea.SetActive(selectedCharacter);

        PlayerGridUnit currentPlayer = allPlayers[index];

        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (i >= allPlayers.Count)
            {
                continue;
            }
            unitHealthUIData[i].IsSelected(i == index);
        }

        string appendString = "'s Skills";
        currentSkillOwnerTitle.text = currentPlayer.unitName + appendString;
    }

    private void SelectOption()
    {
        if (selectedCharacter) { return; }

        selectedCharacter = allPlayers[currentPlayerIndex];
        UpdateSelectedCharacter(0);
        CreateSkillUI(selectedCharacter);
        
    }

    private void CancelSelectedCharacter()
    {
        AudioManager.Instance.PlaySFX(SFXType.TabBack);

        currentSkillIndex = 0;

        selectedCharacter = null;
        UpdateSelectedCharacter(0);

        //Reset Description
        ResetDescription();
    }

    //SKILL UI
    private void CreateSkillUI(PlayerGridUnit player)
    {
        skillsArea.SetActive(selectedCharacter);
        selectCharacterArea.SetActive(!selectedCharacter);

        currentSkillList = player.GetActiveLearnedSkills();

        foreach (Transform child in skillScrollRect.content)
        {
            child.gameObject.SetActive(false);
        }

        foreach (PlayerBaseSkill skill in currentSkillList)
        {
            int index = currentSkillList.IndexOf(skill);

            GameObject skillGO;

            if (skillScrollRect.content.childCount > index)
            {
                skillGO = skillScrollRect.content.GetChild(index).gameObject;
            }
            else
            {
                skillGO = Instantiate(skillPrefab, skillScrollRect.content);
            }

            skillGO.SetActive(true);

            //SetIcon
            foreach (Transform icon in skillGO.transform.GetChild(skillIconIndex))
            {
                icon.gameObject.SetActive(icon.GetSiblingIndex() == skill.GetSkillIndex());
            }

            //Update Name
            skillGO.transform.GetChild(skillNameIndex).GetComponent<TextMeshProUGUI>().text = skill.skillName;

            int costTypeIndex = CombatFunctions.GetSkillCostTypeIndex(skill.costType);

            //Update Cost
            foreach (Transform costType in skillGO.transform.GetChild(skillCostIndex))
            {
                bool isCostType = costType.GetSiblingIndex() == costTypeIndex;
                costType.gameObject.SetActive(isCostType);

                if (isCostType)
                {
                    costType.GetComponent<TextMeshProUGUI>().text = skill.GetCost().ToString();
                }
            }
        }

        UpdateSkillUI(0);
    }

    private void UpdateSkillUI(int indexChange)
    {
        if (currentSkillList.Count == 0) { return; }

        if (indexChange != 0 && currentSkillList.Count > 1)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        CombatFunctions.UpdateListIndex(indexChange, currentSkillIndex, out currentSkillIndex, currentSkillList.Count);

        foreach (Transform child in skillScrollRect.content)
        {
            if (!child.gameObject.activeInHierarchy) { continue; }

            child.GetChild(skillHighlightedIndex).gameObject.SetActive(child.GetSiblingIndex() == currentSkillIndex);
        }

        skillQuickData.text = currentSkillList[currentSkillIndex].quickData;
        skillDescription.text = currentSkillList[currentSkillIndex].description;

        //Update AOE Diagram
        if (skillAOEDiagramHeader.childCount > 0)
            Destroy(skillAOEDiagramHeader.GetChild(0).gameObject);

        Instantiate(currentSkillList[currentSkillIndex].aoeDiagram, skillAOEDiagramHeader);

        //Update Scrollview
        CombatFunctions.VerticalScrollToHighlighted(skillScrollRect.content.GetChild(currentSkillIndex).transform as RectTransform, skillScrollRect, currentSkillIndex, currentSkillList.Count);
    }


    //INput
    private void OnUse(InputAction.CallbackContext context)
    {
        if (context.action.name != "Use1") { return; }

        if (context.performed)
        {
            //Implement Later When Healing Added
        }
    }

    private void OnForget(InputAction.CallbackContext context)
    {
        if (context.action.name != "Use2") { return; }

        if (context.performed)
        {
            //Implement Later
        }
    }


    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
            {
                int indexChange = context.action.name == "ScrollD" ? 1 : -1;

                if (!selectedCharacter)
                {
                    UpdateSelectedCharacter(indexChange);
                }
                else
                {
                    UpdateSkillUI(indexChange);
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
            if (selectedCharacter)
            {
                CancelSelectedCharacter();
            }
            else
            {
                ActivateUI(false);
            }
        }
    }



    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnUse;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnForget;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnUse;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnForget;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }
}
