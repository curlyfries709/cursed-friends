using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
using AnotherRealm;

public class FantasyCombatAI : MonoBehaviour
{
    [Title("Components")]
    [SerializeField] CharacterGridUnit myUnit;
    [SerializeField] Transform skillHeader;
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


    //List
    List<AIBaseSkill> skillsList = new List<AIBaseSkill>();
    List<Vector3> movePositionList = new List<Vector3>();
    public Dictionary<GridUnit, EnemyPartialData> knownEnemyAffinities { get; private set; }

    private void Awake()
    {
        skillsList = skillHeader.GetComponentsInChildren<AIBaseSkill>().ToList();
    }

    public void OnCombatBegin()
    {
        ResetData();

        preferredTarget = null;
        knownEnemyAffinities = new Dictionary<GridUnit, EnemyPartialData>();
        
        FantasyHealth.CharacterUnitKOed += OnUnitKO;

        if(targetingBehaviour == TargetingBehaviour.SameRandomUnitTillKo)
        {
            SetPreferredTarget();
        }

        //Reset Skill Cooldown
        foreach (AIBaseSkill skill in skillsList)
        {
            skill.ResetCooldown();
        }
    }

    public void BeginTurn()
    {
        ResetData();

        if (CanSetPreferredTargetOnTurnStart())
            SetPreferredTarget();

        CalculateBestAction();
    }


    private void CalculateBestAction()
    {
        List<GridPosition> moveGridPos = FantasyCombatManager.Instance.GetFantasyCombatMovement().GetValidMovementGridPositions(myUnit);

        float bestActionScore = Mathf.NegativeInfinity;
        AIBaseSkill.AISkillData bestSkill = null;

        //Skills already ordered by priority under skill header based on sibling index.
        foreach (AIBaseSkill skill in skillsList)
        {
            AIBaseSkill.AISkillData newSkill = skill.GetBestActionScore(moveGridPos, this);

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
            SetMoveToPositionList(bestSkill.posToTriggerSkill, CombatFunctions.GetDirectionAsVector(bestSkill.directionToTriggerSkill));
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
            SetMoveToPositionList(GetPositionClosestToPreferredTarget(), Vector3.zero);
        }

        //Sort Skills Based On Priority
        //Loop Through Skills.
        //Get Action best Action Score.
        //if not null & Priority Skill, immediately break out of loop.
        //At end of loop, if not null execute.
        //Else move to preferred Target...If Preferred Target null, Grab Random Unit.
    }

    private void OnUnitKO(GridUnit unit)
    {
        if(unit == myUnit)
        {
            FantasyHealth.CharacterUnitKOed -= OnUnitKO;
        }
        else if(targetingBehaviour == TargetingBehaviour.SameRandomUnitTillKo && unit == preferredTarget)
        {
            SetPreferredTarget();
        }
    }

    private void SetMoveToPositionList(GridPosition destination, Vector3 directionToFace)
    {
        finalLookDirection = directionToFace;

        List<GridPosition> gridPosList = PathFinding.Instance.FindPath(myUnit.GetGridPositionsOnTurnStart()[0], destination, myUnit, out int pathLength, true);

        foreach (GridPosition gridPosition in gridPosList)
        {
            if (IsCurrentGridPositionOccupiedByAnotherUnit(gridPosition, false))
            {
                //We want to move around them.
                Vector3 worldPos = LevelGrid.Instance.gridSystem.GetWorldPosition(gridPosition);
                Vector3 newMovePos = worldPos + (transform.right * (LevelGrid.Instance.GetCellSize() * 0.5f));
                movePositionList.Add(newMovePos);
            }
            else
            {
                movePositionList.Add(LevelGrid.Instance.gridSystem.GetWorldPosition(gridPosition));
            } 
        }
    }

    public List<Vector3> GetMoveList()
    {
        return movePositionList;
    }

    private GridPosition GetPositionClosestToPreferredTarget()
    {
        float shortestPathLengthToTarget = Mathf.Infinity;

        GridPosition targetGridPos = myUnit.GetGridPositionsOnTurnStart()[0];
        List<GridPosition> moveGridPos = FantasyCombatManager.Instance.GetFantasyCombatMovement().GetValidMovementGridPositions(myUnit);

        foreach (GridPosition movePos in moveGridPos)
        {
            if (!LevelGrid.Instance.IsGridPositionOccupied(movePos, true))
            {
                float pathLength = PathFinding.Instance.GetPathLength(movePos, preferredTarget.GetGridPositionsOnTurnStart()[0], myUnit, myUnit.MoveRange() > 1);

                if (pathLength < shortestPathLengthToTarget)
                {
                    shortestPathLengthToTarget = pathLength;
                    targetGridPos = movePos;
                }
            }
        }

        return targetGridPos;
    }

    public bool IsAffinityRemembered(GridUnit target, Element element, WeaponMaterial material)
    {
        if (!knownEnemyAffinities.ContainsKey(target))
            knownEnemyAffinities[target] = new EnemyPartialData();

        EnemyPartialData data = knownEnemyAffinities[target];

        if (element != Element.None)
        {
            return data.knownElementAffinities.Where((item) => item.element == element).Count() > 0;
        }
        else
        {
            return data.knownMaterialAffinities.Where((item) => item.material == material).Count() > 0;
        }
    }


    public void UpdateAffinities(GridUnit target, Affinity affinity, Element element, WeaponMaterial material)
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
        else
        {
            MaterialAffinity materialAffinity = new MaterialAffinity();
            materialAffinity.material = material;
            materialAffinity.affinity = affinity;

            data.knownMaterialAffinities.Add(materialAffinity);
        }
    }


    private void ResetData()
    {
        selectedSkill = null;
        movePositionList.Clear();
        finalLookDirection = Vector3.zero;
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
