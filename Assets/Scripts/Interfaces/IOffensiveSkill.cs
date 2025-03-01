using AnotherRealm;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using static UnityEditor.Rendering.FilterWindow;
using UnityEngine.TextCore.Text;

[System.Serializable]
public class OffensiveSkillData
{
    //EXPOSED DATA
    [Title("SKILL DATA")]
    public PowerGrade powerGrade = PowerGrade.D;
    [Header("Bools")]
    public bool isMagical = false;
    [Tooltip("Charged skills activate on unit's next turn")]
    public bool isChargedSkill = false;
    [Tooltip("Can the skill not be evaded")]
    public bool isUnevadable = false;
    [Header("Element, Material & Effects")]
    public Element skillElement = Element.None;
    public Item skillItem = null;
    [Space(10)]
    public List<ChanceOfInflictingStatusEffect> inflictedStatusEffects;
    [Title("ATTACK BEHAVIOUR")]
    [Tooltip("If this skill targets multiple, is each target damaged individually or at the same time. True if at the same time")]
    public bool attackMultipleUnitsIndividually = false;
    [Title("ATTACK ANIMATION DATA")]
    public string animationTriggerName;
    [Space(10)]
    [Tooltip("This is usually true for Physical Attacks")]
    public bool moveToAttack = true;
    [Space(10)]
    [ShowIf("moveToAttack")]
    [Tooltip("If false, it's Distance between the attacker and target during the attack. Else it's an offset on the attacker's forward")]
    public bool isAttackDistanceAForwardOffset = false;
    [ShowIf("moveToAttack")]
    public float animationAttackDistance = 1f;
    [Space(10)]
    [ShowIf("moveToAttack")]
    public float moveToTargetTime = 0.25f;
    [Space(10)]
    [Tooltip("Delay Duration before deactiving Game & Returning to Position")]
    public float delayBeforeReturn = 0.1f;
    public float returnToGridPosTime = 0.35f;
    [Title("FEEDBACKS")]
    public AffinityFeedback attackFeedbacks;


    //INTERNAL DATA
    public BaseSkill associatedSkill { get; private set; }
    public GridUnit skillOwner { get; private set; }

    public CharacterGridUnit character { get; private set; }


    public void SetupData(BaseSkill skill, GridUnit skillOwner)
    {
        associatedSkill = skill;
        this.skillOwner = skillOwner;
        character = skillOwner as CharacterGridUnit;
    }
}

public interface IOffensiveSkill
{
    //ABSTRACT
    public OffensiveSkillData GetOffensiveSkillData();
    public IOffensiveSkill IOffensiveSkill();
    //END ABSTRACT

    //HELPERS

    //DAMAGE
    public Affinity DamageTarget(GridUnit target, int numOfSkillTargets, ref bool anyTargetsWithReflectAffinity)
    {
        AttackData attackData = GetAttackData(target, numOfSkillTargets);

        Health targetHealth = target.Health();
        DamageData damageData = targetHealth.TakeDamage(attackData, DamageType.Default);

        if (damageData != null)
        {
            Affinity affinity = damageData.affinityToAttack;

            if (affinity == Affinity.Reflect)
            {
                anyTargetsWithReflectAffinity = true;
            }

            return affinity;
        }

        return Affinity.None;
    }

    public void MoveToAttack(GridUnit target, Transform unitMoveTransform, Vector3 unitCardinalDirection) 
    {
        OffensiveSkillData offensiveSkillData = GetOffensiveSkillData();
        GridUnit skillOwner = offensiveSkillData.skillOwner;

        //MIGHT NEED TO UPDATE DIRECTION TO SUPPORT INTERCARDINAL

        if (offensiveSkillData.moveToAttack)
        {
            Vector3 destinationWithOffset;

            if (!offensiveSkillData.isAttackDistanceAForwardOffset)
            {
                destinationWithOffset = target.GetClosestPointOnColliderToPosition(LevelGrid.Instance.gridSystem.GetWorldPosition(skillOwner.GetCurrentGridPositions()[0])) - 
                    (unitCardinalDirection * offensiveSkillData.animationAttackDistance);
            }
            else
            {
                //Move Forward
                destinationWithOffset = skillOwner.transform.position + (unitMoveTransform.forward.normalized * offensiveSkillData.animationAttackDistance);
            }

            Vector3 moveDestination = new Vector3(destinationWithOffset.x, unitMoveTransform.position.y, destinationWithOffset.z);
            skillOwner.transform.DOMove(moveDestination, offensiveSkillData.moveToTargetTime);
        }
    }

    public AttackData GetAttackData(GridUnit target, int numOfSkillTargets)
    {
        OffensiveSkillData offensiveSkillData = GetOffensiveSkillData();
        GridUnit skillOwner = offensiveSkillData.skillOwner;
        BaseSkill skill = offensiveSkillData.associatedSkill;

        AttackData attackData = new AttackData(skillOwner, CombatFunctions.GetElement(skillOwner, offensiveSkillData.skillElement, offensiveSkillData.isMagical), GetDamage(out bool isCritical), numOfSkillTargets);

        attackData.attackItem = offensiveSkillData.skillItem;
        attackData.canEvade = !(offensiveSkillData.isUnevadable || (this is PlayerBaseChainAttack));

        attackData.inflictedStatusEffects = CombatFunctions.TryInflictStatusEffects(skillOwner, target, offensiveSkillData.inflictedStatusEffects);
        attackData.forceData = skill.GetSkillForceData(target);

        attackData.isPhysical = !offensiveSkillData.isMagical;
        attackData.isCritical = isCritical;
        attackData.isMultiAction = offensiveSkillData.attackMultipleUnitsIndividually;

        attackData.powerGrade = offensiveSkillData.powerGrade;
        attackData.canCrit = true;

        return attackData;
    }

    private int GetDamage(out bool isCritical)
    {
        OffensiveSkillData offensiveSkillData = GetOffensiveSkillData();
        return TheCalculator.Instance.CalculateRawDamage(offensiveSkillData.skillOwner, offensiveSkillData.isMagical, offensiveSkillData.powerGrade, out isCritical);
    }

    //DOERS
    public void SetUnitsToShow(List<GridUnit> selectedUnits, int forceDistance)
    {
        List<GridUnit> targetedUnits = CombatFunctions.SetOffensiveSkillUnitsToShow(GetOffensiveSkillData().skillOwner, selectedUnits, forceDistance);
        FantasyCombatManager.Instance.SetUnitsToShow(targetedUnits);
    }

    public void StopAllSkillFeedbacks()
    {
        OffensiveSkillData offensiveSkillData = GetOffensiveSkillData();

        offensiveSkillData.attackFeedbacks.attackAbsorbedFeedback?.StopFeedbacks();
        offensiveSkillData.attackFeedbacks.attackEvadedFeedback?.StopFeedbacks();
        offensiveSkillData.attackFeedbacks.attackNulledFeedback?.StopFeedbacks();
        offensiveSkillData.attackFeedbacks.attackReflectedFeedback?.StopFeedbacks();
        offensiveSkillData.attackFeedbacks.attackConnectedFeedback.StopFeedbacks();
    }

    //MORE GETETRS
    public Element GetSkillElement()
    {
        OffensiveSkillData offensiveSkillData = GetOffensiveSkillData();
        return CombatFunctions.GetElement(offensiveSkillData.skillOwner, offensiveSkillData.skillElement, offensiveSkillData.isMagical);
    }

    public Item GetSkillAttackItem()
    {
        return GetOffensiveSkillData().skillItem;
    }

    public bool IsMagical()
    {
        return GetOffensiveSkillData().isMagical;
    }

}
