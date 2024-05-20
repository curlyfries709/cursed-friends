using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class AISkillCondition : MonoBehaviour
{
    [Title("Requirements")]
    [Tooltip("If true, condition will be evaluated at every position, rather than at start of Action Score Generation")]
    public bool evaluateConditionAtEachMovePosition = false;


    protected enum MathOperator
    {
        GreaterThanOrEqualTo,
        LessThanOrEqualTo
    }

    public abstract bool IsConditionMet(CharacterGridUnit myUnit, CharacterGridUnit preferredTarget, List<GridUnit> selectedUnits, GridPosition hypotheticalGridPos, AIBaseSkill skill);
}
