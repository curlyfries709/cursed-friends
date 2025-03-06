using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnotherRealm;
using Sirenix.OdinInspector;

public abstract class AIBaseSkill : BaseSkill
{
    [Title("Components")]
    [Tooltip("A Transform that is moved & rotated to Grid Positions to Generate action Score")]
    [SerializeField] protected Transform evaluationTransform;
    [Title("Conditions & Action Score")]
    public bool isPrioritySkill;
    [Range(1, 100)]
    [SerializeField] protected int triggerChance = 100;
    [Range(0, 20)]
    [SerializeField] protected int skillBonusActionScore;
    [Space(10)]
    [Range(0, 7)]
    [SerializeField] protected int minSkillCooldown = 0;
    [Range(0, 7)]
    [SerializeField] protected int maxSkillCooldown = 0;
    [Space(5)]
    [SerializeField] List<AISkillTriggerCondition> skillConditions;
    [Space(20)]
    [Header("VISUALS")]
    [SerializeField] protected GameObject blendListCamera;

    //Cache
    protected EnemyStateMachine myStateMachine;

    protected Transform myUnitTransform;

    //Shared Skill Data
    protected List<GridPosition> targetGridPositions = new List<GridPosition>();

    private AISkillData currentSkillTargetData = null;
    private BestPositionData skillDataToUse = new BestPositionData();
    protected FantasyCombatAI myAI;

    protected float rotationTime;

    //Instance Data (Data unique for each unit that uses this skill)
    FantasyCombatAI.InstancedSkillData currentInstancedSkillData;

    public class AISkillData
    {
        public AIBaseSkill skill;
        public bool hitsPreferredTarget;
        public GridPosition posToTriggerSkill;
        public Direction directionToTriggerSkill;
        public List<GridPosition> targetedGridPositions;
        public float actionScore; 
    }

    public class BestPositionData
    {
        public List<GridPosition> targetedGridPositions = new List<GridPosition>();

        public GridPosition posToTriggerSkill;
        public Direction directionToTriggerSkill;
    }


    public override void Setup(SkillPrefabSetter skillPrefabSetter, SkillData skillData)
    {
        base.Setup(skillPrefabSetter, skillData);

        AISkillPrefabSetter aISkillPrefabSetter = skillPrefabSetter as AISkillPrefabSetter;

        myUnitTransform = myUnit.transform;

        evaluationTransform = aISkillPrefabSetter.AIEvaluationBoxCollider.transform;
        myUnitMoveTransform = evaluationTransform;
        
        myStateMachine = myUnit.GetComponent<EnemyStateMachine>();

        SetEvaluationCollider();
    }

    public abstract void TriggerSkill();

    protected void BeginSkill(float returnToGridPosTime, float delayBeforeReturn)
    {
        BeginAction();

        GridSystemVisual.Instance.HideAllGridVisuals();

        myCharacter.unitAnimator.ResetMovementSpeed();  //Speed Set to 0 & Cancel Skill Feedback Reset

        //Warp Unit into Position & Rotation in an attempt to remove camera jitter.
        Vector3 desiredRotation = Quaternion.LookRotation(CombatFunctions.GetCardinalDirectionAsVector(myUnitTransform)).eulerAngles;
        myUnit.Warp(LevelGrid.Instance.gridSystem.GetWorldPosition(myUnit.GetCurrentGridPositions()[0]), Quaternion.Euler(new Vector3(0, desiredRotation.y, 0)));

        //Set Times
        myCharacter.returnToGridPosTime = returnToGridPosTime;
        myCharacter.delayBeforeReturn = delayBeforeReturn;

        //Reset Evaluation Transform pos
        myUnitMoveTransform.localPosition = Vector3.zero;
        myUnitMoveTransform.localRotation = Quaternion.Euler(Vector3.zero);

        //Set Selected Units & Grid Pos.
        selectedGridPositions = skillDataToUse.targetedGridPositions;
        SetSelectedUnits();

        SetUnitsToShow();

        SetActionTargets(selectedUnits);

        //UpdatePosition
        myUnit.MovedToNewGridPos();

        if (HasCooldown())
            currentInstancedSkillData.currentCooldown = UnityEngine.Random.Range(minSkillCooldown, maxSkillCooldown + 1);
    }

    protected bool CanTriggerSkill()
    {
        if (myCharacter.CanTriggerSkill == null)
        {
            return true;
        }

        bool canTriggerSkill = myCharacter.CanTriggerSkill();

        if (!canTriggerSkill)
        {
            myStateMachine.EnemyFantasyCombatActionComplete();
        }

        return canTriggerSkill;
    }


    protected void ActivateActionCamList(bool activate)
    {
        if (blendListCamera)
        {
            FantasyCombatManager.Instance.ActivateCurrentActiveCam(!activate);
            blendListCamera.SetActive(activate);
        }
    }

    protected override void SkillComplete()
    {
        base.SkillComplete();
        myStateMachine.EnemyFantasyCombatActionComplete();
    }

    //Skill Action Score Generation.
    public AISkillData GetBestActionScore(List<GridPosition> validMovePositions, FantasyCombatAI myAI, FantasyCombatAI.InstancedSkillData instancedSkillData)
    {
        currentInstancedSkillData = instancedSkillData;

        //Cannot Execute if Skill On Cooldown
        if(currentInstancedSkillData.currentCooldown > 0 && HasCooldown())
        {
            return null;
        }

        //Roll dice to see if can trigger skill
        int randNum = UnityEngine.Random.Range(0, 101);
        //Debug.Log("Trigger Roll For " + transform.name + " :" + randNum);

        if (randNum > triggerChance){ return null; }

        //Evaluate Skill Condition & Immediately return null if not met.
        foreach (AISkillTriggerCondition condition in skillConditions)
        {
            if(condition.evaluateConditionAtEachMovePosition) { continue; }
            if(!condition.IsConditionMet(myCharacter, myAI.preferredTarget as CharacterGridUnit, selectedUnits, myUnit.GetGridPositionsOnTurnStart()[0], this)) { Debug.Log(transform.name + " DID NOT MEET SKILL CONDITIONS");  return null; }
        }

        this.myAI = myAI;

        float currentBestActionScore = Mathf.NegativeInfinity;
        currentSkillTargetData = null;

        foreach (GridPosition movePos in validMovePositions)
        {
            if (IsCurrentGridPositionOccupiedByAnotherUnit(movePos, true)) { continue; }

            //Update Move Transform Position for Skill AOE Calculation
            myUnitMoveTransform.position = LevelGrid.Instance.gridSystem.GetWorldPosition(movePos);

            //Physics.SyncTransforms(); 

            if (IsSkillAffectedByRotation())
            {
                //Must Update Rotation too.
                for (int i = 0; i < 4; i++)
                {
                    myUnitMoveTransform.forward = CombatFunctions.GetDirectionAsVector((Direction)i);
                    AISkillData newSkillData = GetSkillDataAtPos(ref currentBestActionScore, movePos, (Direction)i);

                    if(newSkillData == null) { continue; }

                    UpdateBestPosition(newSkillData);
                    currentSkillTargetData = newSkillData;
                }
            }
            else
            {
                AISkillData newSkillData = GetSkillDataAtPos(ref currentBestActionScore, movePos, CombatFunctions.GetCardinalDirection(myUnitTransform));
                if (newSkillData == null) { continue; }

                UpdateBestPosition(newSkillData);
                currentSkillTargetData = newSkillData;
            }
        }

        return currentSkillTargetData;
    }

    private AISkillData GenerateActionScore(GridPosition currentGridPos)
    {
        //Update Selected Grid Positions & Selected Units.
        CalculateSelectedGridPos();

        if (selectedUnits.Count == 0) { return null; } //No Units selected, invalid.

        AISkillData returnData = new AISkillData(); //Only Necessary to Update action score & hits Preferred target
        AIOffensiveSkill offensiveSkill = this as AIOffensiveSkill;

        float totalActionScore = 0;

        returnData.hitsPreferredTarget = selectedUnits.Contains(myAI.preferredTarget);

        //Add designated skill BonusAction Score.
        totalActionScore = totalActionScore + skillBonusActionScore;

        //+3 For each unit this skill targets. If skill prioritizes multiple targets +5 instead.
        int numTargetMultiplier = myAI.targetingBehaviour == TargetingBehaviour.MaximumUnitsWitthAreaSkills ? 5 : 3;
        totalActionScore = totalActionScore + (selectedUnits.Count * numTargetMultiplier);

        if (offensiveSkill) 
        {
            //+ Skill Power Grade for Offensive Skills
            totalActionScore = totalActionScore + TheCalculator.Instance.GetPowerGradeMultiplier(offensiveSkill.GetOffensiveSkillData().powerGrade);

            //+1 For each unit this skill predicts it’ll KO or each unit at low hp...Might be too strong, so not implemented for now.
        }
        
        if (returnData.hitsPreferredTarget) //+10 if skill targets preferred target.
        {
            totalActionScore = totalActionScore + 10;
        }

        // +2 for each distance from closest target if unit prioritizes furthest grid pos.
        //-2 for each distance from closest target if unit prioritizes closest Grid pos.
        if (myAI.positioningBehaviour == PositioningBehaviour.ClosestPositionToStartingPos || myAI.positioningBehaviour == PositioningBehaviour.FurthestPositionFromTarget)
        {
            GridUnit closestUnit = CombatFunctions.GetClosestUnit(selectedUnits, myUnitMoveTransform);
            GridPosition closestUnitGridPosition = LevelGrid.Instance.gridSystem.GetGridPosition(closestUnit.GetClosestPointOnColliderToPosition(myUnitMoveTransform.transform.position));

            GridPosition startingGridPos = myAI.positioningBehaviour == PositioningBehaviour.FurthestPositionFromTarget ? closestUnitGridPosition : myUnit.GetCurrentGridPositions()[0];

            //Subtract one because Closest Position To Target shouldn't be penalised for having distance of 1 which is shortest possible distance.
            int distance = Mathf.Max((PathFinding.Instance.GetPathLengthInGridUnits(startingGridPos, currentGridPos, myCharacter)) - 1, 0);

            int multiplier = myAI.positioningBehaviour == PositioningBehaviour.FurthestPositionFromTarget ? 2 : -2;

            totalActionScore = totalActionScore + (distance * multiplier);
        }

        foreach (GridUnit target in selectedUnits)
        {
            CharacterGridUnit targetChar = target as CharacterGridUnit;

            if (myAI.prioritizeBackstabs)//+5 for each backstab grid Position if prioritizes backstabs.
            {
                if (TheCalculator.Instance.IsAttackBackStab(myUnitMoveTransform, target))
                {
                    totalActionScore = totalActionScore + 5;
                }
            }

            foreach (StatusEffectTarget statusEffectTarget in myAI.prioritiseUnitsWithTheseStatusEffects)
            {
                if (targetChar && StatusEffectManager.Instance.UnitHasStatusEffect(targetChar, statusEffectTarget.statusEffect) && CombatFunctions.IsUnitValidTarget(statusEffectTarget.unitType, myUnit, target))
                {
                    //+5 for each unit with prioritize SE.
                    totalActionScore = totalActionScore + 5;
                }
            }

            foreach (StatusEffectTarget statusEffectTarget in myAI.avoidTargetingUnitsWithTheseStatusEffects)
            {
                if (targetChar && StatusEffectManager.Instance.UnitHasStatusEffect(targetChar, statusEffectTarget.statusEffect) && CombatFunctions.IsUnitValidTarget(statusEffectTarget.unitType, myUnit, target))
                {
                    //-5 for each unit with avoided SE.
                    totalActionScore = totalActionScore - 5;
                }
            }

            if (offensiveSkill && myAI.shouldRememberAffinities && targetChar)
            {
                Element skillElement = CombatFunctions.GetElement(myCharacter, offensiveSkill.GetOffensiveSkillData().skillElement, offensiveSkill.GetOffensiveSkillData().isMagical); 

                if (myAI.IsAffinityRemembered(target, skillElement))
                {
                    Affinity affinity = TheCalculator.Instance.GetAffinity(targetChar, skillElement, null);

                    switch (affinity)
                    {
                        case Affinity.Weak:
                            if (myAI.prioritizeTargetingWeaknesses)
                            {
                                //+5 for each weakness targeted if the unit prioritises this. 
                                totalActionScore = totalActionScore + 5;
                            }
                            break;
                        case Affinity.Resist:
                            //-1 for each resist unit it hits.
                            totalActionScore = totalActionScore - 1;
                            break;
                        case Affinity.Immune:
                            //-3 for each immune unit it hits.
                            totalActionScore = totalActionScore - 3;
                            break;
                        case Affinity.Absorb:
                            //-5 for each absorb unit it hits.
                            totalActionScore = totalActionScore - 5;
                            break;
                        case Affinity.Reflect:
                            //-7 for each reflect unit it hits. 
                            totalActionScore = totalActionScore - 7;
                            break;
                    }
                }
            }
        }

        if (myAI.prioritizePlacingHazards) //+2 for each empty grid position this skill hits.
        { 
            foreach (GridPosition targetPos in selectedGridPositions)
            {
                if(!levelGrid.IsGridPositionOccupiedByUnit(targetPos, false))
                {
                    totalActionScore = totalActionScore + 2;
                }
            }
        }

        returnData.actionScore = totalActionScore;
        return returnData;
    }



    private AISkillData GetSkillDataAtPos(ref float currentBestActionScore, GridPosition currentGridPos, Direction direction)
    {
        AISkillData newSkillData = GenerateActionScore(currentGridPos);

        if(newSkillData == null) { return null; } //Means no units were selected.

        //Evaluate Skill Conditions for Each Move Position if necessary.
        foreach (AISkillTriggerCondition condition in skillConditions)
        {
            if (condition.evaluateConditionAtEachMovePosition && !condition.IsConditionMet(myCharacter, myAI.preferredTarget as CharacterGridUnit, selectedUnits, currentGridPos, this)) 
            {
                if (myAI.shouldPrintActionScoreDebug)
                    Debug.Log(transform.name + " DID NOT MEET SKILL CONDITIONS");

                return null;
            }
            
        }

        float calculatedScore = newSkillData.actionScore;

        bool defaultCondition = calculatedScore >= currentBestActionScore;
        bool conditionToUpdateData = myAI.targetComesFirst && myAI.preferredTarget ? newSkillData.hitsPreferredTarget && defaultCondition : defaultCondition;

        if (myAI.shouldPrintActionScoreDebug)
        {
            Debug.Log(transform.name + " ACTION SCORE: " + calculatedScore + " FOR POS: " + currentGridPos.ToString() + " WITH DIR: " + direction.ToString());
        }
            
        if (conditionToUpdateData)
        {
            if (calculatedScore == currentBestActionScore)
            {
                switch (myAI.positioningBehaviour)
                {
                    case PositioningBehaviour.ClosestPositionToStartingPos:
                        if (IsGridPositionCloserToMyStartPos(currentGridPos, currentSkillTargetData.posToTriggerSkill))
                        {
                            return GetSkillData(currentGridPos, direction, calculatedScore);
                        }
                        break;
                    case PositioningBehaviour.FurthestPositionFromTarget:
                        if (!IsGridPositionCloserToMyStartPos(currentGridPos, currentSkillTargetData.posToTriggerSkill))
                        {
                            return GetSkillData(currentGridPos, direction, calculatedScore);
                        }
                        break;
                    default:
                        //Choose Random.
                        int randNum = Random.Range(0, 2);
                        if (randNum == 1)
                        {
                            return GetSkillData(currentGridPos, direction, calculatedScore);
                        }
                        break;
                }

                //If None of above conditions satifised, stick with current value. So Return null.
                return null;
            }

            currentBestActionScore = calculatedScore;
            return GetSkillData(currentGridPos, direction, calculatedScore);
        }

        //If not high enough return nothing Or Doesn't hit preferred target, is not valid skill.
        return null;
    }

    private AISkillData GetSkillData(GridPosition currentGridPos, Direction direction, float newScore)
    {
        AISkillData skillData = new AISkillData();

        skillData.skill = this;
        skillData.hitsPreferredTarget = myAI.preferredTarget && selectedUnits.Contains(myAI.preferredTarget);
        skillData.posToTriggerSkill = currentGridPos;
        skillData.directionToTriggerSkill = direction;
        skillData.targetedGridPositions = selectedGridPositions;
        skillData.actionScore = newScore;

        return skillData;
    }


    private void UpdateBestPosition(AISkillData skillData)
    {
        skillDataToUse.targetedGridPositions = selectedGridPositions;

        skillDataToUse.posToTriggerSkill = skillData.posToTriggerSkill;
        skillDataToUse.directionToTriggerSkill = skillData.directionToTriggerSkill;
    }

    private bool HasCooldown()
    {
        return minSkillCooldown > 0 || maxSkillCooldown > 0;
    }

    public int GetFirstCooldown()
    {
        //To Ensure Skill isn't always first skill triggered
        if (HasCooldown())
        {
            return UnityEngine.Random.Range(0, maxSkillCooldown + 1);
        }

        return 0;
    }

    private bool IsSkillAffectedByRotation()
    {
        return !(originateFromUnitCentre || skillShape == SkillShape.Cross);
    }

    private bool IsGridPositionCloserToMyStartPos(GridPosition posToEvaluate, GridPosition otherPos)
    {
        float posToEvaluteDistance = Vector3.Distance(myUnit.transform.position, LevelGrid.Instance.gridSystem.GetWorldPosition(posToEvaluate));
        float otherPosDistance = Vector3.Distance(myUnit.transform.position, LevelGrid.Instance.gridSystem.GetWorldPosition(otherPos));

        return posToEvaluteDistance > otherPosDistance;
    }

    private void SetEvaluationCollider()
    {
        moveTransformGridCollider = evaluationTransform.GetComponent<BoxCollider>();

        moveTransformGridCollider.center = myUnit.gridCollider.center;
        moveTransformGridCollider.size = myUnit.gridCollider.size;
    }

    //Getters
    public List<FantasyCombatTarget> GetSkillTargets()
    {
        return targets;
    }


    //OLD CODE
    /*public void Setup(List<PlayerGridUnit> targets, float rotationTime)
    {
        //this.targets = targets;
        this.rotationTime = rotationTime;
    }*/



}
