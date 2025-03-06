using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
using AnotherRealm;
using System;
using Pathfinding;

public class FantasyCombatAI : MonoBehaviour
{
    [Title("Components")]
    [SerializeField] CharacterGridUnit myUnit;
    [Title("Targeting Behaviour")]
    [SerializeField] FantasyCombatTarget preferredTargetType;
    [Space(10)]
    public TargetingBehaviour targetingBehaviour;
    [ShowIf("@targetingBehaviour == TargetingBehaviour.UnitWithHighestAttribute || targetingBehaviour == TargetingBehaviour.UnitWithLowestAttribute")]
    [SerializeField] Attribute targetAttribute;
    [ShowIf("targetingBehaviour", TargetingBehaviour.SpecificRace)]
    [SerializeField] Race targetRace;
    [ShowIf("targetingBehaviour", TargetingBehaviour.SpecificCharacter)]
    [SerializeField] string targetCharacterName;
    [Space(10)]
    [Tooltip("If not skill hit their target despite a good action score, they will move closer to their target.")]
    public bool targetComesFirst;
    [Space(5)]
    [ListDrawerSettings(Expanded = true)]
    public List<StatusEffectTarget> prioritiseUnitsWithTheseStatusEffects;
    [Space(5)]
    [ListDrawerSettings(Expanded = true)]
    public List<StatusEffectTarget> avoidTargetingUnitsWithTheseStatusEffects;
    [Title("Action Behaviour")]
    public bool prioritizePlacingHazards;
    public bool prioritizeTargetingWeaknesses;
    public bool prioritizeBackstabs;
    [Space(10)]
    public bool shouldRememberAffinities;
    [Title("Positioning Behaviour")]
    public PositioningBehaviour positioningBehaviour;
    [Header("TEST")]
    public bool shouldPrintActionScoreDebug;

    //Public Variables
    public AIBaseSkill selectedSkill { get; private set; }
    public GridUnit preferredTarget { get; private set; }
    public Vector3 finalLookDirection { get; private set; }

    //Cached Callbacks
    Action<List<GridPosition> /* GridPosition Movement List */, List<Vector3> /* World Position Movement List */> ActionReadyCallback = null;
    MultiPathCallbackData multiPathCallbackData = null;

    bool hasAnyDiagonalSkills = false;

    //List
    List<AIBaseSkill> skillsList = new List<AIBaseSkill>();

    //The calculated path to traverse to reach destination and perform action
    List<Vector3> moveToPath = new List<Vector3>();

    //A list of valid grid positions that AI can move to. 
    List<GridPosition> currentValidMovementPos = new List<GridPosition>();
    public Dictionary<GridUnit, EnemyPartialData> knownEnemyAffinities { get; private set; }

    //Instanced Skill Data
    Dictionary<AIBaseSkill, InstancedSkillData> instancedSkillDataDict = new Dictionary<AIBaseSkill, InstancedSkillData>();

    public class MultiPathCallbackData
    {
        public GridPosition closestDestination;

        public float currentShortestPathLength = Mathf.Infinity;
        public int multiPathQueryCount = 0;
        public int multiPathCallbackCount = 0;

        public bool HasCallbackCheckedAllPaths()
        {
            return multiPathCallbackCount >= multiPathQueryCount;
        }

        public void ResetData()
        {
            multiPathQueryCount = 0;
            multiPathCallbackCount = 0;
            currentShortestPathLength = Mathf.Infinity;
        }
    }
    public struct InstancedSkillData
    {
        public InstancedSkillData(int newCooldown)
        {
            currentCooldown = newCooldown;
        }

        public int currentCooldown;

        public void SetCooldown(int newCooldown)
        {
            currentCooldown = newCooldown;
        }
        public void DecrementCooldown()
        {
            currentCooldown = Mathf.Max(currentCooldown - 1, 0);
        }
    }

    public void OnCombatBegin()
    {
        ResetData();

        preferredTarget = null;
        knownEnemyAffinities = new Dictionary<GridUnit, EnemyPartialData>();

        //Set Skillset
        skillsList = CombatSkillManager.Instance.GetAISpawnedSkills(myUnit);

        Health.UnitKOed += OnUnitKO;

        if(targetingBehaviour == TargetingBehaviour.SameRandomUnitTillKo)
        {
            SetPreferredTarget();
        }

        //Reset Skill Cooldown. To Ensure Skill isn't always first skill triggered
        foreach (AIBaseSkill skill in skillsList)
        {
            AddSkillToDataDict(skill);
            instancedSkillDataDict[skill].SetCooldown(skill.GetFirstCooldown());

            if (!hasAnyDiagonalSkills)
            {
                hasAnyDiagonalSkills = skill.IsDiagonal();
            }
        }
    }

    public void BeginTurn(Action<List<GridPosition>, List<Vector3>> onActionReadyCallback)
    {
        ResetData();
        ActionReadyCallback = onActionReadyCallback;

        if (CanSetPreferredTargetOnTurnStart())
            SetPreferredTarget();

        DecrementAllCooldowns();

        //Begin by querying valid move posiions
        PathFinding.Instance.QueryPathNodesWithinUnitMoveRange(myUnit, CalculateBestAction);
    }


    private void CalculateBestAction(Path path)
    {
        //List<GridPosition> moveGridPos = FantasyCombatManager.Instance.GetFantasyCombatMovement().GetValidMovementGridPositions(myUnit);
        currentValidMovementPos = PathFinding.Instance.GetGridPositionsFromPath(path); //Path returned to pool. So path will be null after this line.

        float bestActionScore = Mathf.NegativeInfinity;
        AIBaseSkill.AISkillData bestSkill = null;

        //Skills already ordered by priority under skill header based on sibling index.
        foreach (AIBaseSkill skill in skillsList)
        {
            AIBaseSkill.AISkillData newSkill = skill.GetBestActionScore(currentValidMovementPos, this, instancedSkillDataDict[skill]);

            if(newSkill == null) { continue; }

            if (skill.isPrioritySkill)
            {
                //Execute Priority Skill Immediately
                bestSkill = newSkill;
                break;
            }
            else if(newSkill.actionScore > bestActionScore)
            {
                bestActionScore = newSkill.actionScore;
                bestSkill = newSkill;
            }

            //Case if action score equal choose random.
        }

        if (bestSkill != null)
        {
            selectedSkill = bestSkill.skill;
            QueryPathToDestination(bestSkill.posToTriggerSkill, CombatFunctions.GetDirectionAsVector(bestSkill.directionToTriggerSkill));
        }
        else
        {
            //Move to Prefered Target
            if(preferredTarget == null)
            {
                //If null, set to closest unit.
                preferredTarget = CombatFunctions.GetClosestUnitOfTypeOrDefault(myUnit, preferredTargetType);
            }

            //Get Destination Closest to preferred target
            FindMovePosClosestToPreferredTarget();
        }

        //Sort Skills Based On Priority
        //Loop Through Skills.
        //Get Action best Action Score.
        //if not null & Priority Skill, immediately break out of loop.
        //At end of loop, if not null execute.
        //Else move to preferred Target...If Preferred Target null, Grab Random Unit.
    }


    private void DecrementAllCooldowns()
    {
        //Do this in separate function ensure all skill cooldowns are decremented. 
        foreach (AIBaseSkill skill in skillsList)
        {
            AddSkillToDataDict(skill);
            instancedSkillDataDict[skill].DecrementCooldown();
        }
    }

    private void OnUnitKO(GridUnit unit)
    {
        if(unit == myUnit)
        {
            Health.UnitKOed -= OnUnitKO;
        }
        else if(targetingBehaviour == TargetingBehaviour.SameRandomUnitTillKo && unit == preferredTarget)
        {
            SetPreferredTarget();
        }
    }

    private void QueryPathToDestination(GridPosition destination, Vector3 directionToFace)
    {
        finalLookDirection = directionToFace;
        Debug.Log("Setting look rotation for: " + EnemyDatabase.Instance.GetEnemyDisplayName(myUnit, myUnit.stats.data) + " " + finalLookDirection.ToString());

        //List<GridPosition> gridPosList = PathFinding.Instance.FindPath(myUnit.GetGridPositionsOnTurnStart()[0], destination, myUnit, out int pathLength, true);
        PathFinding.Instance.QueryStartToEndPath(myUnit.GetGridPositionsOnTurnStart()[0], destination, myUnit, SetMovementPath);
    }

    private void FindMovePosClosestToPreferredTarget()
    {
        /*This implementation currently has quick results. However, should it begin to bottleneck, an alternative Solution:
         * Cheaply calculate Manhattan distance between each occupiable pos in move list against each occupiable pos in target neighbour list.
         * However, this doesn’t take obstacles into account.
         * Pick path with smallest result. Query Start To End Path with pos in move list as destination. 
         * When updating move list, check if node is not occupied and is neighbour of target then make this new destination and break loop.  
         */
        List<GridPosition> occupiableGridPosInMoveList = currentValidMovementPos.Where((gridPos) => !LevelGrid.Instance.IsGridPositionOccupiedByDifferentUnit(myUnit, gridPos, true)).ToList();
        List<GridPosition> targetOccupiableNeighbourList = PathFinding.Instance.GetGridPositionOccupiableNeighbours(preferredTarget.GetGridPositionsOnTurnStart()[0], myUnit, hasAnyDiagonalSkills);

        if(targetOccupiableNeighbourList.Count == 0)
        {
            Debug.Log("DESIGN FALLBACK BEHAVIOUR FOR WHEN TARGET OCCUPIABLE NEIGHBOURS IS 0");
            throw new NotImplementedException();
        }
        else
        {
            multiPathCallbackData.multiPathQueryCount = targetOccupiableNeighbourList.Count;

            foreach (GridPosition occupiableNeighbourGridPos in targetOccupiableNeighbourList)
            {
                PathFinding.Instance.QueryClosestNodeToDestination(occupiableGridPosInMoveList, occupiableNeighbourGridPos, myUnit, OnShortestPathToPreferredTargetComplete);
            }
        }
    }

    private void OnShortestPathToPreferredTargetComplete(Path path)
    {
        //Increase callback Count
        multiPathCallbackData.multiPathCallbackCount++;

        int pathLength = path.vectorPath.Count;

        if (pathLength < multiPathCallbackData.currentShortestPathLength)
        {
            multiPathCallbackData.currentShortestPathLength = path.vectorPath.Count;
            multiPathCallbackData.closestDestination = LevelGrid.Instance.gridSystem.GetGridPosition((Vector3)path.path[0].position);
        }

        if(multiPathCallbackData.HasCallbackCheckedAllPaths())
        {
            QueryPathToDestination(multiPathCallbackData.closestDestination, Vector3.zero);
        }
    }

    private void SetMovementPath(Path path)
    {
        List<GridPosition> gridPosList = PathFinding.Instance.GetGridPositionsFromPath(path); //Path returned to pool so path will be null after this line
        List<GridPosition> gridPosMoveList = new List<GridPosition>();

        foreach (GridPosition gridPosition in gridPosList)
        {
            if (!currentValidMovementPos.Contains(gridPosition)) //Break out of loop if point beyond movement path
            {
                break;
            }

            if (IsCurrentGridPositionOccupiedByAnotherUnit(gridPosition, false))
            {
                //We want to move around them.
                Vector3 worldPos = LevelGrid.Instance.gridSystem.GetWorldPosition(gridPosition);
                Vector3 newMovePos = worldPos + (transform.right * (LevelGrid.Instance.GetCellSize() * 0.5f));
                moveToPath.Add(newMovePos);
            }
            else
            {
                moveToPath.Add(LevelGrid.Instance.gridSystem.GetWorldPosition(gridPosition));
            }

            gridPosMoveList.Add(gridPosition);
        }

        //Tell Enemy State Machine the action is ready to be performed.
        ActionReadyCallback?.Invoke(gridPosMoveList, moveToPath);
    }

    public bool IsAffinityRemembered(GridUnit target, Element element)
    {
        if (!knownEnemyAffinities.ContainsKey(target))
            knownEnemyAffinities[target] = new EnemyPartialData();

        EnemyPartialData data = knownEnemyAffinities[target];

        return data.knownElementAffinities.Where((item) => item.element == element).Count() > 0;
    }


    public void UpdateAffinities(GridUnit target, Affinity affinity, Element element)
    {
        if(affinity == Affinity.Evade || !shouldRememberAffinities) { return; }

        if (!knownEnemyAffinities.ContainsKey(target))
            knownEnemyAffinities[target] = new EnemyPartialData();

        EnemyPartialData data = knownEnemyAffinities[target];

        if (element != Element.None)
        {
            ElementAffinity elementAffinity = new ElementAffinity();
            elementAffinity.element = element;
            elementAffinity.affinity = affinity;

            data.knownElementAffinities.Add(elementAffinity);
        }
    }


    private void ResetData()
    {
        selectedSkill = null;
        moveToPath.Clear();
        finalLookDirection = Vector3.zero;
        hasAnyDiagonalSkills = false;


        if(multiPathCallbackData  != null)
        {
            multiPathCallbackData.ResetData();
        }
        else
        {
            multiPathCallbackData = new MultiPathCallbackData();
        }
    }

    private void AddSkillToDataDict(AIBaseSkill skill)
    {
        if (!instancedSkillDataDict.ContainsKey(skill))
        {
            instancedSkillDataDict[skill] = new InstancedSkillData();
        }
    }

    private bool CanSetPreferredTargetOnTurnStart()
    {
        return targetingBehaviour != TargetingBehaviour.SameRandomUnitTillKo && targetingBehaviour != TargetingBehaviour.MaximumUnitsWitthAreaSkills;
    }

    private void SetPreferredTarget()
    {
        preferredTarget = AICombatEvaluation.GetPriorityTarget(myUnit, preferredTargetType, targetingBehaviour,
            prioritiseUnitsWithTheseStatusEffects, avoidTargetingUnitsWithTheseStatusEffects,
            targetAttribute, targetRace, targetCharacterName);
    }

    private bool IsCurrentGridPositionOccupiedByAnotherUnit(GridPosition gridPosition, bool incluedKOedUnits)
    {
        return LevelGrid.Instance.IsGridPositionOccupiedByDifferentUnit(myUnit, gridPosition, incluedKOedUnits);
    }

    //Getters
    public float GetTurnStartDelay()
    {
        return FantasyCombatManager.Instance.onTurnStartDelay;
    }

    public float GetActDelay()
    {
        return FantasyCombatManager.Instance.actDelayIfNoMove;
    }

}
