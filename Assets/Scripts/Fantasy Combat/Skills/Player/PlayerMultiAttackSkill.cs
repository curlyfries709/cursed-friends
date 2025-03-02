using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMultiAttackSkill : PlayerOffensiveSkill
{
    [Title("Multi-Attack Behaviour")]
    [Tooltip("The number of attacks to perform before moving on to next target or ending skill")]
    [Range(1, 21)]
    [SerializeField] int numOfAttacks = 1;
    [Space(5)]
    [HideIf("isSingleTarget")]
    [Tooltip("If multi-target attack, are the targets attacked one-by-one or simultaneously")]
    [SerializeField] bool attackTargetsIndividually = false;
    [ShowIf("attackTargetsIndividually")]
    [Tooltip("If multi-target attack, the next target is found by searching an adjacent Grid Pos")]
    [SerializeField] bool findNewTargetInAdjacentPos = false;
    [ShowIf("findNewTargetInAdjacentPos")]
    [SerializeField] bool includeDiagonalsInAdjacentPosSearch = false;
    [Title("Multi-Attack Cancellation Conditions")]
    [Tooltip("Conditions that should immediately end this skill")]
    [SerializeField] List<MultiAttackCancellationCondition> cancellationConditions = new List<MultiAttackCancellationCondition>();

    bool cancelConditionMet = false;
    bool canMoveToAttack = true;
    int numOfAttackCounter = 0;

    List<GridUnit> hitTargets = new List<GridUnit>();

    public override void Attack()
    {
        if (attackTargetsIndividually)
        {
            List<GridUnit> singleTarget = new List<GridUnit>() //Grab first target
            {
                skillTargets[0]
            };

            //Attack Target
            IOffensiveSkill().Attack(singleTarget, ref anyTargetsWithReflectAffinity);
        }
        else
        {
            base.Attack();
        }

        canMoveToAttack = false;

        if (numOfAttackCounter == numOfAttacks)
        {
            if (attackTargetsIndividually)
            {
                GridUnit hitTarget = skillTargets[0];
                skillTargets.RemoveAt(0); //Only remove first target

                canMoveToAttack = true;

                if (findNewTargetInAdjacentPos)
                    SetNextTarget(hitTarget);
            }
            else
            {
                skillTargets.Clear();
            }
        }
        else
        {
            //Increase attack Counter
            numOfAttackCounter++;
        }
    }

    public override void OnDamageDealtToTarget(GridUnit target, DamageData damageData)
    {
        foreach (MultiAttackCancellationCondition condition in cancellationConditions)
        {
            if (condition.IsConditionMet(myUnit, target, damageData))
            {
                cancelConditionMet = true;
                return;
            }
        }
    }

    protected override void OnAllHealthUIComplete()
    {
        //If skill condition to cancel true or all targets hit, end action
        if (cancelConditionMet || skillTargets.Count >= 0)
        {
            base.OnAllHealthUIComplete(); //Calls End Action
            return;
        }

        //Else attack next target
        Attack();
    }

    private void SetNextTarget(GridUnit hitTarget)
    {
        hitTargets.Add(hitTarget);

        GridPosition currentGridPos = hitTarget.GetGridPositionsOnTurnStart()[0];

        //Get Neighbour Grid Pos with valid units occupying them.
        List<GridPosition> adjacentPositions = PathFinding.Instance.GetGridPositionOccupiedByUnitNeighbours(currentGridPos, 
            hitTargets, includeDiagonalsInAdjacentPosSearch, false, IsUnitValidTarget);

        if(adjacentPositions.Count > 0)
        {
            int randIndex = Random.Range(0, adjacentPositions.Count);
            skillTargets.Add(LevelGrid.Instance.GetUnitAtGridPosition(adjacentPositions[randIndex]));
        }
    }

    protected override void ResetData()
    {
        base.ResetData();
        cancelConditionMet = false;
        numOfAttackCounter = 0;
        canMoveToAttack = true;

        hitTargets.Clear();
        hitTargets.Add(myUnit);
    }

    //GETTERS
    public override bool IsMultiActionSkill()
    {
        return true;
    }

    public override bool AffectTargetsIndividually()
    {
        return attackTargetsIndividually;
    }

    public override bool ShouldMoveToAttack()
    {
        return base.ShouldMoveToAttack() && canMoveToAttack;
    }
}
