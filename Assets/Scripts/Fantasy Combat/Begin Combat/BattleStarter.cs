using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using DG.Tweening;
using Cinemachine;
using AnotherRealm;
using MoreMountains.Feedbacks;

public enum BattleType
{
    Normal,
    Story,
    MonsterChest,
    Trial
}

public class BattleStarter : MonoBehaviour
{
    public static BattleStarter Instance { get; private set; }
    [Header("Values")]
    [SerializeField] float companionsMoveOutOfShotTime = 0.25f;
    [SerializeField] float companionsMoveOutOfShotDistance = 1;
    [Header("Leader Neutral Attack Data")]
    [SerializeField] float attackOffset = 1.8f;
    [Header("Timers")]
    [SerializeField] float unitMoveToGridPosTime = 0.25f;
    [SerializeField] float unitJumpPower = 2;
    [Space(10)]
    [SerializeField] float moveToGridPosDelay = 0.25f;
    [SerializeField] float canvasDisplayTime = 0.5f;
    [Space(10)]
    [SerializeField] float playerAmbushMoveTime = 0.25f;
    [SerializeField] float playerAmbushRotateTime = 0.15f;
    [Header("Delays")]
    [SerializeField] float playerAdvantageCanvasDelay = 0.15f;
    [SerializeField] float enemyAdvanatgeCanvasDelay = 0.5f;
    [SerializeField] float neutralCanvasDelay = 0;
    [Space(10)]
    [SerializeField] float playerAdvantageBattleDelay = 0.25f;
    [SerializeField] float enemyAdvantageBattleDelay = 0.25f;
    [SerializeField] float neutralBattleDelay = 0;
    [Header("GameObjects")]
    [SerializeField] GameObject playerAdvantageCam;
    [SerializeField] GameObject enemyAdvantageCam;
    [SerializeField] GameObject neutralBattleCam;
    [Space(10)]
    [SerializeField] CinemachineTargetGroup ambushTargetGroup;
    [Space(10)]
    [SerializeField] GameObject playerHitVolume;
    [Header("UI")]
    [SerializeField] GameObject playerAdvantageCanvas;
    [SerializeField] GameObject enemyAdvantageCanvas;
    [SerializeField] GameObject neutralBattleCanvas;
    [Header("Feedback")]
    [SerializeField] MMF_Player enemyAmbushPlayerFeedback;

    
    public enum CombatAdvantage
    {
        Neutral,
        PlayerAdvantage,
        EnemyAdvantage
    }


    //Cache
    PlayerGridUnit leader;
    EnemyStateMachine contactedEnemy;
    CombatAdvantage advantageType;
    CinemachineImpulseSource impulseSource;
    PlayerStateMachine playerStateMachine;

    GameObject currentCanvas;
    GameObject currentCam;

    //Important
    GridPosition centreGridPos;
    Direction combatDirection; //The players forward direction at start of Battle
    Vector3 combatDirectionAsVector;  //Vector version of Combat Direction.

    List<CharacterGridUnit> assignedEnemies = new List<CharacterGridUnit>();

    //Generated Formation
    int expandIncrement = 1;
    List<GridPosition> availableEnemyGridPositions = new List<GridPosition>();
    List<GridPosition> takenGridPositions = new List<GridPosition>();
    

    //Setup Variables
    GridUnitAnimator attackerAnimator;
    IBattleTrigger battleTrigger;
    EnemyGroup enemyGroup;

    float activateCanvasDelay;
    float beginBattleDelay;

    bool combatInitiated = false;
    bool targetHit = false;

    //Events
    public Action<EnemyStateMachine> PlayerStartCombatAttackComplete;
    public Action TargetHit;
 

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void OnEnable()
    {
        TargetHit += OnTargetHit;
    }

    private void SetPlayerData()
    {
        leader = PartyManager.Instance.GetLeader();
        playerStateMachine = PlayerSpawnerManager.Instance.GetPlayerStateMachine();

    }

    private void Setup(GridUnitAnimator attacker, IBattleTrigger battleTrigger, float canvasDelay, float battleDelay)
    {
        ControlsManager.Instance.DisableControls();

        combatInitiated = true;

        this.battleTrigger = battleTrigger;

        //FantasyCombatManager.Instance.ShowHUD(false);
        HUDManager.Instance.HideHUDs();

        PlayerStartCombatAttackComplete += CombatAttackComplete;

        attackerAnimator = attacker;
        activateCanvasDelay = canvasDelay;
        beginBattleDelay = battleDelay;
    }

    public void PlayerAdvantageTriggered(EnemyStateMachine attackedEnemy, Transform ambushDestination, IBattleTrigger battleTrigger)
    {
        if (HasCombatIntiated()) { return; }

        SetPlayerData();
        Setup(leader.unitAnimator, battleTrigger, playerAdvantageCanvasDelay, playerAdvantageBattleDelay);

        EnlistCombatants(attackedEnemy, CombatAdvantage.PlayerAdvantage);
        
        MoveCompanionsOutOfShot();

        //Trigger Anim & Feedback.
        leader.unitAnimator.Ambush();
        playerStateMachine.ambushEnemyFeedback?.PlayFeedbacks();

        float angleCheck = Vector3.Angle(leader.transform.forward.normalized, ambushDestination.transform.forward.normalized);

        if(angleCheck > 90)
        {
            //Warp
            leader.Warp(ambushDestination.position, ambushDestination.rotation);
        }
        else
        {
            //Move
            leader.transform.DOMove(ambushDestination.position, playerAmbushMoveTime);
            leader.transform.DORotate(ambushDestination.rotation.eulerAngles, playerAmbushRotateTime);
        }
        
        ActivateAdvantageCam(playerAdvantageCam, true);
    }

    public void EnemyAdvantageTriggered(EnemyStateMachine enemyWhoTriggeredCombat, IBattleTrigger battleTrigger)
    {
        if(HasCombatIntiated()) { return; }

        SetPlayerData();
        Setup(enemyWhoTriggeredCombat.animator, battleTrigger, enemyAdvanatgeCanvasDelay, enemyAdvantageBattleDelay);

        EnlistCombatants(enemyWhoTriggeredCombat, CombatAdvantage.EnemyAdvantage);

        MoveCompanionsOutOfShot();
        OnTargetHit();

        MoveAmbushedUnit(true);

        ActivateAdvantageCam(enemyAdvantageCam, true);
    }

    public void NeutralBattleTriggered(EnemyStateMachine attackedEnemy, IBattleTrigger battleTrigger)
    {
        if (HasCombatIntiated()) { return; }

        SetPlayerData();
        Setup(leader.unitAnimator, battleTrigger, neutralCanvasDelay, neutralBattleDelay);

        EnlistCombatants(attackedEnemy, CombatAdvantage.Neutral);

        MoveCompanionsOutOfShot();

        //Trigger Anim & Feedback.
        leader.unitAnimator.AttackIntruder();
        playerStateMachine.neutralAttackFeedback?.PlayFeedbacks();

        Vector3 direction = (attackedEnemy.transform.position - leader.transform.position).normalized;
        leader.transform.DOMove(attackedEnemy.transform.position - direction * attackOffset, playerAmbushMoveTime);

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        leader.transform.DORotate(lookRotation.eulerAngles, playerAmbushRotateTime);

        ActivateAdvantageCam(neutralBattleCam, true);
    }

    public void StoryBattleTriggered(EnemyGroup enemyGroup, CombatAdvantage advantage, Transform centreGridPos, IBattleTrigger battleTrigger)
    {
        this.battleTrigger = battleTrigger;
        contactedEnemy = null;

        //Set Centre Grid Position
        this.centreGridPos = LevelGrid.Instance.gridSystem.GetGridPosition(centreGridPos.position);

        //Enlist the Combatants
        EnlistCombatants(enemyGroup, advantage, false);

        //Move Combatants
        MoveCombatantsToGridPos(true, centreGridPos);
    }

    public void StartBattle()
    {
        if (battleTrigger.battleType != BattleType.Story)
            PlayerStartCombatAttackComplete -= CombatAttackComplete;

        FantasyCombatManager.Instance.BeginCombat(advantageType, battleTrigger, PartyManager.Instance.GetActivePlayerParty(), assignedEnemies);
        playerHitVolume.SetActive(false);

        combatInitiated = false;
        targetHit = false;
    }


    private void GenerateAvailableGridPositions(bool expand)
    {
        //Players Grab their positions First
        //Contacted Enemy is always first in assignedEnemyList, therefore gets their position first.

        if (battleTrigger.battleType != BattleType.Normal) { return; } //Behaviour only occurs if Battle Type is Normal.

        Vector2 formationArea;

        if (expand)
        {
            //Means Available Grid Pos is empty
            formationArea = new Vector2(enemyGroup.formationWidth + expandIncrement, enemyGroup.formationLength + expandIncrement);
            availableEnemyGridPositions = CombatFunctions.GetValidGridPositionsBasedOnDirection(centreGridPos, formationArea, combatDirection);

            expandIncrement++;
        }
        else
        {            
            expandIncrement = 1;
            availableEnemyGridPositions.Clear();

            formationArea = new Vector2(enemyGroup.formationWidth, enemyGroup.formationLength);  
        }

        //Generate Grid
        availableEnemyGridPositions = CombatFunctions.GetValidGridPositionsBasedOnDirection(centreGridPos, formationArea, combatDirection);

        //Remove Taken Grid Positions from generated Grid.
        availableEnemyGridPositions = availableEnemyGridPositions.Except(takenGridPositions).ToList();
    }

    private void OnTargetHit()
    {
        Debug.Log("Calling Battle Starter Target Hit");
        if (targetHit) { return; }

        targetHit = true;

        impulseSource.GenerateImpulse();
        centreGridPos = LevelGrid.Instance.gridSystem.GetGridPosition(leader.transform.position);

        switch (advantageType)
        {
            case CombatAdvantage.Neutral:
                currentCanvas = neutralBattleCanvas;
                contactedEnemy.SwitchState(contactedEnemy.emptyState);
                contactedEnemy.myGridUnit.unitAnimator.Hit();
                break;
            case CombatAdvantage.EnemyAdvantage:
                enemyAmbushPlayerFeedback?.PlayFeedbacks();
                playerHitVolume.SetActive(true);
                currentCanvas = enemyAdvantageCanvas;
                break;
            case CombatAdvantage.PlayerAdvantage:
                currentCanvas = playerAdvantageCanvas;
                contactedEnemy.myGridUnit.unitAnimator.SetTrigger(contactedEnemy.myGridUnit.unitAnimator.animIDAmbushed);
                MoveAmbushedUnit(false);
                break;
        }

        StartCoroutine(DisplayCanvasRoutine());
    }

    IEnumerator DisplayCanvasRoutine()
    {
        yield return new WaitForSeconds(activateCanvasDelay);
        currentCanvas.SetActive(true);
        yield return new WaitForSeconds(canvasDisplayTime);
        currentCanvas.SetActive(false);
    }

    private void CombatAttackComplete(EnemyStateMachine enemyStateMachine)
    {
        if(advantageType != CombatAdvantage.EnemyAdvantage && enemyStateMachine) { return; }
        if(advantageType == CombatAdvantage.EnemyAdvantage && enemyStateMachine && enemyStateMachine != contactedEnemy) { return; }

        StartCoroutine(MoveCombatantsRoutine());
    }

    IEnumerator MoveCombatantsRoutine()
    {
        attackerAnimator.ActivateSlowmo();
        yield return new WaitForSeconds(moveToGridPosDelay);
        attackerAnimator.ReturnToNormalSpeed();
        MoveCombatantsToGridPos(false);
        yield return new WaitForSeconds(beginBattleDelay + unitMoveToGridPosTime);
        currentCam.SetActive(false);
        StartBattle();
    }

    private void OnDisable()
    {
        PlayerStartCombatAttackComplete -= CombatAttackComplete;
        TargetHit -= OnTargetHit;
    }


    private void EnlistCombatants(EnemyStateMachine contactedEnemy, CombatAdvantage combatAdvantage)
    {
        this.contactedEnemy = contactedEnemy;

        EnlistCombatants(contactedEnemy.enemyGroup, combatAdvantage);

        //Move Units to Positions & activate them.

    }

    private void EnlistCombatants(EnemyGroup enemyGroup, CombatAdvantage combatAdvantage, bool activateUnits = true)
    {
        this.enemyGroup = enemyGroup;

        advantageType = combatAdvantage;

        assignedEnemies.Clear();

        List<EnemyStateMachine> iterableList = enemyGroup.linkedEnemies.Concat(EnemyTacticsManager.Instance.GetAllChasingEnemies()).Distinct().ToList();

        if(contactedEnemy)
            iterableList.Insert(0, contactedEnemy); //Add Contacted Enemy to front of list.

        

        foreach (EnemyStateMachine enemy in iterableList)
        {
            CharacterGridUnit enemyGridUnit = enemy.GetComponent<CharacterGridUnit>();

            if (!assignedEnemies.Contains(enemyGridUnit) && !enemyGridUnit.Health().isKOed)
            {
                //Activate Enemy
                if (activateUnits)
                    enemyGridUnit.ActivateUnit(true);

                //Switch To Empty State.
                enemy.SwitchState(enemy.emptyState);

                //Intalize Enemy
                assignedEnemies.Add(enemyGridUnit);
                FantasyCombatManager.Instance.IntializeUnit(enemyGridUnit);
            }
        }

        foreach (PlayerGridUnit player in PartyManager.Instance.GetActivePlayerParty())
        {
            FantasyCombatManager.Instance.IntializeUnit(player);
        }

        if(contactedEnemy)
            UpdateTargetGroup(ambushTargetGroup);

        //Raise Event
        FantasyCombatManager.Instance.BattleTriggered?.Invoke(advantageType);
    }


    private void MoveAmbushedUnit(bool isPlayer)
    {
        CharacterGridUnit hitUnit = isPlayer ? leader : contactedEnemy.myGridUnit;

        contactedEnemy.SwitchState(contactedEnemy.emptyState);

        StatusEffectManager.Instance.UnitKnockedDown(hitUnit);
        hitUnit.unitAnimator.SetBool(hitUnit.unitAnimator.animIDKnockdown, true);

        Vector3 forwardDirection = CombatFunctions.GetCardinalDirectionAsVector(contactedEnemy.transform);
        GridPosition desiredGridPos = new GridPosition(centreGridPos.x + (int)forwardDirection.x, centreGridPos.z + (int)forwardDirection.z);

        Quaternion lookRotation = Quaternion.LookRotation(CombatFunctions.GetCardinalDirectionAsVector(hitUnit.transform));
        hitUnit.transform.DORotate(lookRotation.eulerAngles, playerAmbushRotateTime);

        GridPosition takenPos = FindSuitableBattleStartGridPos(desiredGridPos);

        Vector3 worldPos = LevelGrid.Instance.gridSystem.GetWorldPosition(takenPos);
        hitUnit.transform.DOMove(worldPos, unitMoveToGridPosTime).OnComplete(() => ReadyUnitForCombat(hitUnit));
    }

    private void SetCombatDirection(Transform customDirection)
    {
        Transform transformToCheck;
        int multiplier = 1;

        if (customDirection)
        {
            transformToCheck = customDirection;
        }
        else
        {
            transformToCheck = advantageType == CombatAdvantage.EnemyAdvantage ? contactedEnemy.transform : leader.transform;
            multiplier = advantageType == CombatAdvantage.EnemyAdvantage ? -1 : 1;
        }

        combatDirectionAsVector = CombatFunctions.GetCardinalDirectionAsVector(transformToCheck) * multiplier;
        combatDirection = CombatFunctions.GetCardinalDirection(combatDirectionAsVector);
    }


    private void MoveCombatantsToGridPos(bool warpUnits, Transform customBattleDirection = null)
    {
        takenGridPositions.Clear();

        SetCombatDirection(customBattleDirection); //Set Combat Direction

        MovePlayersToGridPos(warpUnits); // Players Moved First & CombatDirection Set

        //Generate Grid Here. Allow Players To Move First
        GenerateAvailableGridPositions(false);

        //Other Enemies should now grab their positions
        foreach(CharacterGridUnit enemy in assignedEnemies)
        {
            if(advantageType == CombatAdvantage.PlayerAdvantage && contactedEnemy && enemy == contactedEnemy.myGridUnit)
            {
                continue;
            }
            
            EnemyStateMachine stateMachine = enemy.GetComponent<EnemyStateMachine>();
            GridPosition desiredGridPos = GetEnemyDesiredGridPos(stateMachine, EnemyTacticsManager.Instance.GetAllChasingEnemies().Contains(stateMachine));

            //Vector3 direction = CombatFunctions.GetDirectionAsVector(enemy.transform);
            Vector3 direction = -combatDirectionAsVector;

            if (advantageType == CombatAdvantage.Neutral && contactedEnemy && enemy == contactedEnemy.myGridUnit)
            {
                direction = -CombatFunctions.GetCardinalDirectionAsVector(leader.transform);
            }

            Quaternion lookRotation = Quaternion.LookRotation(direction);

            if (warpUnits)
            {
                enemy.transform.rotation = lookRotation;
                enemy.transform.position = LevelGrid.Instance.gridSystem.GetWorldPosition(FindSuitableBattleStartGridPos(desiredGridPos));

                ReadyUnitForCombat(enemy);
            }
            else
            {
                enemy.transform.DORotate(lookRotation.eulerAngles, playerAmbushRotateTime);
                enemy.transform.DOJump(LevelGrid.Instance.gridSystem.GetWorldPosition(FindSuitableBattleStartGridPos(desiredGridPos)), unitJumpPower, 1, unitMoveToGridPosTime).OnComplete(() => ReadyUnitForCombat(enemy));
            }
        }
    }

    private void ReadyUnitForCombat(CharacterGridUnit unit)
    {
        unit.ActivateUnit(true);
        unit.unitAnimator.ShowModel(true);

        unit.SetGridPositions();
        unit.GetComponent<CharacterStateMachine>().BeginCombat();

        //Set Knockdown status.
        if (advantageType == CombatAdvantage.PlayerAdvantage && contactedEnemy && unit == contactedEnemy.myGridUnit)
        {
            StatusEffectManager.Instance.TriggerNewlyAppliedEffects(unit);
        }
        else if(advantageType == CombatAdvantage.EnemyAdvantage && contactedEnemy && unit == leader)
        {
            StatusEffectManager.Instance.TriggerNewlyAppliedEffects(unit);
        }
    }

    private void MoveCompanionsOutOfShot()
    {
        foreach (PlayerGridUnit player in PartyManager.Instance.GetActivePlayerParty())
        {
            if(player.unitName == leader.unitName)
            {
                continue;
            }

            Vector3 destination = player.transform.position +  -player.transform.forward * companionsMoveOutOfShotDistance;
            player.GetComponent<CharacterStateMachine>().BeginCombat();
            player.transform.DOJump(destination, unitJumpPower, 1, companionsMoveOutOfShotTime);
        }
    }

    private void MovePlayersToGridPos(bool warpUnits)
    {
        foreach (PlayerGridUnit player in GetPartySendToPosOrder())
        {
            if (advantageType == CombatAdvantage.EnemyAdvantage && contactedEnemy && player == leader) //Contacted Enemy is null during Story Battle.
            {
                continue;
            }

            GridPosition gridPos = centreGridPos + PartyManager.Instance.GetPlayerRelativeGridPosToCentrePos(player, combatDirectionAsVector);
            Quaternion lookRotation = Quaternion.LookRotation(combatDirectionAsVector);

            if (warpUnits)
            {
                player.transform.rotation = lookRotation;
                player.transform.position = LevelGrid.Instance.gridSystem.GetWorldPosition(FindSuitableBattleStartGridPos(gridPos));

                ReadyUnitForCombat(player);
            }
            else
            {
                player.transform.DORotate(lookRotation.eulerAngles, playerAmbushRotateTime);
                player.transform.DOJump(LevelGrid.Instance.gridSystem.GetWorldPosition(FindSuitableBattleStartGridPos(gridPos)), unitJumpPower, 1, unitMoveToGridPosTime).OnComplete(() => ReadyUnitForCombat(player));
            }
        }
    }


    private GridPosition GetEnemyDesiredGridPos(EnemyStateMachine enemy, bool isChasing)
     {
        if (enemy == contactedEnemy)
        {
            Vector3 backwardDirection = -CombatFunctions.GetCardinalDirectionAsVector(contactedEnemy.transform);

            if(advantageType == CombatAdvantage.Neutral)
            {
                backwardDirection = CombatFunctions.GetCardinalDirectionAsVector(leader.transform);
            }

            GridPosition desiredGridPos = new GridPosition(centreGridPos.x + (int)backwardDirection.x, centreGridPos.z + (int)backwardDirection.z);

            return desiredGridPos;
        }
        else if (isChasing || battleTrigger.battleType != BattleType.Normal)
        {
            return LevelGrid.Instance.gridSystem.GetGridPosition(enemy.transform.position);
        }

        //return LevelGrid.Instance.gridSystem.GetGridPosition(enemy.transform.position);

        //Get a random one from Valid Grid Positions. Only Executed During Normal Battles.
        if (availableEnemyGridPositions.Count == 0)
        {
            GenerateAvailableGridPositions(true);
        }

        int randIndex = UnityEngine.Random.Range(0, availableEnemyGridPositions.Count);
        return availableEnemyGridPositions[randIndex];
    }


    //Suitable Grid Pos
    public GridPosition FindSuitableBattleStartGridPos(GridPosition desiredGridPos, List<GridPosition> bannedPositions = null)
    {
        if (LevelGrid.Instance.TryGetObstacleAtPosition(desiredGridPos, out Collider obstacleData) || LevelGrid.Instance.IsGridPositionOccupiedByUnit(desiredGridPos, true))
        {
            //Theres an Obstacle at Position.
            Collider obstacleCollider;

            if (LevelGrid.Instance.GetUnitAtGridPosition(desiredGridPos))
            {
                obstacleCollider = LevelGrid.Instance.GetUnitAtGridPosition(desiredGridPos).gridCollider;
            }
            else
            {
                obstacleCollider = obstacleData;
            }
            
            //Find New Grid Pos and repeat validation.
            GridPosition newGridPos = GetNewGridPositionFromObstacle(desiredGridPos, obstacleCollider);
            return FindSuitableBattleStartGridPos(newGridPos);
        }
        else if (takenGridPositions.Contains(desiredGridPos) || (bannedPositions != null && bannedPositions.Contains(desiredGridPos)))
        {
            // A Character unit in the battle has already assigned this position.
            GridPosition newGridPos = GetNewGridPositionFromAssignedPos(desiredGridPos);
            return FindSuitableBattleStartGridPos(newGridPos);
        }

        if (!FantasyCombatManager.Instance.InCombat())
        {
            availableEnemyGridPositions.Remove(desiredGridPos);
            takenGridPositions.Add(desiredGridPos);
        }

        return desiredGridPos;
    }


    private GridPosition GetNewGridPositionFromObstacle(GridPosition desiredGridPosition, Collider obstacleCollider)
    {
        //Get Points on Collider Base. Find whichever is closest to desired Position. 
        //Then Place player either Front, Behind, Left or Right to that Grid Pos.
        Vector3 desiredWorldPos = LevelGrid.Instance.gridSystem.GetWorldPosition(desiredGridPosition);
        Vector3 closestWorldPosOnBounds = obstacleCollider.ClosestPointOnBounds(desiredWorldPos);

        GridPosition closestGridPosOnBounds = LevelGrid.Instance.gridSystem.GetGridPosition(closestWorldPosOnBounds);
        List<GridPosition> walkableNeighbours = PathFinding.Instance.GetGridPositionWalkableNeighbours(closestGridPosOnBounds, false);

        foreach (GridPosition gridPos in walkableNeighbours)
        {
            if (takenGridPositions.Contains(gridPos))
            {
                continue;
            }

            return gridPos;
        }
        //If you're here then that means either no walkable Neighbours or all walkable neighbours have been assigned.

        if(walkableNeighbours.Count > 0)
        {
            return walkableNeighbours[0];
        }

        //If here, means all neighbours are not walkable.
        Debug.Log("ALL NEIGHBOURS FROM OBSTACLE: " + obstacleCollider.name + " ARE UNWALKABLE. SETTING NEW BATTLE START GRID POS RANDOM POS");
        return closestGridPosOnBounds + new GridPosition(0, 1);
    }

    private GridPosition GetNewGridPositionFromAssignedPos(GridPosition desiredGridPosition)
    {
        List<GridPosition> walkableNeighbours = PathFinding.Instance.GetGridPositionWalkableNeighbours(desiredGridPosition, false);
        walkableNeighbours.Remove(centreGridPos);

        int randIndex = UnityEngine.Random.Range(0, walkableNeighbours.Count);
        return walkableNeighbours[randIndex];
    }

    /*private List<GridPosition> FilterPositionsOnHollowWrongSide(List<GridPosition> posList)
    {
        //This Method will remove grid positions that on the wrong side of the wall, if Obstacle is Hollow. 
    }*/
    private bool HasCombatIntiated()
    {
        return combatInitiated || FantasyCombatManager.Instance.InCombat() || CinematicManager.Instance.isCinematicPlaying || StoryManager.Instance.isTutorialPlaying;
    }

    private void UpdateTargetGroup(CinemachineTargetGroup targetGroup)
    {
        targetGroup.m_Targets[0].target = leader.camFollowTarget;
        targetGroup.m_Targets[1].target = contactedEnemy.myGridUnit.camFollowTarget;
    }

    private void ActivateAdvantageCam(GameObject camera, bool activate)
    {
        if (activate)
        {
            currentCam = camera;
        }

        currentCam.SetActive(activate);
    }

    private List<PlayerGridUnit> GetPartySendToPosOrder()
    {
        //Order Units first
        List<PlayerGridUnit> partyPosFree = new List<PlayerGridUnit>();
        List<PlayerGridUnit> partyPosOccupied = new List<PlayerGridUnit>();

        foreach (PlayerGridUnit player in PartyManager.Instance.GetPartyFormationOrder())
        {
            Vector3 direction = CombatFunctions.GetCardinalDirectionAsVector(leader.transform);
            GridPosition gridPos = centreGridPos + PartyManager.Instance.GetPlayerRelativeGridPosToCentrePos(player, direction);

            if (LevelGrid.Instance.TryGetObstacleAtPosition(gridPos, out Collider obstacleData) || LevelGrid.Instance.IsGridPositionOccupiedByUnit(gridPos, true))
            {
                partyPosOccupied.Add(player);
            }
            else
            {
                partyPosFree.Add(player);
            }
        }

        return partyPosFree.Concat(partyPosOccupied).ToList();
    }

}
