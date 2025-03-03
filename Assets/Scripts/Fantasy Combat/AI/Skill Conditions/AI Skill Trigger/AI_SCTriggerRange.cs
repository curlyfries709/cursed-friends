using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class AI_SCTriggerRange : AISkillTriggerCondition
{
    [Title("Range Conditions")]
    [Tooltip("Leave false to only compare with units selected by skill. Otherwise true for all non-KO enemies in battle")]
    [SerializeField] bool compareWithAllTargets;
    [SerializeField] protected MathOperator conditionOperator;
    [Range(1, 10)]
    [SerializeField] protected int rangeThreshold = 3;

    private void Awake()
    {
        evaluateConditionAtEachMovePosition = true; //Range has to be checked at each new position.
    }

    public override bool IsConditionMet(CharacterGridUnit myUnit, CharacterGridUnit preferredTarget, List<GridUnit> selectedUnits, GridPosition hypotheticalGridPos, AIBaseSkill skill)
    {
        List<GridUnit> targets;

        if (compareWithAllTargets)
        {
            targets = AnotherRealm.CombatFunctions.GetEligibleTargets(myUnit, skill.GetSkillTargets()).ConvertAll((item) => item as GridUnit);
        }
        else
        {
            targets = selectedUnits;
        }

        if(targets.Count == 0) { return false; }

        foreach (GridUnit unit in targets)
        {
            int distance = PathFinding.Instance.ManhanttanDistance(hypotheticalGridPos, unit.GetGridPositionsOnTurnStart()[0]);

            if (conditionOperator == MathOperator.GreaterThanOrEqualTo && distance >= rangeThreshold)
            {
                continue;
            }
            else if (conditionOperator == MathOperator.LessThanOrEqualTo && distance <= rangeThreshold)
            {
                continue;
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}
