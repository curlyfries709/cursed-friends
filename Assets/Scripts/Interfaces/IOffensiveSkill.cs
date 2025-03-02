using AnotherRealm;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;
using Sirenix.Utilities;
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
    [Title("ATTACK ANIMATION DATA")]
    public string animationTriggerName;
    [Space(10)]
    [Tooltip("If set, the skill will trigger this to move to attack")]
    public AutoMover preAttackAutoMover;
    [Tooltip("If set, the skill will trigger once attack complete")]
    public AutoMover postAttackAutoMover;
    [Space(10)]
    [Tooltip("Used when calculating distance between target. For offsets on the skill owner's forward, leave this null, position a transform and set it as a destination in automover")]
    public Transform preMoveTargetTransform; //Position & Rotation updated in data. 
    public float attackDistanceBetweenTarget = 0.5f;
    [Space(10)]
    [Tooltip("Delay Duration before deactiving Game & Returning to Position")]
    public float delayBeforeReturn = 0.1f;
    public float returnToGridPosTime = 0.35f;
    [Title("VFX & FEEDBACKS")]
    public AffinityFeedback attackFeedbacks;
    [Space(10)]
    public Transform HitVFXPoolHeader;
    [Space(5)]
    [Tooltip("If this attack fires a projectile, set this header and the code will update the children's transform to each target's pos")]
    public Transform projectileAttackVFXDestinationsHeader;

    //INTERNAL DATA
    public BaseSkill associatedSkill { get; private set; }
    public GridUnit skillOwner { get; private set; }

    public CharacterGridUnit character { get; private set; }

    public List<GameObject> spawnedHitVFX { get; private set; } = new List<GameObject>();

    public void SetupData(BaseSkill skill, GridUnit skillOwner)
    {
        associatedSkill = skill;
        this.skillOwner = skillOwner;
        character = skillOwner as CharacterGridUnit;
    }

    public GameObject GetVFXFromPool(int index)
    {
        if (!HitVFXPoolHeader) { return null; }

        if(spawnedHitVFX.Count == 0)
        {
            GeneratePool();
        }

        return HitVFXPoolHeader.childCount > index ? HitVFXPoolHeader.GetChild(index).gameObject : SpawnNewVFX();
    }

    public void ReturnVFXToPool()
    {
        foreach (GameObject vfx in spawnedHitVFX)
        {
            vfx.SetActive(false);
            vfx.transform.parent = HitVFXPoolHeader;
        }
    }

    private void GeneratePool()
    {
        foreach (Transform child in HitVFXPoolHeader)
        {
            spawnedHitVFX.Add(child.gameObject);
        }
    }

    private GameObject SpawnNewVFX()
    {
        GameObject spawnedVFX = GameObject.Instantiate(spawnedHitVFX[0], HitVFXPoolHeader);
        spawnedVFX.SetActive(false);
        spawnedHitVFX.Add(spawnedVFX);

        return spawnedVFX;
    }
}

public interface IOffensiveSkill
{
    //ABSTRACT
    public OffensiveSkillData GetOffensiveSkillData();
    public IOffensiveSkill IOffensiveSkill();

    public bool ShouldMoveToAttack();

    public void OnDamageDealtToTarget(GridUnit target, DamageData damageData);
    //END ABSTRACT

    //HELPERS

    public void Attack(List<GridUnit> skillTargets, ref bool anyTargetsWithReflectAffinity)
    {
        Affinity attackAffinity = Affinity.None;
        List<Affinity> allTargetsAffinity = new List<Affinity>();

        OffensiveSkillData offensiveSkillData = GetOffensiveSkillData();

        //Damage all targets
        for(int i = 0; i < skillTargets.Count; ++i)
        {
            GridUnit target = skillTargets[i];
            DamageData damageData = DamageTarget(target, i, skillTargets.Count);

            Affinity targetAffinity = damageData != null ? damageData.affinityToAttack : Affinity.None;
            allTargetsAffinity.Add(targetAffinity);

            //Set if any reflects
            if (!anyTargetsWithReflectAffinity)
                anyTargetsWithReflectAffinity = targetAffinity == Affinity.Reflect;

            //Set Projectile destination position
            Transform projectileHeader = offensiveSkillData.projectileAttackVFXDestinationsHeader;

            if (projectileHeader)
            {
                projectileHeader.GetChild(i).transform.position = target.camFollowTarget.position;
            }

            OnDamageDealtToTarget(target, damageData);
        }

        //Set Attack overall Affinity for feedback
        if (skillTargets.Count == 1 || allTargetsAffinity.Distinct().Count() == 1)
        {
            attackAffinity = allTargetsAffinity[0];
        }

        /*MOVE BEFORE TRIGGER OPTIONS:
         *1) Use an AutoMover component for the movment and this code will trigger it
         *2) Use a MMF_Player feedback for the movement.
         *3) For simpler movement, use a MMF_Player feedback unity event to trigger movement via an AutoMoverComponent
         */
        if (ShouldMoveToAttack())
        {
            //Should Move to attack
            AutoMover mover = offensiveSkillData.preAttackAutoMover;

            //Set Move To Destination
            if (offensiveSkillData.preMoveTargetTransform)
                offensiveSkillData.preMoveTargetTransform.position = SetMoveToAttackDestination(skillTargets.Count == 1 ? skillTargets[0] : null);

            //Play Movement
            mover.PlayMovement(offensiveSkillData.character, offensiveSkillData.skillOwner.transform, TriggerSkillAnimation);
        }
        else
        {
            TriggerSkillAnimation();
        }

        //Play Feedback 
        CombatFunctions.PlayAffinityFeedback(attackAffinity, offensiveSkillData.attackFeedbacks);
    }

    private void TriggerSkillAnimation()
    {
        OffensiveSkillData offensiveSkillData = GetOffensiveSkillData();
        CharacterGridUnit character = offensiveSkillData.character;

        if (character)
        {
            character.unitAnimator.SetMovementSpeed(0);
        }
        else
        {
            return;
        }

        string skillAnimTrigger = offensiveSkillData.animationTriggerName;

        if (!skillAnimTrigger.IsNullOrWhitespace())
        {
            character.unitAnimator.TriggerSkill(skillAnimTrigger);
        }
    }

    public DamageData DamageTarget(GridUnit target, int targetIndex, int numOfSkillTargets)
    {
        AttackData attackData = GetAttackData(target, numOfSkillTargets);

        //SET HIT VFX
        GameObject hitVFX = GetOffensiveSkillData().GetVFXFromPool(targetIndex);

        if (hitVFX)
        {
            attackData.hitVFX = hitVFX;
            attackData.hitVFXPos = target.GetModelCollider().ClosestPointOnBounds(GetOffensiveSkillData().skillOwner.transform.position);
            attackData.hitVFXPos.y = target.camFollowTarget.position.y;
        }

        Health targetHealth = target.Health();
        DamageData damageData = targetHealth.TakeDamage(attackData, DamageType.Default);

        return damageData;
    }

    private Vector3 SetMoveToAttackDestination(GridUnit target)
    {
        OffensiveSkillData offensiveSkillData = GetOffensiveSkillData();
        GridUnit skillOwner = offensiveSkillData.skillOwner;

        Transform unitMoveTransform = skillOwner.transform;
        Vector3 destinationWithOffset;

        destinationWithOffset = target.GetClosestPointOnColliderToPosition(LevelGrid.Instance.gridSystem.GetWorldPosition(skillOwner.GetCurrentGridPositions()[0])) -
               (unitMoveTransform.forward.normalized * offensiveSkillData.attackDistanceBetweenTarget);

        Vector3 moveDestination = new Vector3(destinationWithOffset.x, unitMoveTransform.position.y, destinationWithOffset.z);

        return moveDestination;
    }

    public AttackData GetAttackData(GridUnit target, int numOfSkillTargets)
    {
        OffensiveSkillData offensiveSkillData = GetOffensiveSkillData();
        GridUnit skillOwner = offensiveSkillData.skillOwner;
        BaseSkill skill = offensiveSkillData.associatedSkill;

        AttackData attackData = new AttackData(skillOwner, GetSkillElement(), GetDamage(out bool isCritical), numOfSkillTargets);

        attackData.attackItem = offensiveSkillData.skillItem;
        attackData.canEvade = !(offensiveSkillData.isUnevadable || (this is PlayerBaseChainAttack));

        attackData.inflictedStatusEffects = CombatFunctions.TryInflictStatusEffects(skillOwner, target, offensiveSkillData.inflictedStatusEffects);
        attackData.forceData = skill.GetSkillForceData(target);

        attackData.isPhysical = !offensiveSkillData.isMagical;
        attackData.isCritical = isCritical;
        attackData.isMultiAction = skill.IsMultiActionSkill();

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
