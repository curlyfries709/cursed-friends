using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using MoreMountains.Feedbacks;
using System.Linq;
using Sirenix.OdinInspector;
using AnotherRealm;


public  abstract class PlayerOffensiveSkill : PlayerBaseSkill
{
    [Title("OFFENSIVE SKILL DATA")]
    [SerializeField] PowerGrade powerGrade = PowerGrade.D;
    [Header("Bools")]
    [SerializeField] bool isMagical = false;
    [Tooltip("Charged skills activate on unit's next turn")]
    [SerializeField] bool isChargedSkill = false;
    [Range(0, 9)]
    [SerializeField] int knockbackDistance = 0;
    [Header("Element, Material & Effects")]
    [SerializeField] Element skillElement = Element.None;
    [SerializeField] WeaponMaterial skillMaterial = WeaponMaterial.None;
    [Space(10)]
    [SerializeField] List<ChanceOfInflictingStatusEffect> inflictedStatusEffects;
    [Header("Behaviour")]
    [HideIf("isSingleTarget")]
    [Tooltip("If this skill targets multiple, is each target damaged individually or at the same time. True if at the same time")]
    [SerializeField] bool attackMultipleUnitsIndividually = false;
    [Title("VFX & Spawn Points")]
    [SerializeField] protected Transform hitVFXPoolHeader;
    [SerializeField] protected List<Transform> hitVFXSpawnOffsets;
    //[Space(10)]
    //[ShowIf("isMagical")]
    //[SerializeField] protected GameObject magicalAttackVFX;
    [Space(10)]
    [ShowIf("isMagical")]
    [SerializeField] protected Transform magicalAttackVFXDestination;
    [Title("ATTACK ANIMATION DATA")]
    [SerializeField] protected string animationTriggerName;
    [Space(10)]
    [Tooltip("This is usually true for Physical Attacks")]
    [SerializeField] protected bool moveToAttack = true;
    [Space(10)]
    [ShowIf("moveToAttack")]
    [Tooltip("If false, it's Distance between the attacker and target during the attack. Else it's an offset on the attacker's forward")]
    [SerializeField] protected bool isAttackDistanceAForwardOffset = false;
    [ShowIf("moveToAttack")]
    [SerializeField] protected float animationAttackDistance = 1f;
    [Space(10)]
    [ShowIf("moveToAttack")]
    [SerializeField] protected float moveToTargetTime = 0.25f;
    [Space(10)]
    [Tooltip("Delay Duration before deactiving Game & Returning to Position")]
    [SerializeField] protected float delayBeforeReturn = 0.1f;
    [SerializeField] protected float returnToGridPosTime = 0.35f;
    [Title("FEEDBACKS")]
    [SerializeField] AffinityFeedback attackFeedbacks;

    //Storage
    protected List<MMF_Player> targetFeedbacksToPlay = new List<MMF_Player>();
    private List<GameObject> hitVFXPool = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();

        //Generate Pool Reference
        if (hitVFXPoolHeader)
        {
            foreach (Transform child in hitVFXPoolHeader)
            {
                hitVFXPool.Add(child.gameObject);
            }
        }
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if(battleResult != BattleResult.Restart) { return; }
        //STOP ALL FEEDBACKS

        foreach (var feedback in targetFeedbacksToPlay)
        {
            feedback?.StopFeedbacks();
        }
        
        attackFeedbacks.attackAbsorbedFeedback?.StopFeedbacks();
        attackFeedbacks.attackEvadedFeedback?.StopFeedbacks();
        attackFeedbacks.attackNulledFeedback?.StopFeedbacks();
        attackFeedbacks.attackReflectedFeedback?.StopFeedbacks();
        attackFeedbacks.attackConnectedFeedback.StopFeedbacks();
    }

    protected void Attack()
    {
        targetFeedbacksToPlay.Clear();

        Affinity attackAffinity = Affinity.None;

        List<Affinity> allTargetsAffinity = new List<Affinity>();

        foreach (GridUnit target in skillTargets)
        {
            int targetIndex = skillTargets.IndexOf(target);

            Affinity targetAffinity = DamageTarget(target, !(this is PlayerBaseChainAttack));
            allTargetsAffinity.Add(targetAffinity);

            GameObject hitVFX = hitVFXPool.Count > 0 ? hitVFXPool[targetIndex] : null;

            AffinityFeedback feedbacks = target.GetDamageable().GetDamageFeedbacks(CombatFunctions.GetVFXSpawnTransform(hitVFXSpawnOffsets, target), hitVFX);
            targetFeedbacksToPlay.Add(CombatFunctions.GetTargetFeedback(feedbacks, targetAffinity));
        }

        if (isSingleTarget)
        {
            attackAffinity = allTargetsAffinity[0];

            if (magicalAttackVFXDestination)
                magicalAttackVFXDestination.transform.position = skillTargets[0].camFollowTarget.position;
        }
        else
        {
            //If all targets Evaded
            if(allTargetsAffinity.Distinct().Count() == 1 && allTargetsAffinity[0] == Affinity.Evade)
            {
                attackAffinity = Affinity.Evade;
            }
        }

        MoveToAttack();

        myUnit.unitAnimator.TriggerSkill(animationTriggerName);

        CombatFunctions.PlayAttackFeedback(attackAffinity, attackFeedbacks);
    }


    private void MoveToAttack()
    {
        if (moveToAttack)
        {
            Vector3 destinationWithOffset;

            if (!isAttackDistanceAForwardOffset)
            {
                destinationWithOffset = skillTargets[0].GetClosestPointOnColliderToPosition(gridSystem.GetWorldPosition(myUnit.GetCurrentGridPositions()[0])) - (GetDirectionAsVector() * animationAttackDistance);
            }
            else
            {
                //Move Forward
                destinationWithOffset = myUnit.transform.position + (myUnitMoveTransform.forward.normalized * animationAttackDistance);
            }

            Vector3 moveDestination = new Vector3(destinationWithOffset.x, myUnitMoveTransform.position.y, destinationWithOffset.z);
            myUnit.transform.DOMove(moveDestination, moveToTargetTime);
        }
    }

    //Damage Methods
    protected Affinity DamageTarget(GridUnit target, bool canEvade = true)
    {
        myUnit.unitAnimator.beginHealthCountdown = isSingleTarget || !attackMultipleUnitsIndividually || (attackMultipleUnitsIndividually && skillTargets.Count == 1);

        List<InflictedStatusEffectData> successfulInflictedStatusEffects = CombatFunctions.TryInflictStatusEffects(myUnit, target, inflictedStatusEffects);

        AttackData damageData = new AttackData(myUnit, CombatFunctions.GetElement(myUnit, skillElement, isMagical), 
            CombatFunctions.GetMaterial(myUnit, skillMaterial, isMagical), GetDamage(), isCritical, successfulInflictedStatusEffects, knockbackDistance, skillTargets.Count);

        damageData.canEvade = canEvade;

        IDamageable damageable = target.GetDamageable();

        return damageable.TakeDamage(damageData); //(However Damage dealt & Status effects visual only shown much later)
    }


    public void PlayTargetsAffinityFeedback()
    {
        //Called Via A feedback
        foreach(var feedback in targetFeedbacksToPlay)
        {
            feedback?.PlayFeedbacks();
        }
    }

    protected override void SetUnitsToShow()
    {
        List<GridUnit> targetedUnits = CombatFunctions.SetOffensiveSkillUnitsToShow(myUnit, selectedUnits, knockbackDistance);
        FantasyCombatManager.Instance.SetUnitsToShow(targetedUnits);
    }

    //GETTERS

    protected int GetDamage()
    {
        return TheCalculator.Instance.CalculateRawDamage(myUnit, isMagical, powerGrade, out isCritical);
    }

    public override int GetSkillIndex()
    {
        if(CombatFunctions.GetElement(myUnit, skillElement, isMagical) != Element.None)
        {
            switch (CombatFunctions.GetElement(myUnit, skillElement, isMagical))
            {
                case Element.Fire:
                    return 3;
                case Element.Ice:
                    return 4;
                case Element.Air:
                    return 5;
                case Element.Earth:
                    return 6;
                case Element.Holy:
                    return 7;
                default:
                    return 8;
            }
        }
        else
        {
            switch (CombatFunctions.GetMaterial(myUnit, skillMaterial, isMagical))
            {
                case WeaponMaterial.Steel:
                    return 2;
                case WeaponMaterial.Gold:
                    return 1;
                default:
                    return 0;
            }
        }
    }

    public bool IsMagical()
    {
        return isMagical;
    }
}
