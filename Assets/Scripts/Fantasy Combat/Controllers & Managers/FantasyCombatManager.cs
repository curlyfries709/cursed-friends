using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Linq;
using Sirenix.OdinInspector;
using MoreMountains.Feedbacks;
using AnotherRealm;

public enum BattleResult
{
    Victory,
    Defeat,
    Fled,
    Restart
}

public class FantasyCombatManager : MonoBehaviour, IControls
{
    public static FantasyCombatManager Instance { get; private set; }

    [Title("Controllers & Managers")]
    [SerializeField] FantasyCombatMovement combatMovementController;
    [SerializeField] FantasyCombatCollectionManager collectionManager;
    [Space(10)]
    [SerializeField] Victory victory;
    [SerializeField] Defeat defeat;
    [SerializeField] Flee flee;
    [Title("UI")]
    [SerializeField] FantasyCombatHUD hud;
    [SerializeField] GameObject photoshootSet;
    [Title("Custom Pass")] 
    [SerializeField] GameObject uICustomPass;
    [SerializeField] GameObject unitObscuredCustomPass;
    [Title("Timers")]
    [SerializeField] float retryDelay = 0.25f;
    [Space(5)]
    [SerializeField] float skillFeedbackDisplayTime = 1f;
    [SerializeField] float victoryBlowDisplayTime = 0.5f;
    [Space(5)]
    [SerializeField] float resistDisplayExtension = 0.15f;
    [SerializeField] float knockdownDisplayExtension = 0.35f;
    [SerializeField] float unitKOedExtension = 0.85f;
    [Header("AI Action Delays")]
    public float onTurnStartDelay = 0.25f;
    public float actDelayIfNoMove = 0.25f;
    [Title("Turn Order Calculations")]
    //[SerializeField] float constantDividedBySpeed = 100;
    [SerializeField] float actionTimeSubtractionConstant = 2;
    [Space(10)]
    [SerializeField] float baseActionTime = 800;
    [SerializeField] float maxActionTimeIncrement = 5;
    [SerializeField] int NSpeedTurnToDecreaseIncrement = 10;
    [Title("Fired Up Data")]
    public int fpBasicGainAmount = 3;
    public int fpEnhancedGainAmount = 5;
    [Space(10)]
    public int fpLossAmount = 20;
    [Header("TEST")]
    [SerializeField] bool beginCombatOnPlay = true;
    [SerializeField] BattleStarter.CombatAdvantage combatAdvantageType;
    [SerializeField] EnemyIntiateCombat testBattleTrigger;

    //Battle Time & Counters
    public int battleTurnNumber { get; private set; } = 0;

    float battleStartTime = -1;
    const string myActionMap = "FantasyCombat";

    //Variables
    CharacterGridUnit activeUnit = null; //Which Unit is Acting...Due to Again Event. It's possible for Units to act on someone else's turn.
    CharacterGridUnit currentTurnOwner = null; //The Owner of the current Turn.

    PlayerGridUnit selectedPlayerUnit = null;
    PlayerBaseSkill currentSelectedSkill = null;

    float currentSkillFeedbackDisplayTime = 0;

    bool currentSelectedSkillTriggered = false;
    bool isHealthUICooldownRunning = false;

    bool isTurnStartEventPlaying = false;
    bool isPassiveHealthUIActive = false;

    bool inCombat = false;
    bool usedTactic = false;
    public bool restartingBattle { get; private set; } = false;

    [HideInInspector] public bool CombatCinematicPlaying = false; //Set By POF & Duofires, Read by Equipment script to avoid running unnecessary Methods.
    public Transform postCombatWarpPoint { get; private set; } = null; //Set By Cinematic Area triggers, if player enters them during Combat.

    //Turn Order Lists & Dicts
    List<CharacterGridUnit> allCharacterCombatUnits = new List<CharacterGridUnit>();
    List<CharacterGridUnit> turnOrder = new List<CharacterGridUnit>();
    
    Dictionary<CharacterGridUnit, float> unitCurrentActionTimeDict = new Dictionary<CharacterGridUnit, float>();

    //Unit Lists
    [HideInInspector] public CharacterGridUnit TeamAttackInitiator = null;

    List<GridUnit> unitsToShow = new List<GridUnit>();

    List<PlayerGridUnit> playerCombatParticipants = new List<PlayerGridUnit>();
    List<CharacterGridUnit> enemyCombatParticipants = new List<CharacterGridUnit>();

    List<CharacterGridUnit> patrollingEnemiesJoinedDuringBattle = new List<CharacterGridUnit>();
    List<CharacterGridUnit> spawnedUnitsDuringBattle = new List<CharacterGridUnit>();

    //Queues
    List<ITurnStartEvent> turnStartEvents = new List<ITurnStartEvent>();
    List<ITurnEndEvent> turnEndEvents = new List<ITurnEndEvent>();

    //Storage
    Dictionary<GridUnit, UnitDataAtBattleStart> dataAtBattleStart = new Dictionary<GridUnit, UnitDataAtBattleStart>();
    Dictionary<int, float> speedToActionTimeDict = new Dictionary<int, float>();

    List<GridUnit> allUnitsAtBattleStart = new List<GridUnit>();

    BattleStarter.CombatAdvantage battleAdvantageType;
    public IBattleTrigger battleTrigger { get; private set; }

    //Inputs 
    bool gridSelectionMode = false; //Keyboard & Mouse Only

    //Cache
    PlayerInput playerInput;
    GameObject mainCam;
    //Events
    public Action<BattleStarter.CombatAdvantage> CombatBegun;
    public Action<BattleResult, IBattleTrigger> CombatEnded;
    public Action BattleRestarted;

    public Action<CharacterGridUnit, int> OnNewTurn; //Turn Owner, Turn Number
    public Action OnTurnFinished;
    public Action ActionComplete;

    public struct UnitDataAtBattleStart
    {
        public int healthAtStart;
        public int spAtStart;
        public int fpAtStart;

        public Weapon weaponAtStart;

        public Vector3 directionAtStart;
        public List<GridPosition> gridPosAtStart;
    }



    private void Awake()
    {
        Instance = this;

        playerInput = ControlsManager.Instance.GetPlayerInput();
        mainCam = GameObject.FindGameObjectWithTag("MainCamera");

        SetActionTimes();

        ActiveUnitObscureCustomPass(false);
    }

    private void OnEnable()
    {
        //Combat Events
        ActionComplete += OnActionComplete;
        FantasyHealth.CharacterUnitKOed += OnUnitKO;
        FantasyHealth.CharacterUnitRevived += OnUnitRevive;
        Flee.UnitFled += OnUnitFlee;
        CombatEnded += OnCombatEnd;
        ControlsManager.Instance.SubscribeToPlayerInput(myActionMap, this);
    }


    // Start is called before the first frame update
    void Start()
    {
        ShowHUD(true);
        ShowHUD(false);

        if (beginCombatOnPlay)
        {
            GetCombatants();
            OnCombatBegin(combatAdvantageType, false);
        }
    }



    // Update is called once per frame
    void Update()
    {
        if (selectedPlayerUnit && !currentSelectedSkillTriggered)
        {
            combatMovementController.BasicUnitMovement(CanMovePlayer());
        }

        if (currentSelectedSkill)
        {
            currentSelectedSkill.SkillSelected();
        }
    }

    public void BeginCombat(BattleStarter.CombatAdvantage advantageType, IBattleTrigger battleTrigger, List<PlayerGridUnit> playerCombatants, List<CharacterGridUnit> enemyCombatants, bool isRetry = false)
    {
        this.battleTrigger = battleTrigger;

        ReadyCombatants(playerCombatants, enemyCombatants);
        OnCombatBegin(advantageType, isRetry);
    }

    public void RestartBattle()
    {
        restartingBattle = true;
        postCombatWarpPoint = null;

        ClearEventQueues();

        playerCombatParticipants.Clear();
        enemyCombatParticipants.Clear();

        foreach (CharacterGridUnit enemy in patrollingEnemiesJoinedDuringBattle)
        {
            enemy.Health().ResetVitals();
            enemy.ActivateUnit(true); //InCase They were dead.
            enemy.GetComponent<EnemyStateMachine>().WarpBackToPatrol(); 
        }

        foreach(CharacterGridUnit spawnedUnit in spawnedUnitsDuringBattle)
        {
            //I Suppose Deactivate them
            spawnedUnit.ActivateUnit(false);
        }

        spawnedUnitsDuringBattle.Clear();
        patrollingEnemiesJoinedDuringBattle.Clear();

        //Cache
        int leaderIndex = 0;
        PlayerGridUnit leader = null;

        foreach(GridUnit unit in allUnitsAtBattleStart)
        {
            UnitDataAtBattleStart data = dataAtBattleStart[unit];

            unit.GetDamageable().ResetStateToBattleStart(data.healthAtStart, data.spAtStart, data.fpAtStart);
            //Warp & Set Position

            unit.Warp(LevelGrid.Instance.gridSystem.GetWorldPosition(data.gridPosAtStart[0]), Quaternion.LookRotation(data.directionAtStart));
            unit.SetGridPositions();

            //Debug.Log("Warping: " + unit.unitName + "to: " + data.gridPosAtStart[0].ToString());

            CharacterGridUnit charUnit = unit as CharacterGridUnit;

            if (!charUnit) 
            {
                continue; 
            }

            StatusEffectManager.Instance.ResetUnitToBattleStartState(charUnit);

            PlayerGridUnit player = unit as PlayerGridUnit;

            if (player)
            {
                player.stats.EquipWeapon(data.weaponAtStart);
                player.unitAnimator.ShowWeapon(true);

                playerCombatParticipants.Add(player);

                if(player == PartyData.Instance.GetLeader())
                {
                    leader = player;
                    leaderIndex = playerCombatParticipants.IndexOf(player);
                }
            }
            else
            {
                enemyCombatParticipants.Add(charUnit);
            }

            charUnit.GetComponent<StateMachine>().BeginCombat();
        }

        //Set Leader At Front of list.
        playerCombatParticipants.RemoveAt(leaderIndex);
        playerCombatParticipants.Insert(0, leader);

        BattleRestarted?.Invoke(); //InventoryManager & Enemy Database & CollectionManager subscribed
        BeginCombat(battleAdvantageType, battleTrigger, playerCombatParticipants, enemyCombatParticipants, true);
    }

    private void OnCombatBegin(BattleStarter.CombatAdvantage advantageType, bool isRetry)
    {
        ActiveUnitObscureCustomPass(true);

        inCombat = true;
        restartingBattle = false;
        battleTurnNumber = 0;

        SetAllActiveCombatUnits();
        StoreBattleData(advantageType, isRetry);

        ControlsManager.Instance.DisableControls();
        ShowHUD(true);

        HUDManager.Instance.OnCombatBegin(playerCombatParticipants);
        EnemyDatabase.Instance.SetEnemyDisplayNames(true);
        CalculateTurnOrder(advantageType);

        //Play Music.
        AudioManager.Instance.PlayBattleMusic(battleTrigger);

        //Trigger Event
        CombatBegun?.Invoke(advantageType);

        if (beginCombatOnPlay || isRetry)
        {
            if(isRetry)
                UpdateActiveCamera(turnOrder[0], true);

            Invoke("OnTurnStart", retryDelay);
        }
        else
        {
            OnTurnStart();
        }
    }


    private void StoreBattleData(BattleStarter.CombatAdvantage advantageType, bool isRetry)
    {
        battleStartTime = Time.time;

        if (isRetry) { return; }

        collectionManager.StoreBattleStartData();
        EnemyDatabase.Instance.StoreBattleStartData();

        dataAtBattleStart.Clear();
        StatusEffectManager.Instance.ClearBattleStartData();
        battleAdvantageType = advantageType;

        allUnitsAtBattleStart = new List<GridUnit>(LevelGrid.Instance.GetAllActiveGridUnits());

        foreach (GridUnit unit in allUnitsAtBattleStart)
        {
            UnitDataAtBattleStart data = new UnitDataAtBattleStart();

            data.healthAtStart = unit.GetDamageable().currentHealth;
            data.spAtStart = unit.GetDamageable().currentSP;
            data.fpAtStart = unit.GetDamageable().currentFP;

            data.gridPosAtStart = new List<GridPosition>(unit.GetGridPositionsOnTurnStart());
            data.directionAtStart = CombatFunctions.GetDirectionAsVector(unit.transform);

            PlayerGridUnit player = unit as PlayerGridUnit;

            if (player)
            {
                data.weaponAtStart = player.stats.Weapon();
            }

            dataAtBattleStart[unit] = data;

            //Debug.Log("Stored Data for " + unit.unitName + " at: " + data.gridPosAtStart[0].ToString());

            StatusEffectManager.Instance.StoreBattleStartData(unit as CharacterGridUnit);
        }
    }


    public void IntializeUnit(CharacterGridUnit unit)
    {
        if(!(unit is PlayerGridUnit))
        {
            EnemyDatabase.Instance.NewEnemyEncountered(unit);
        }

        unit.Health().SetupHealthUI();
        StatusEffectManager.Instance.IntializeUnitStatusEffects(unit);
    }

    public void AddNewUnitDuringBattle(CharacterGridUnit unit, bool wasSpawned)
    {
        enemyCombatParticipants.Add(unit);
        allCharacterCombatUnits.Add(unit);

        InsertUnitInTurnOrder(unit);

        //IntializeUnit.
        IntializeUnit(unit);
        EnemyDatabase.Instance.SetEnemyDisplayNames(false);
        unit.stats.SpawnEnchantments();

        if (wasSpawned)
        {
            spawnedUnitsDuringBattle.Add(unit);
        }
        else
        {
            patrollingEnemiesJoinedDuringBattle.Add(unit);
        }
    }

    private void ReadyCombatants(List<PlayerGridUnit> playerCombatants, List<CharacterGridUnit> enemyCombatants)
    {
        playerCombatParticipants = new List<PlayerGridUnit>(playerCombatants);
        enemyCombatParticipants = new List<CharacterGridUnit>(enemyCombatants);

        EnemyDatabase.Instance.UpdateEnemyCombatants(enemyCombatParticipants);
    }

    private void BeginNextTurn()
    {
        HideAllKOedEnemies();

        //Call Turn End Events
        currentTurnOwner.EndTurn?.Invoke();
        OnTurnFinished?.Invoke();

        //Remove First Unit from Turn Order List.
        if (currentTurnOwner == turnOrder[0])
        {
            //In case Active Unit dies during their turn.
            turnOrder.RemoveAt(0);
        }

        OnTurnStart();
    }

    private void OnTurnStart()
    {
        battleTurnNumber++;
        usedTactic = false;

        ActiveUserChangeSetup(turnOrder[0], false);
        CalculateActiveUnitsNextTurn();

        //New Turn Begun & Always Called Before Turn Start Events Begin.
        OnNewTurn?.Invoke(currentTurnOwner, battleTurnNumber);

        //Play Turn Start Events
        StartTurnOrPlayEvent();
    }

    private void TurnStartContinuation()
    {
        if (!StatusEffectManager.Instance.IsUnitDisabled(currentTurnOwner))
        {
            combatMovementController.ShowMovementGridPos(currentTurnOwner);
        }
        else
        {
            UpdateActiveCamera(currentTurnOwner, false);
            StatusEffectManager.Instance.ShowUnitAfflictedByStatusEffect(currentTurnOwner);
        }

        if (selectedPlayerUnit && !StatusEffectManager.Instance.IsUnitDisabled(selectedPlayerUnit))
        {
            //Means current Unit is Player
            ControlsManager.Instance.SwitchCurrentActionMap(myActionMap);
        }
        else
        {
            //Active unit is enemy or Player is disabled.
            ControlsManager.Instance.DisableControls();
        }

        //Begin Turn Event & Always Called After Turn Events Played
        currentTurnOwner.BeginTurn?.Invoke();
    }

    public void StartTurnOrPlayEvent()
    {
        //Turn Start EVents
        //Dialogue
        //Fired Up
        //Blessing Effect
        //Enchantment Effects.

        if (turnStartEvents.Count > 0)
        {
            isTurnStartEventPlaying = true;

            ControlsManager.Instance.DisableControls();

            ITurnStartEvent eventToPlay = turnStartEvents[0];
            turnStartEvents.RemoveAt(0);
            eventToPlay.PlayTurnStartEvent();
        }
        else
        {
            //Countinue
            isTurnStartEventPlaying = false;
            TurnStartContinuation();
        }
    }


    public void BeginGoAgainTurn(CharacterGridUnit unit)
    {
        HideAllKOedEnemies();

        ActiveUserChangeSetup(unit, true);

        if (unit is PlayerGridUnit)
        {
            //Means current Unit is Player
            ControlsManager.Instance.SwitchCurrentActionMap(myActionMap);
            ShowActionMenu(true);
        }
        else
        {
            //Active unit is enemy
            ControlsManager.Instance.DisableControls();
            unit.GetComponent<EnemyStateMachine>().FantasyCombatGoAgain();
        }

        unit.Health().Guard(false);
    }

    public void BeginChainAttackAreaSelection(CharacterGridUnit unit, PlayerBaseSkill selectedSkill)
    {
        //Enable Controls
        ControlsManager.Instance.SwitchCurrentActionMap(myActionMap);

        ActiveUserChangeSetup(unit, false);

        combatMovementController.SetMovementAsCurrentPosOnly(unit);

        currentSelectedSkill = selectedSkill;
    }

    public bool SkillSelectedFromList(PlayerBaseSkill newSkill)
    {
        if (newSkill.TrySelectSkill())
        {
            currentSelectedSkill = newSkill;
            return true;
        }

        return false;
    }

    private void ActiveUserChangeSetup(CharacterGridUnit newUnit, bool showMovement)
    {
        gridSelectionMode = false;

        ShowHUD(true);

        UpdateSelectedUnit(newUnit);

        UpdateActiveCamera(newUnit, true);

        if (showMovement)
        {
            combatMovementController.ShowMovementGridPos(newUnit);
        }
    }

    private void HideAllKOedEnemies()
    {
        foreach(CharacterGridUnit enemy in GetEnemyCombatParticipants(true, true).Where((enemy) => enemy.Health().isKOed && enemy.gameObject.activeInHierarchy))
        {
            //Remove From Grid & Deactivate Unit.
            LevelGrid.Instance.RemoveUnitFromGrid(enemy);
            enemy.ActivateUnit(false);
        }
    }

    public void BeginHealthUICountdown()
    {
        if (!isHealthUICooldownRunning)
        {
            StartCoroutine(HealthUIDisplayRoutine(currentSkillFeedbackDisplayTime));
        }
    }

    public void BeginPassiveHealRoutine(CharacterGridUnit unit)
    {
        StartCoroutine(PassiveHealRoutine(unit as PlayerGridUnit));
    }

    IEnumerator HealthUIDisplayRoutine(float waitTime)
    {
        isHealthUICooldownRunning = true;
        yield return new WaitForSeconds(waitTime);
        isHealthUICooldownRunning = false;
        currentSkillFeedbackDisplayTime = 0;
        ActionComplete();
    }

    IEnumerator PassiveHealRoutine(PlayerGridUnit healingPlayer)
    {
        if (healingPlayer && healingPlayer == activeUnit)
        {
            isPassiveHealthUIActive = true;
            ShowActionMenu(false);
        }

        yield return new WaitForSeconds(skillFeedbackDisplayTime);
        isPassiveHealthUIActive = false;

        if (isTurnStartEventPlaying)
        {
            ActionComplete();
        }
        else if (healingPlayer && healingPlayer == activeUnit)
        {
            ShowActionMenu(hud.gameObject.activeInHierarchy);
        }
    }

    private void OnActionComplete()
    {
        if (!inCombat) { return; }

        if (isTurnStartEventPlaying)
        {
            StartTurnOrPlayEvent();
            return;
        }

        StatusEffectManager.Instance.HideTurnEndCam();
        //activeUnit.ReturnToPosAfterAttack(turnEndEvents.Count > 0);
        activeUnit.ReturnToPosAfterAttack(false);

        if (turnEndEvents.Count > 0)
        {
            ResetUnitsToShow();
            ITurnEndEvent eventToPlay = turnEndEvents[0]; 
            turnEndEvents.RemoveAt(0);//Must Be called First to avoid a Stack Overflow when a "PlayTurnEndEvent" adds another Turn End Event to Queue. Otherwise it keeps looping and RemoveAt(0) Never gets called if called afterwards.
            eventToPlay.PlayTurnEndEvent();
        }
        else
        {
            BeginNextTurn();
        }
    }



    public void BattleInterrupted()
    {
        if (!inCombat) { return; }

        AudioManager.Instance.StopMusic();

        //Debug.Log("Battle Interupted");
        EndCombat();

        ControlsManager.Instance.DisableControls(); //Disable controls Again to Activate enemy cinematic mode.

        StopAllCoroutines();

        CombatEnded?.Invoke(BattleResult.Restart, battleTrigger);

        ShowActionMenu(false);

        selectedPlayerUnit = null;
        currentSelectedSkill = null;
        
        //TURN END & START EVENTS CLEARED IN ON BATTLE RESTART.
    }

    public void TacticActivated()
    {
        ControlsManager.Instance.SwitchCurrentActionMap(myActionMap);
        usedTactic = true;
        ShowActionMenu(true);
    }



    private void OnUnitKO(CharacterGridUnit unit)
    {
        //Update Lists.
        turnOrder.RemoveAll((item) => item == unit);
        hud.UpdateTurnOrder(turnOrder, unitCurrentActionTimeDict);

        //Determine if Victory Or Defeat.
        DefeatCheck(unit);
        VictoryCheck(unit);

    }

    private void OnUnitFlee(CharacterGridUnit unit)
    {
        //Update Lists.
        turnOrder.RemoveAll((item) => item == unit);
        hud.UpdateTurnOrder(turnOrder, unitCurrentActionTimeDict);

        ShowActionMenu(false);
        LevelGrid.Instance.RemoveUnitFromGrid(unit);

        playerCombatParticipants.Remove(unit as PlayerGridUnit);
        enemyCombatParticipants.Remove(unit);

        //Determine if All Units KOED Or Fled.
        FleeCheck();
        //VictoryCheck(unit); For Fleeing Enemies
    }

    private void FleeCheck()
    {
        if (GetPlayerCombatParticipants(false, true).Count == 0)
        {
            EndCombat();

            flee.OnLastPlayerFled();
            ActivateCurrentActiveCam(false);
        }
        else
        {
            ActionComplete();
        }
    }

    public void OnUnitRevive(CharacterGridUnit unit)
    {
        //Add Them To End Of Turn Order.
        turnOrder.Add(unit);
        hud.UpdateTurnOrder(turnOrder, unitCurrentActionTimeDict);
    }


    private void VictoryCheck(CharacterGridUnit KOEDUnit)
    {
        if (GetEnemyCombatParticipants(false, true).Count == 0)
        {
            EndCombat();

            currentSkillFeedbackDisplayTime = victoryBlowDisplayTime;
            victory.OnVictory(KOEDUnit, Time.time - battleStartTime);
            ActivateCurrentActiveCam(false);
        }
    }

    private void DefeatCheck(CharacterGridUnit KOEDUnit)
    {
        if (GetPlayerCombatParticipants(false, true).Count == 0)
        {
            EndCombat();

            defeat.OnDefeat(KOEDUnit as PlayerGridUnit);
            ActivateCurrentActiveCam(false);
        }
    }

    private void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        StatusEffectManager.Instance.HideTurnEndCam();

        ActiveUnitObscureCustomPass(false);
        uICustomPass.SetActive(true);

        ClearEventQueues();

        foreach (CharacterGridUnit unit in enemyCombatParticipants)
        {
            LevelGrid.Instance.RemoveUnitFromGrid(unit);
        }

        foreach (CharacterGridUnit unit in playerCombatParticipants)
        {
            LevelGrid.Instance.RemoveUnitFromGrid(unit);
        }
    }



    public void UpdateDamageDataDisplayTime(Affinity affinity, bool isKO, bool isKnockdown, float customExtension = 0)
    {
        float newCalculatedDisplayTime = skillFeedbackDisplayTime + GetExtensionTimeBasedOnAffinity(affinity, isKO, isKnockdown) + customExtension;

        if (newCalculatedDisplayTime > currentSkillFeedbackDisplayTime)
        {
            currentSkillFeedbackDisplayTime = newCalculatedDisplayTime;
        }
    }

    private float GetExtensionTimeBasedOnAffinity(Affinity affinity, bool isKO, bool isKnockdown)
    {
        if (isKnockdown)
        {
            return knockdownDisplayExtension;

        }
        else if (isKO)
        {
            return unitKOedExtension;
        }

        switch (affinity)
        {
            case Affinity.Absorb:
                return resistDisplayExtension;
            case Affinity.Weak:
                return knockdownDisplayExtension;
            case Affinity.Resist:
                return resistDisplayExtension;
            default:
                return 0;
        }
    }

    //TESTING METHOD
    private void GetCombatants()
    {
        int leaderIndex = 0;
        PlayerGridUnit leader = null;
        List<CharacterGridUnit> allFoundCharacterUnits = FindObjectsOfType<CharacterGridUnit>(false).ToList();
        battleTrigger = testBattleTrigger;

        foreach (CharacterGridUnit unit in allFoundCharacterUnits)
        {
            bool isEligible = false;

            if (unit is PlayerGridUnit)
            {
                isEligible = true;
                PlayerGridUnit playerGridUnit = unit as PlayerGridUnit;

                playerCombatParticipants.Add(playerGridUnit);

                if (unit.unitName.ToLower() == PartyData.Instance.GetLeaderName().ToLower())
                {
                    leader = playerGridUnit;
                    leaderIndex = playerCombatParticipants.IndexOf(playerGridUnit);
                }
            }
            else if(unit.gameObject.activeInHierarchy && unit.TryGetComponent(out EnemyStateMachine enemyStateMachine) && !enemyStateMachine.enemyGroup)
            {
                isEligible = true;
                enemyCombatParticipants.Add(unit);
            }

            if (!isEligible) { continue; }

            IntializeUnit(unit);
            unit.SetGridPositions();
            unit.GetComponent<StateMachine>().BeginCombat();
        }

        //Set Leader At Front of list.
        playerCombatParticipants.RemoveAt(leaderIndex);
        playerCombatParticipants.Insert(0, leader);

        EnemyDatabase.Instance.UpdateEnemyCombatants(enemyCombatParticipants);
    }

    //Turn Order Calculations
    private void CalculateTurnOrder(BattleStarter.CombatAdvantage advantageType)
    {
        switch (advantageType)
        {
            case BattleStarter.CombatAdvantage.Neutral:
                CalculateNeutralBattleTurnOrder();
                break;
            case BattleStarter.CombatAdvantage.EnemyAdvantage:
                CalculateAdvantageTurnOrder(false);
                break;
            case BattleStarter.CombatAdvantage.PlayerAdvantage:
                CalculateAdvantageTurnOrder(true);
                break;
        }
    }

    private void CalculateNeutralBattleTurnOrder()
    {
        //Randomize the active Combat Units so it's random for Units with Same Speed.
        System.Random rng = new System.Random();

        //Once Combat begins, Calculate turn order based on highest unit speed.
        turnOrder = allCharacterCombatUnits.OrderBy(a => rng.Next()).OrderByDescending((unit) => unit.stats.Speed).ToList();

        //Calculate Current AT for each Unit.
        foreach (CharacterGridUnit unit in allCharacterCombatUnits)
        {
            float currentAT = CalculateUnitActionTime(unit.stats.Speed);
            unitCurrentActionTimeDict[unit] = currentAT;
        }
        
        hud.UpdateTurnOrder(turnOrder, unitCurrentActionTimeDict);
    }

    private void CalculateAdvantageTurnOrder(bool isPlayerAdvantage)
    {
        //Randomize the active Combat Units so it's random for Units with Same Speed.
        System.Random rng = new System.Random();

        //Once Combat begins, Calculate turn order based on highest unit speed.
        List<CharacterGridUnit> playerConversionList = new List<CharacterGridUnit>(playerCombatParticipants);
        List<CharacterGridUnit> playersOrderedBySpeed = playerConversionList.OrderBy(a => rng.Next()).OrderByDescending((unit) => unit.stats.Speed).ToList();
        List<CharacterGridUnit> enemiesOrderedBySpeed = enemyCombatParticipants.OrderBy(a => rng.Next()).OrderByDescending((unit) => unit.stats.Speed).ToList();

        if (isPlayerAdvantage)
        {
            turnOrder = playersOrderedBySpeed.Concat(enemiesOrderedBySpeed).ToList();
        }
        else
        {
            turnOrder = enemiesOrderedBySpeed.Concat(playersOrderedBySpeed).ToList();
        }
        
        //Calculate Current AT for each Unit.
        foreach (CharacterGridUnit unit in allCharacterCombatUnits)
        {
            float currentAT = CalculateUnitActionTime(unit.stats.Speed);
            unitCurrentActionTimeDict[unit] = currentAT;
        }

        hud.UpdateTurnOrder(turnOrder, unitCurrentActionTimeDict);
    }

    private void CalculateActiveUnitsNextTurn()
    {
        //Subtract constant from each unit's AT. 
        for (int i = 0; i < unitCurrentActionTimeDict.Count; i++)
        {
            KeyValuePair<CharacterGridUnit, float> currentDictItem = unitCurrentActionTimeDict.ElementAt(i);

            float currentAT = unitCurrentActionTimeDict.ElementAt(i).Value;
            unitCurrentActionTimeDict[currentDictItem.Key] = DeductUnitActionTime(currentAT);

            //Debug.Log(currentDictItem.Key.unitName + " Action Time: " + unitCurrentActionTimeDict[currentDictItem.Key]);
        }

        //Reset Current Active Unit's action time
        CharacterGridUnit currentActiveUnit = turnOrder[0];
        InsertUnitInTurnOrder(currentActiveUnit);
    }

    private void InsertUnitInTurnOrder(CharacterGridUnit unitToInsert)
    {
        unitCurrentActionTimeDict[unitToInsert] = CalculateUnitActionTime(unitToInsert.stats.Speed);

        int indexToInsertAt = -1;
        float currentActiveUnitAT = unitCurrentActionTimeDict[unitToInsert];

        //Debug.Log("Acting Unit " + unitToInsert.unitName + " Action Time: " + currentActiveUnitAT);

        //Loop Through and get First Index of Unit where Current's Unit AT is lower
        for (int i = 1; i < turnOrder.Count; i++)
        {
            float unitAT = unitCurrentActionTimeDict[turnOrder[i]];

            if (currentActiveUnitAT < unitAT)
            {
                indexToInsertAt = i;
                break;
            }
            else if (currentActiveUnitAT == unitAT && unitToInsert.stats.Speed > turnOrder[i].stats.Speed)
            {
                //Unit With Higher Speed is favoured
                indexToInsertAt = i;
                break;
            }
        }

        if (indexToInsertAt < 0)
        {
            //Add to end of List
            turnOrder.Add(unitToInsert);
        }
        else
        {
            //Insert.
            turnOrder.Insert(indexToInsertAt, unitToInsert);
        }

        hud.UpdateTurnOrder(turnOrder, unitCurrentActionTimeDict);
    }


    private float CalculateUnitActionTime(int unitSpeed)
    {
        if (!speedToActionTimeDict.ContainsKey(unitSpeed))
        {
            AddActionTimeToDict(unitSpeed);
        }

        return speedToActionTimeDict[unitSpeed];
    }

    private float DeductUnitActionTime(float currentAT)
    {
        return Mathf.Max(0, currentAT - actionTimeSubtractionConstant);
    }

    private void SetActionTimes()
    {
        speedToActionTimeDict[1] = baseActionTime;

        for (int speed = 2; speed <= 100; speed++)
        {
            AddActionTimeToDict(speed);
        }
    }

    private void AddActionTimeToDict(int speed)
    {
        float increment = maxActionTimeIncrement - Mathf.FloorToInt(speed / NSpeedTurnToDecreaseIncrement);
        increment = Mathf.Max(increment, 1);

        int prevSpeed = speed - 1;
        float AT;

        if (speedToActionTimeDict.ContainsKey(prevSpeed))
        {
            AT = speedToActionTimeDict[prevSpeed] - increment;       
        }
        else
        {
            int speedWithMinIncrement = NSpeedTurnToDecreaseIncrement * speed; //50
            float ATWithMinIncrement = speedToActionTimeDict[speedWithMinIncrement]; //659

            AT = ATWithMinIncrement - ((speed - speedWithMinIncrement) * 1); //Min Increment is 1
        }

        speedToActionTimeDict[speed] = AT;
        //Debug.Log("Speed: " + speed + "; Increment: " + increment + "; AT: " + AT);
    }


    //Quick Actions

    private void OnAttackOrActivateSkill(InputAction.CallbackContext context)
    {
        if (context.action.name != "Attack") { return; }

        if (context.performed && selectedPlayerUnit)
        {
            if (!currentSelectedSkill)
            {
                if (selectedPlayerUnit.GetBasicAttack().TrySelectSkill())
                {
                    //Play SFX
                    AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

                    ShowActionMenu(false);
                    currentSelectedSkill = selectedPlayerUnit.GetBasicAttack();
                }
                else
                {
                    //Play SFX
                    AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
                } 
            }
            else if(!currentSelectedSkillTriggered)
            {
                currentSelectedSkillTriggered = currentSelectedSkill.TryTriggerSkill();

                if (!currentSelectedSkillTriggered) //Means Skill Failed To Trigger
                    AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            }
        }
    }



    private void OnSkill(InputAction.CallbackContext context)
    {
        if (context.action.name != "Skill") { return; }

        if (context.performed && selectedPlayerUnit && !currentSelectedSkill)
        {
            //Play SFX
            AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

            collectionManager.OpenSkillMenu(selectedPlayerUnit);
        }
    }

    private void OnItem(InputAction.CallbackContext context)
    {
        if (context.action.name != "Item") { return; }

        if (context.performed && selectedPlayerUnit && !currentSelectedSkill)
        {
            //Play SFX
            AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

            collectionManager.OpenItemMenu(selectedPlayerUnit, true);
        }
    }

    private void OnTactic(InputAction.CallbackContext context)
    {
        if (context.action.name != "Tactic") { return; }

        if (usedTactic)
        {
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            return;
        }

        if (context.performed && selectedPlayerUnit && !currentSelectedSkill)
        {
            //Play SFX
            AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

            collectionManager.OpenTacticsMenu(selectedPlayerUnit);
        }
    }

    private void OnAnalyse(InputAction.CallbackContext context)
    {
        if (context.action.name != "Analyse") { return; }

        if (context.performed && selectedPlayerUnit)
        {
            EnemyDatabase.Instance.ShowAnalysisUI(true);
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (context.action.name != "Interact") { return; }

        if (context.performed && selectedPlayerUnit)
        {
            if (InteractionManager.Instance.HandleInteraction(true))
            {
                //Fantasy Combat Manager updates activeUnit Grid Pos and Rotation. 
            }
        }
    }

    private void OnFlee(InputAction.CallbackContext context)
    {
        if (context.action.name != "Flee") { return; }

        if (context.performed && selectedPlayerUnit && !currentSelectedSkill)
        {
            flee.TryFlee(selectedPlayerUnit);
        }
    }

    private void OnToggleGridSelection(InputAction.CallbackContext context)
    {
        //PC ONLY INPUT
        if (context.action.name != "ToggleGridSelect" || !ControlsManager.Instance.IsCurrentDeviceKeyboardMouse()) { return; }

        if (context.performed && selectedPlayerUnit && currentSelectedSkill && currentSelectedSkill.RequiresGridSelection()) 
        {
            gridSelectionMode = true;
        }
        else if (context.canceled && selectedPlayerUnit)
        {
            gridSelectionMode = false;
        }
    }

    private void OnGridSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "GridSelect") { return; }

        if (context.performed && currentSelectedSkill  && CanGridSelect())
        {
            currentSelectedSkill.UpdateGridSelection(context.ReadValue<Vector2>(), mainCam.transform);
        }
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        if (context.action.name != "Pause") { return; }

        if (context.performed)
        {
            GameManager.Instance.PauseGame(true);
        }
    }

    /*private void OnGuard(InputAction.CallbackContext context)
    {
        if (context.action.name != "Guard") { return; }

        if (context.performed && selectedPlayerUnit && !currentSelectedSkill)
        {
            //Also Ensure Menu is active.
            if (selectedPlayerUnit.Guard().TrySelectSkill())
            {
                //Play SFX
                AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

                ShowActionMenu(false);
                currentSelectedSkill = selectedPlayerUnit.Guard();
            }
            else
            {
                //Play SFX
                AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            }
        }
    }*/


    private void OnGuardOrCancel(InputAction.CallbackContext context) //Joined because Guard & Cancel same button on controller.
    {
        if (context.action.name == "Cancel" && !ControlsManager.Instance.IsCurrentDeviceKeyboardMouse()) { return; }

        if (context.performed && selectedPlayerUnit && (context.action.name == "Cancel" || context.action.name == "Guard"))
        {
            if (currentSelectedSkill)
            {
                PlayerBaseSkill previousSkill = currentSelectedSkill;
                hud.SelectedSkill("");

                currentSelectedSkill = null;
                previousSkill.SkillCancelled();
            }
            else
            {
                //Also Ensure Menu is active.
                if (selectedPlayerUnit.Guard().TrySelectSkill())
                {
                    //Play SFX
                    AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

                    ShowActionMenu(false);
                    currentSelectedSkill = selectedPlayerUnit.Guard();
                }
                else
                {
                    //Play SFX
                    AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
                }
            }
        }
    }

    private void OnTest(InputAction.CallbackContext context)
    {
        if (context.action.name != "Test") { return; }

        if (context.performed)
        {
            Debug.Log("TEST CALLED");
        }
    }

    private void OnDisable()
    {
        ActionComplete -= OnActionComplete;
        FantasyHealth.CharacterUnitKOed -= OnUnitKO;
        FantasyHealth.CharacterUnitRevived -= OnUnitRevive;
        CombatEnded -= OnCombatEnd;
        Flee.UnitFled -= OnUnitFlee;
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            playerInput.onActionTriggered += OnAttackOrActivateSkill;
            playerInput.onActionTriggered += OnGuardOrCancel;
            playerInput.onActionTriggered += OnPause;
            playerInput.onActionTriggered += OnTest;
            //playerInput.onActionTriggered += OnGuard;
            playerInput.onActionTriggered += OnInteract;
            playerInput.onActionTriggered += OnAnalyse;
            playerInput.onActionTriggered += OnItem;
            playerInput.onActionTriggered += OnSkill;
            playerInput.onActionTriggered += OnGridSelect;
            playerInput.onActionTriggered += OnToggleGridSelection;
            playerInput.onActionTriggered += OnTactic;
            playerInput.onActionTriggered += OnFlee;
        }
        else
        {
            playerInput.onActionTriggered -= OnAttackOrActivateSkill;
            playerInput.onActionTriggered -= OnPause;
            playerInput.onActionTriggered -= OnGuardOrCancel;
            playerInput.onActionTriggered -= OnTest;
            //playerInput.onActionTriggered -= OnGuard;
            playerInput.onActionTriggered -= OnInteract;
            playerInput.onActionTriggered -= OnAnalyse;
            playerInput.onActionTriggered -= OnSkill;
            playerInput.onActionTriggered -= OnItem;
            playerInput.onActionTriggered -= OnGridSelect;
            playerInput.onActionTriggered -= OnToggleGridSelection;
            playerInput.onActionTriggered -= OnTactic;
            playerInput.onActionTriggered -= OnFlee;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }
    private bool CanMovePlayer()
    {
        if (ControlsManager.Instance.IsCurrentDeviceKeyboardMouse())
        {
            //Using Keyboard, Only Move when Grid Selection mode Disabled
            return !gridSelectionMode;
        }

        return true;
    }


    private bool CanGridSelect()
    {
        if (ControlsManager.Instance.IsCurrentDeviceKeyboardMouse())
        {
            return selectedPlayerUnit && gridSelectionMode;
        }

        return selectedPlayerUnit;
    }

    //Doers
    private void UpdateSelectedUnit(CharacterGridUnit newUnit)//Method called By BeginGoAgainTurn & OnTurnStart.
    {
        //Deactive Cam First.
        if (activeUnit)
        {
            UpdateActiveCamera(activeUnit, false);
        }
        
        //Update Units
        currentTurnOwner = turnOrder[0]; //The Owner of the current turn
        activeUnit = newUnit; //Which Unit is Acting...Due to Again Event. It's possible for Units to act on someone else's turn.
        selectedPlayerUnit = activeUnit as PlayerGridUnit;

        //Reset Skills
        currentSelectedSkill = null;
        currentSelectedSkillTriggered = false;

        selectedPlayerUnit?.ResetOrbitCam();

        if (selectedPlayerUnit)
            collectionManager.CleanUI();

        ResetUnitsToShow();
    }

    //TURN EVENTS
    public void AddTurnStartEventToQueue(ITurnStartEvent turnStartEvent)
    {
        turnStartEvents.Add(turnStartEvent);
        //Order Events By Priority
        turnStartEvents = turnStartEvents.OrderBy((startEvent) => startEvent.turnStartEventOrder).ToList();
    }

    public bool AddTurnEndEventToQueue(ITurnEndEvent turnEndEvent)
    {
        foreach (ITurnEndEvent endEvent in turnEndEvents)
        {
            Type eventType = endEvent.GetType();

            if (turnEndEvent.GetEventTypesThatCancelThis().Contains(eventType))
            {
                return false;
            }
        }

        for (int i = turnEndEvents.Count - 1; i >= 0; i--)
        {
            //Remove all events that are cancelled by the newly added event. 
            Type eventType = turnEndEvent.GetType();

            if (turnEndEvents[i].GetEventTypesThatCancelThis().Contains(eventType))
            {
                turnEndEvents[i].OnEventCancelled();
                turnEndEvents.RemoveAt(i);
            }
        }

        turnEndEvents.Add(turnEndEvent);

        //Order Events By Priority
        turnEndEvents = turnEndEvents.OrderBy((endEvent) => endEvent.turnEndEventOrder).ToList();
        return true;
    }

    public void CancelTurnEndEvent(ITurnEndEvent turnEndEvent)
    {
        if (turnEndEvents.Remove(turnEndEvent))
        {
            Debug.Log("TURN END EVENT CANCELLED: " + turnEndEvent.GetType().Name);
        }
    }

    private void EndCombat()
    {
        inCombat = false;
        currentTurnOwner.EndTurn?.Invoke();
    }

    private void ClearEventQueues()
    {
        turnStartEvents.Clear();

        //Cancel All Turn End Events
        for (int i = turnEndEvents.Count - 1; i >= 0; i--)
        { 
            turnEndEvents[i].OnEventCancelled();
            turnEndEvents.RemoveAt(i);
        }
    }

    public void ShowActionMenu(bool show)
    {
        if(selectedPlayerUnit && !currentSelectedSkill)
            selectedPlayerUnit.ShowActionMenu(show && !isPassiveHealthUIActive, usedTactic);
    }

    //Setters
    private void SetAllActiveCombatUnits()
    {
        allCharacterCombatUnits.Clear();
        allCharacterCombatUnits = playerCombatParticipants.Concat(enemyCombatParticipants).ToList();
    }
    private void UpdateActiveCamera(CharacterGridUnit newUnit, bool activate)
    {
        newUnit.ActivateFollowCam(activate);

        PlayerGridUnit newPlayer = newUnit as PlayerGridUnit;

        if (newPlayer && !activate)
            newPlayer.SetFollowCamInheritPosition(!activate);
    }

    public void ActivateCurrentActiveCam(bool activate)
    {
        if(inCombat)
            activeUnit.ActivateFollowCam(activate);
    }

    public void ActivatePhotoshootSet(bool activate)
    {
        photoshootSet.SetActive(activate);
    }

    public void SetPostCombatWarpPoint(Transform point)
    {
        if (!postCombatWarpPoint)
            postCombatWarpPoint = point;
    }

    public void WarpPlayerToPostCombatPos(PlayerStateMachine playerStateMachine)
    {
        if (postCombatWarpPoint)
        {
            //Warp Player to Combat Warp Point.
            playerStateMachine.WarpPlayer(postCombatWarpPoint.position, postCombatWarpPoint.rotation, PlayerStateMachine.PlayerState.FantasyRoam, true);
            postCombatWarpPoint = null; //Set To Null
            return;
        }

        Transform leaderTransform = playerStateMachine.transform;

        //Warp Player to pos current Grid Pos Rounded, so that spawned loot is spawned on same grid pos which is guaranteed to have no obstacle.
        Vector3 leaderCurrentPosition = LevelGrid.Instance.gridSystem.RoundWorldPositionToGridPosition(leaderTransform.position);
        playerStateMachine.WarpPlayer(leaderCurrentPosition, leaderTransform.rotation, PlayerStateMachine.PlayerState.FantasyRoam, true);
    }

    public void SetLootPos(GameObject loot)
    {
        loot.transform.position = victory.GetLootPos();
    }

    public void SetUnitsToShow(List<GridUnit> unitList)
    {
        unitsToShow.Clear();

        foreach (GridUnit unit in unitList)
        {
            if (!unitsToShow.Contains(unit))
            {
                unitsToShow.Add(unit);
            } 
        }
    }

    public void ResetUnitsToShow()
    {
        unitsToShow.Clear();
        unitsToShow.Add(activeUnit);
    }

    //Getters
    public FantasyCombatMovement GetFantasyCombatMovement()
    {
        return combatMovementController;
    }

    public float GetSkillFeedbackDisplayTime()
    {
        return currentSkillFeedbackDisplayTime;
    }

    public float GetUnextendedSkillFeedbackDisplayTime()
    {
        return skillFeedbackDisplayTime;
    }

    public bool IsTurnEndEventFirstInQueue(ITurnEndEvent turnEndEvent)
    {
        return turnEndEvents.Count > 0 && turnEndEvents[0].GetType().Name == turnEndEvent.GetType().Name;
    }

    public bool IsUnitInBattle(CharacterGridUnit unit)
    {
        return allCharacterCombatUnits.Contains(unit);
    }

    public float GetKODisplayTime()
    {
        return unitKOedExtension;
    }

    public void RetryBattle()
    {
        defeat.Retry();
    }

    public List<CharacterGridUnit> GetAllCharacterCombatUnits(bool incluedKOEDUnits)
    {
        if (incluedKOEDUnits)
        {
            return allCharacterCombatUnits;
        }

        return allCharacterCombatUnits.Where((unit) => !unit.Health().isKOed).ToList();
    }

    public List<PlayerGridUnit> GetPlayerCombatParticipants(bool includeKOEDUnits, bool includeDisabledUnits)
    {
        List<PlayerGridUnit> listToReturn = new List<PlayerGridUnit>(playerCombatParticipants);

        if (!includeKOEDUnits)
        {
            listToReturn = listToReturn.Where((unit) => !unit.Health().isKOed).ToList();
        }

        if (!includeDisabledUnits)
        {
            listToReturn = listToReturn.Where((unit) => !StatusEffectManager.Instance.IsUnitDisabled(unit)).ToList();
        }
        PlayerGridUnit leader = PartyData.Instance.GetLeader();

        if (listToReturn.Contains(leader))
        {
            listToReturn.Remove(leader);
            listToReturn.Insert(0, leader);
        }
        return listToReturn;
    }

    public List<CharacterGridUnit> GetEnemyCombatParticipants(bool includeKOEDUnits, bool includeDisabledUnits)
    {
        List<CharacterGridUnit> listToReturn = new List<CharacterGridUnit>(enemyCombatParticipants);

        if (!includeKOEDUnits)
        {
            listToReturn = listToReturn.Where((unit) => !unit.Health().isKOed).ToList();
        }

        if (!includeDisabledUnits)
        {
            listToReturn = listToReturn.Where((unit) => !StatusEffectManager.Instance.IsUnitDisabled(unit)).ToList();
        }

        return listToReturn;
    }

    public CharacterGridUnit GetActiveUnit()
    {
        return activeUnit;
    }

    public CharacterGridUnit GetCurrentTurnOwner()
    {
        return currentTurnOwner;
    }


    public List<GridUnit> GetUnitsToShow()
    {
        return unitsToShow;

    }

    public void ShowHUD(bool show, bool showWorldSpaceUI = true)
    {
        hud.gameObject.SetActive(show);
        //cullWorldSpaceUI.enabled = !showWorldSpaceUI;
        uICustomPass.SetActive(showWorldSpaceUI);
    }

    public void ActiveUnitObscureCustomPass(bool show)
    {
        unitObscuredCustomPass.SetActive(show);
    }


    public bool InCombat()
    {
        return inCombat;
    }

    public void ShowAllUnits()
    {
        foreach (CharacterGridUnit unit in GetAllCharacterCombatUnits(false))
        {
            unit.unitAnimator.ShowModel(true);
        }
    }
}
