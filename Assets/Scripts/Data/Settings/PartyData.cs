using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Sirenix.Serialization;

public class PartyData : MonoBehaviour, IControls, ISaveable
{
    public static PartyData Instance { get; private set; }
    [Header("Party Data")]
    [SerializeField] string playerLeaderName = "Keenan";
    [SerializeField] int maxCompanionsFollow = 3;
    [Space(10)]
    [SerializeField] int humanSkillsSiblingIndex = 0;
    [SerializeField] int mythicalSkillsSiblingIndex = 1;
    [Header("Accelerations")]
    [SerializeField] float defaultAcceleration = 15f;
    [SerializeField] float sneakAcceleration = 5f;
    [Header("Party Follow Data")]
    [SerializeField] float horizontalDistance = 2f;
    [SerializeField] float verticalDistance = 1f;
    [SerializeField] float distanceToBeginFollow = 2f;
    [SerializeField] float laggingBehindDistance = 4f;
    [Space(5)]
    [SerializeField] float maxLagSpeed = 12f;
    [Header("Party Sneak Follow Data")]
    [SerializeField] float sneakVerticalDistance = 2f;
    [SerializeField] float sneakDistanceToBeginFollow = 2f;
    [SerializeField] float sneakLaggingBehindDistance = 4f;
    [Space(5)]
    [SerializeField] float sneakMaxLagSpeed = 12f;
    [Header("FORMATION")]
    [SerializeField] GameObject formationUI;
    [SerializeField] Transform formationGridHeader;
    [Space(10)]
    [SerializeField] int gridCursorIndex = 1;
    [SerializeField] int gridSetPortraitIndex = 0;
    [SerializeField] int gridMovingPortraitIndex = 1;
    [Space(10)]
    [SerializeField] int formationRowCount = 5;
    [SerializeField] int formationColumnCount = 5;

    //Saving Data
    [SerializeField, HideInInspector]
    PartyState partyState = new PartyState();
    public bool AutoRestoreOnNewTerritoryEntry { get; set; } = false;

    //Events
    Action SwapCompanionPositionsEvent;

    //Active party members
    PlayerGridUnit leader;

    List<PlayerGridUnit> activeParty = new List<PlayerGridUnit>();
    List<PlayerGridUnit> allPatyMembers = new List<PlayerGridUnit>();
    List<PlayerGridUnit> partyFormationOrder = new List<PlayerGridUnit>();
    List<CompanionStateMachine> companions = new List<CompanionStateMachine>();

    //FORMATION DATA
    Vector2 combatCentreGridPos;
    Vector2 currentFormationGridPos = Vector2.zero;
    int currentGridIndex = 0;

    Sprite selectedPotrait = null;
    Dictionary<PlayerGridUnit, Vector2> combatFormationDict = new Dictionary<PlayerGridUnit, Vector2>();

    const string myActionKey = "FormationMenu";

    private void Awake()
    {
        Instance = this;

        CalculateCentreGridPos();
        SetParty();
    }

    private void OnEnable()
    {
        SetCompanionFollowBehaviour();

        SwapCompanionPositionsEvent += SwapCompanionPositions;
        ControlsManager.Instance.SubscribeToPlayerInput(myActionKey, this);
    }


    private void SetParty()
    {
        if (this.leader) { return; }

        List<PlayerGridUnit> playersFound = FindObjectsOfType<PlayerGridUnit>().ToList();

        allPatyMembers = playersFound;

        int leaderIndex = 0;
        PlayerGridUnit leader = null;

        foreach (PlayerGridUnit unit in playersFound)
        {
            if (unit.unitName.ToLower() == playerLeaderName.ToLower())
            {
                leader = unit;
                leaderIndex = playersFound.IndexOf(unit);
            }
        }

        //Set Leader At Front of list.
        playersFound.RemoveAt(leaderIndex);
        playersFound.Insert(0, leader);

        this.leader = leader;
        activeParty = playersFound;

        UpdateFormationUIActiveMembers();
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
        partyFormationOrder.Clear();

        foreach(Transform child in formationGridHeader)
        {
            if (child.GetChild(gridSetPortraitIndex).gameObject.activeSelf)
            {
                Sprite potrait = child.GetChild(gridSetPortraitIndex).GetChild(0).GetComponent<Image>().sprite;
                PlayerGridUnit player = activeParty.FirstOrDefault((member) => member.portrait.name == potrait.name);

                if (player)
                {
                    combatFormationDict[player] = GetGridPosRelativeToCentrePos(GridHeaderIndexToGridPos(child.GetSiblingIndex()));
                    partyFormationOrder.Add(player);
                }
            }
        }
    }

    public List<PlayerGridUnit> GetActivePlayerParty()
    {
        return new List<PlayerGridUnit>(activeParty);
    }

    public List<PlayerGridUnit> GetPartyFormationOrder()
    {
        return partyFormationOrder;
    }

    public GridPosition GetPlayerRelativeGridPosToCentrePos(PlayerGridUnit player, Vector3 leaderGlobalDirection)
    {
        GridPosition gridPos = new GridPosition();

         int xPos = (int)combatFormationDict[player].x;
         int yPos = (int)combatFormationDict[player].y;

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
                Sprite potrait = child.GetChild(gridSetPortraitIndex).GetChild(0).GetComponent<Image>().sprite;
                PlayerGridUnit player = activeParty.FirstOrDefault((member) => member.portrait.name == potrait.name);

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
        return playerLeaderName;
    }

    public PlayerGridUnit GetPlayerUnitViaName(string charName)
    {
        return allPatyMembers.FirstOrDefault((item) => item.unitName == charName);
    }

    public List<PlayerGridUnit> GetAllPlayerMembersInWorld()
    {
        return allPatyMembers;
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

    //PARTY FOLLOW BEHAVIOUR
    private void SetCompanionFollowBehaviour()
    {
        foreach (PlayerGridUnit member in activeParty)
        {
            if (member.TryGetComponent(out CompanionStateMachine companionStateMachine))
            {
                companions.Add(companionStateMachine);

                int index = companions.IndexOf(companionStateMachine);

                //Set Swap Pos Raise Event Designator
                if (index == 0)
                {
                    companionStateMachine.raiseSwapPosEventDesignee = true;
                }

                float localHorizontalDis = horizontalDistance;

                switch (index)
                {
                    case 0:
                        break;
                    case 1:
                        localHorizontalDis = horizontalDistance * -1f;
                        break;
                    case 2:
                        localHorizontalDis = 0;
                        break;
                }

                companionStateMachine.horizontalFollowOffset = localHorizontalDis;

                UpdateCompanionFollowBehaviour(companionStateMachine, false);
            }
        }
    }


    public void UpdateCompanionFollowBehaviour(CompanionStateMachine companion, bool isSneaking)
    {
        float localVerticalDis = isSneaking ? sneakVerticalDistance * -1f : verticalDistance * -1f;

        int index = companions.IndexOf(companion);

        switch (index)
        {
            case 0:
                break;
            case 1:
                break;
            case 2:
                localVerticalDis = localVerticalDis - 1f;
                break;
        }

        companion.verticalFollowOffset = localVerticalDis;
    }

    public float GetDistanceToBeginFollow(CompanionStateMachine companion, bool isSneaking)
    {
        float distance = isSneaking ? sneakDistanceToBeginFollow : distanceToBeginFollow;

        if (companions.IndexOf(companion) == maxCompanionsFollow - 1)
        {
            distance = distance + 1;
        }

        return distance;
    }

    public float GetLagSpeed(bool isSneaking)
    {
        return isSneaking ? sneakMaxLagSpeed : maxLagSpeed;
    }

    public float GetLaggingBehindDistance(bool isSneaking)
    {
        return isSneaking ? sneakLaggingBehindDistance : laggingBehindDistance;
    }

    public float GetAcceleration(bool isSneaking)
    {
        return isSneaking ? sneakAcceleration : defaultAcceleration;
    }
    public void SwapCompanionPositions()
    {
        if(companions.Count > 0)
            companions[0].horizontalFollowOffset = companions[0].horizontalFollowOffset * -1;

        if(companions.Count > 1)
            companions[1].horizontalFollowOffset = companions[1].horizontalFollowOffset * -1;
    }

    private void OnDisable()
    {
        SwapCompanionPositionsEvent -= SwapCompanionPositions;
    }

    //Saving
    
    
    [System.Serializable]
    public class PartyState
    {
        //Attribute & Skills
        public Dictionary<string, List<AttributeBonus>> partyAttributes = new Dictionary<string, List<AttributeBonus>>();
        public Dictionary<string, List<string>> partyLearnedSkills = new Dictionary<string, List<string>>();

        public Dictionary<string, Vector2> combatFormationDict = new Dictionary<string, Vector2>();
    }


    public object CaptureState()
    {
        foreach (PlayerGridUnit player in GetAllPlayerMembersInWorld())
        {
            partyState.partyAttributes[player.unitName] = new List<AttributeBonus>();
            //partyState.partyLearnedSkills[player.unitName] = new List<string>();

            //Store Attributes
            foreach (int i in Enum.GetValues(typeof(Attribute)))
            {
                AttributeBonus attribute = new AttributeBonus();

                attribute.attribute = (Attribute)i;
                attribute.attributeChange = player.stats.GetAttributeValueWithoutEquipmentBonuses((Attribute)i);

                partyState.partyAttributes[player.unitName].Add(attribute);
            }
        }

        //Capture Formation
        partyState.combatFormationDict = combatFormationDict.ToDictionary(k => k.Key.unitName, k => k.Value);

        return SerializationUtility.SerializeValue(partyState, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        if (state == null) //On New Game
        {
            SetFormation();
            return; 
        }

        byte[] bytes = state as byte[];
        partyState = SerializationUtility.DeserializeValue<PartyState>(bytes, DataFormat.Binary);

        SetParty();

        foreach (PlayerGridUnit player in GetAllPlayerMembersInWorld())
        {
            PlayerUnitStats playerStats = player.stats as PlayerUnitStats;
            if (!partyState.partyAttributes.ContainsKey(player.unitName)) { continue; }

            //Restore Attributes
            foreach (AttributeBonus data in partyState.partyAttributes[player.unitName])
            {
                Attribute attribute = data.attribute;
                playerStats.RestoreAttribute(attribute, data.attributeChange);
            }
        }
        //Restore Formation Dict
        combatFormationDict = partyState.combatFormationDict.ToDictionary(k => GetPlayerUnitViaName(k.Key), k => k.Value);
        UpdateSelectedFormationGrid();
        SetFormation();
    }
}
