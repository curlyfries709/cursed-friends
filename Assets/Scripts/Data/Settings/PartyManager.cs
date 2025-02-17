using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Sirenix.Serialization;

public class PartyManager : MonoBehaviour, IControls, ISaveable
{
    public static PartyManager Instance { get; private set; }

    [Header("Party State Data")]
    [SerializeField] PartyMemberData partyLeader;
    [Space(10)]
    [SerializeField] List<PartyMemberData> allPartyMembersNames = new List<PartyMemberData>();
    [Space(10)]
    [SerializeField] Transform partyStateDataHeader;
    [Header("Party Data")]
    [SerializeField] int humanSkillsSiblingIndex = 0;
    [SerializeField] int mythicalSkillsSiblingIndex = 1;
    [Header("FORMATION")]
    [SerializeField] GameObject formationUI;
    [SerializeField] Transform formationGridHeader;
    [Space(10)]
    [SerializeField] int gridCursorIndex = 2;
    [SerializeField] int gridSetPortraitIndex = 0;
    [SerializeField] int gridMovingPortraitIndex = 1;
    [Space(10)]
    [SerializeField] int formationRowCount = 5;
    [SerializeField] int formationColumnCount = 5;

    //Active party members
    PlayerGridUnit leader;

    List<PlayerGridUnit> activeParty = new List<PlayerGridUnit>();
    List<PlayerGridUnit> allPatyMembers = new List<PlayerGridUnit>();
    List<string> partyFormationOrder = new List<string>();

    //Events
    public Action PlayerPartyDataSet;

    //Cache 
    List<PlayerUnitStats> allPartyMembersStats = new List<PlayerUnitStats>();
    List<PlayerSkillset> allPlayerLearnedSkills = new List<PlayerSkillset>();

    //FORMATION DATA
    Vector2 combatCentreGridPos;
    Vector2 currentFormationGridPos = Vector2.zero;
    int currentGridIndex = 0;

    Sprite selectedPotrait = null;
    Dictionary<string, Vector2> combatFormationDict = new Dictionary<string, Vector2>();

    const string myActionKey = "FormationMenu";

    //Saving Data
    [SerializeField, HideInInspector]
    PartyState partyState = new PartyState();
    bool isDataRestored = false;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        
        CalculateCentreGridPos();
    }

    private void OnEnable()
    {
        ControlsManager.Instance.SubscribeToPlayerInput(myActionKey, this);
    }

    public void SetParty()
    {
        if (this.leader) { return; }

        List<PlayerGridUnit> playersFound = FindObjectsOfType<PlayerGridUnit>(false).ToList();

        if(playersFound.Count == 0) 
        {
            Debug.Log("Party Data found no Player Grid Units");
            return; 
        }

        allPatyMembers = playersFound;

        int leaderIndex = 0;
        PlayerGridUnit leader = null;

        foreach (PlayerGridUnit unit in playersFound)
        {
            if (unit.unitName.ToLower() == GetLeaderName().ToLower())
            {
                leader = unit;
                leaderIndex = playersFound.IndexOf(unit);
            }
        }

        if (!leader)
        {
            Debug.Log("LEADER NOT FOUND! SERIOUS PROBLEM");
            return;
        }

        //Set Leader At Front of list.
        playersFound.RemoveAt(leaderIndex);
        playersFound.Insert(0, leader);

        this.leader = leader;
        activeParty = playersFound;

        UpdateFormationUIActiveMembers();
        PlayerPartyDataSet?.Invoke();
    }

    public void ActivateFormationUI(bool activate)
    {
        if (activate)
        {
            AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

            selectedPotrait = null;
            currentGridIndex = 0;
            currentFormationGridPos = Vector2.zero;
            ControlsManager.Instance.SwitchCurrentActionMap(myActionKey);

            UpdateSelectedFormationGrid();
        }
        else
        {
            AudioManager.Instance.PlaySFX(SFXType.TabBack);
        }

        PhoneMenu.Instance.OpenApp(activate);
        formationUI.SetActive(activate);
    }

    private void SetFormation()
    {
        foreach(Transform child in formationGridHeader)
        {
            if (child.GetChild(gridSetPortraitIndex).gameObject.activeSelf)
            {
                Sprite portrait = child.GetChild(gridSetPortraitIndex).GetChild(0).GetComponent<Image>().sprite;
                PartyMemberData player = GetAllPartyMembersData().FirstOrDefault((member) => member.portrait.name == portrait.name);

                if (player)
                {
                    combatFormationDict[player.memberName] = GetGridPosRelativeToCentrePos(GridHeaderIndexToGridPos(child.GetSiblingIndex()));
                    partyFormationOrder.Add(player.memberName);
                }
                else
                {
                    Debug.Log("FORMATION COULD NOT FIND PLAYER FOR PORTRAIT NAME: " + portrait.name);
                }
            }
        }
    }


    public List<PlayerGridUnit> GetPartyFormationOrder()
    {
        List<PlayerGridUnit> partyFormationOrder = new List<PlayerGridUnit>();

        foreach(string playerName in this.partyFormationOrder)
        {
            PlayerGridUnit playerGridUnit = GetPlayerUnitViaName(playerName);

            //Check if pary member even spawned
            if (playerGridUnit)
            {
                partyFormationOrder.Add(playerGridUnit);
            } 
        }

        return partyFormationOrder;
    }

    public GridPosition GetPlayerRelativeGridPosToCentrePos(PlayerGridUnit player, Vector3 leaderGlobalDirection)
    {
        GridPosition gridPos = new GridPosition();

         int xPos = (int)combatFormationDict[player.unitName].x;
         int yPos = (int)combatFormationDict[player.unitName].y;

         if (leaderGlobalDirection == Vector3.right)
         {
             gridPos.x = yPos;
             gridPos.z = -xPos;
         }
         else if(leaderGlobalDirection == Vector3.left)
         {
             gridPos.x = -yPos;
             gridPos.z = xPos;
         }
         else if(leaderGlobalDirection == Vector3.back)
         {
             gridPos.x = -xPos;
             gridPos.z = -yPos;
         }
         else
         {
             gridPos.x = xPos;
             gridPos.z = yPos;
         }

        return gridPos;
    }

    private void CalculateCentreGridPos()
    {
        combatCentreGridPos = new Vector2(Mathf.FloorToInt(formationColumnCount / 2), 0);
    }


    private Vector2 GetGridPosRelativeToCentrePos(Vector2 gridPos)
    {
        return new Vector2(gridPos.x - combatCentreGridPos.x, -gridPos.y - 1);
    }

    private void UpdateFormationUIActiveMembers()
    {
        foreach (Transform child in formationGridHeader)
        {
            if (child.GetChild(gridSetPortraitIndex).gameObject.activeSelf)
            {
                Sprite portrait = child.GetChild(gridSetPortraitIndex).GetChild(0).GetComponent<Image>().sprite;
                PlayerGridUnit player = activeParty.FirstOrDefault((member) => member.portrait.name == portrait.name);
                
                if (!player)
                {
                    child.GetChild(gridSetPortraitIndex).gameObject.SetActive(false);
                }
            }
        }
    }

    private void UpdateSelectedFormationGrid()
    {
        foreach(Transform child in formationGridHeader)
        {
            bool isGridSelected = child.GetSiblingIndex() == currentGridIndex;

            if (selectedPotrait && isGridSelected)
            {
                child.GetChild(gridMovingPortraitIndex).GetChild(0).GetComponent<Image>().sprite = selectedPotrait;
                child.GetChild(gridMovingPortraitIndex).gameObject.SetActive(true);
            }
            else
            {
                child.GetChild(gridMovingPortraitIndex).gameObject.SetActive(false);
            }

            child.GetChild(gridCursorIndex).gameObject.SetActive(isGridSelected);
        }
    }

    private void TogglePortraitSelection()
    {
        Transform selectedGrid = formationGridHeader.GetChild(currentGridIndex);

        if (selectedPotrait)
        {
            if (selectedGrid.GetChild(gridSetPortraitIndex).gameObject.activeInHierarchy) 
            {
                AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
                return; 
            }

            AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

            selectedGrid.GetChild(gridSetPortraitIndex).GetChild(0).GetComponent<Image>().sprite = selectedPotrait;

            selectedGrid.GetChild(gridMovingPortraitIndex).gameObject.SetActive(false);
            selectedGrid.GetChild(gridSetPortraitIndex).gameObject.SetActive(true);

            selectedGrid.GetChild(gridMovingPortraitIndex).GetChild(0).GetComponent<Image>().sprite = selectedPotrait;

            selectedPotrait = null;
        }
        else
        {
            if (!selectedGrid.GetChild(gridSetPortraitIndex).gameObject.activeInHierarchy) 
            {
                AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
                return; 
            }

            AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

            selectedPotrait = selectedGrid.GetChild(gridSetPortraitIndex).GetChild(0).GetComponent<Image>().sprite;
            selectedGrid.GetChild(gridMovingPortraitIndex).GetChild(0).GetComponent<Image>().sprite = selectedPotrait;

            selectedGrid.GetChild(gridSetPortraitIndex).gameObject.SetActive(false);
            selectedGrid.GetChild(gridMovingPortraitIndex).gameObject.SetActive(true);
        }
    }

    private void UpdateFormationIndices(Vector2 cursorDirection)
    {
        if(cursorDirection != Vector2.zero)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        Vector2 updatedCursorDirection = new Vector2(cursorDirection.x, cursorDirection.y * -1);

        if (currentFormationGridPos.x + updatedCursorDirection.x >= formationColumnCount)
        {
            float yPos = currentFormationGridPos.y + 1;

            if(yPos >= formationRowCount)
            {
                yPos = 0;
            }

            Vector2 newPos = new Vector2(0, yPos);
            currentFormationGridPos = newPos;
        }
        else if (currentFormationGridPos.x + updatedCursorDirection.x < 0)
        {
            float yPos = currentFormationGridPos.y - 1;

            if (yPos < 0)
            {
                yPos = formationRowCount - 1;
            }

            Vector2 newPos = new Vector2(formationColumnCount - 1, yPos);
            currentFormationGridPos = newPos;
        }
        else if (currentFormationGridPos.y + updatedCursorDirection.y >= formationRowCount)
        {
            Vector2 newPos = new Vector2(currentFormationGridPos.x, 0);
            currentFormationGridPos = newPos;
        }
        else if (currentFormationGridPos.y + updatedCursorDirection.y < 0)
        {
            Vector2 newPos = new Vector2(currentFormationGridPos.x, formationRowCount-1);
            currentFormationGridPos = newPos;
        }
        else
        {
            currentFormationGridPos = currentFormationGridPos + updatedCursorDirection;
        }

        currentGridIndex = GridPosToGridIndex(currentFormationGridPos);

        UpdateSelectedFormationGrid();
    }

    private int GridPosToGridIndex(Vector2 gridPos)
    {
        //0, 0 = 0 
        //1, 0 = 1
        //0, 1 = 5
        //0, 2 = 10
        
        return Mathf.RoundToInt(gridPos.x + (gridPos.y * formationColumnCount));
    }

    private Vector2 GridHeaderIndexToGridPos(int index)
    {
        //0, 0 = 0 
        //1, 0 = 1
        //0, 1 = 5
        //0, 2 = 10  
        //1, 3 = 16  16/5 = 3  
        return new Vector2(index % formationRowCount, Mathf.Floor(index / formationRowCount)); 
    }

    //GETTERS
    public bool IsLeader(PartyMemberData member)
    {
        return member.memberName == GetLeaderName();
    }

    public PlayerGridUnit GetLeader()
    {
        return leader;
    }

    public int GetLeaderLevel()
    {
        return leader.stats.level;
    }

    public string GetLeaderName()
    {
        return partyLeader.memberName;
    }

    public PlayerGridUnit GetPlayerUnitViaName(string charName)
    {
        return allPatyMembers.FirstOrDefault((item) => item.unitName == charName);
    }

    public List<PlayerGridUnit> GetActivePlayerParty()
    {
        return new List<PlayerGridUnit>(activeParty);
    }

    public List<PlayerGridUnit> GetAllPlayerMembersInWorld()
    {
        return allPatyMembers;
    }

    public List<PartyMemberData> GetAllPartyMembersData()
    {
        return allPartyMembersNames;
    }

    public PlayerUnitStats GetPartyMemberStats(PartyMemberData memberData)
    {     
        return GetAllPartyMemberStats().FirstOrDefault((item) => item.GetPartyMemberData() == memberData);
    }

    public PlayerSkillset GetPartyMemberLearnedSkill(PartyMemberData memberData)
    {
        if(allPlayerLearnedSkills.Count == 0)
        {
            allPlayerLearnedSkills = partyStateDataHeader.GetComponentsInChildren<PlayerSkillset>().ToList();
        }

        return allPlayerLearnedSkills.FirstOrDefault((item) => item.GetPartyMemberData() == memberData);
    }

    public List<PlayerUnitStats> GetAllPartyMemberStats()
    {
        if(allPartyMembersStats.Count == 0)
        {
            allPartyMembersStats = partyStateDataHeader.GetComponentsInChildren<PlayerUnitStats>().ToList();
        }

        return allPartyMembersStats;
    }

    public int GetSkillHeaderIndex()
    {
        UnitStats playerStat = allPatyMembers[0].stats;

        if (playerStat.data.race == Race.Human)
        {
            return humanSkillsSiblingIndex;
        }

        return mythicalSkillsSiblingIndex;
    }

    //Inputs.
    private void OnMoveCursor(InputAction.CallbackContext context)
    {
        if (context.action.name != "MoveCursor") { return; }

        if (context.performed)
        {
            UpdateFormationIndices(context.ReadValue<Vector2>());
        }
    }

    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            TogglePortraitSelection();
        }
    }

    private void OnExit(InputAction.CallbackContext context)
    {
        if (context.action.name != "Exit") { return; }

        if (context.performed)
        {
            SetFormation();
            ActivateFormationUI(false);
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnMoveCursor;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnExit;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnMoveCursor;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnExit;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }

    
    //Saving
    
    
    [System.Serializable]
    public class PartyState
    {

        public Dictionary<string, Vector2> combatFormationDict = new Dictionary<string, Vector2>();
    }


    public object CaptureState()
    {
        //Capture Formation
        partyState.combatFormationDict = combatFormationDict;

        return SerializationUtility.SerializeValue(partyState, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        isDataRestored = true;

        if (state == null) //On New Game
        {
            SetFormation();
            return; 
        }

        byte[] bytes = state as byte[];
        partyState = SerializationUtility.DeserializeValue<PartyState>(bytes, DataFormat.Binary);

        //Restore Formation Dict
        combatFormationDict = partyState.combatFormationDict;
        UpdateSelectedFormationGrid();
        SetFormation();
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
