using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class SkillCondition : MonoBehaviour
{
    protected enum MathOperator
    {
        GreaterThanOrEqualTo,
        LessThanOrEqualTo
    }
}

public abstract class AISkillTriggerCondition : SkillCondition
{
    [Title("Requirements")]
    [Tooltip("If true, condition will be evaluated at every position, rather than at start of Action Score Generation")]
    public bool evaluateConditionAtEachMovePosition = false;

    public abstract bool IsConditionMet(CharacterGridUnit myUnit, CharacterGridUnit preferredTarget, List<GridUnit> selectedUnits, GridPosition hypotheticalGridPos, AIBaseSkill skill);
}

public abstract class MultiAttackCancellationCondition : SkillCondition
{
    public abstract bool IsConditionMet(GridUnit attacker, GridUnit target, DamageData targetCalculateDamageData);
}