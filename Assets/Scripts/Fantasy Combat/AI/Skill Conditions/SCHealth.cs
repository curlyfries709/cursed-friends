using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


public class SCHealth : AISkillCondition
{
    [Title("Health Conditions")]
    [SerializeField] protected bool unitHealthAtThreshold;
    [SerializeField] protected bool targetHealthAtThreshold;
    [Space(10)]
    [SerializeField] protected MathOperator conditionOperator;
    [Range(1, 100)]
    [SerializeField] protected int percantageThreshold = 5;

    public override bool IsConditionMet(CharacterGridUnit myUnit, CharacterGridUnit preferredTarget, List<GridUnit> selectedUnits, GridPosition hypotheticalGridPos, AIBaseSkill skill)
    {
        float healthToEvaluate = unitHealthAtThreshold ? myUnit.CharacterHealth().GetHealthNormalized() : preferredTarget.CharacterHealth().GetHealthNormalized();

        if (evaluateConditionAtEachMovePosition)
        {
            foreach(GridUnit unit in selectedUnits)
            {
                CharacterGridUnit targetChar = unit as CharacterGridUnit;

                if (!targetChar){ continue; }

                if (conditionOperator == MathOperator.GreaterThanOrEqualTo && healthToEvaluate >= ((float)percantageThreshold / 100))
                {
                    return true;
                }
                else if(conditionOperator == MathOperator.LessThanOrEqualTo && healthToEvaluate <= ((float)percantageThreshold / 100))
                {
                    return true;
                }
            }

            return false;
        }
        else
        {
            if (conditionOperator == MathOperator.GreaterThanOrEqualTo)
            {
                return healthToEvaluate >= ((float)percantageThreshold / 100);
            }
            else
            {
                return healthToEvaluate <= ((float)percantageThreshold / 100);
            }
        }
    }
}
