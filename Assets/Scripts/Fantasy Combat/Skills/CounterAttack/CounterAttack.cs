using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using AnotherRealm;
using MoreMountains.Feedbacks;

public abstract class CounterAttack : MonoBehaviour, ICombatAction
{
    //Counter Attacks can never occur diagonally. 
    [Title("Setup")]
    [SerializeField] protected CharacterGridUnit myUnit;
    [Title("Visuals")]
    [SerializeField] protected MMF_Player counterAttackFeedback;
    [Space(5)]
    [SerializeField] GameObject hitVFX;
    [SerializeField] Transform vfxSpawnOffset;
    [Title("COUNTER DATA")]
    [SerializeField] int range = 1;
    [Space(5)]
    [SerializeField] PowerGrade powerGrade = PowerGrade.D;
    [Range(0,9)]
    [SerializeField] int knockbackDistance = 0;
    [Space(10)]
    [Tooltip("Counterattacks Default to Natural or Weapon Element/Material ")]
    [SerializeField] bool isMagical;
    [Space(10)]
    [SerializeField] List<ChanceOfInflictingStatusEffect> inflictedStatusEffects;

    //Cache
    protected bool isCritical;
    protected Transform myUnitTransform;

    //State Variables
    public bool isActive { get; set; } = false;
    bool isReflectAffinity = false;
    int uiCounter = 0;

    List<Transform> hitVFXSpawnOffsets = new List<Transform>();

    MMF_Player targetFeedbackToPlay;

    protected virtual void Awake()
    {
        myUnitTransform = myUnit.transform;

        if(vfxSpawnOffset)
            hitVFXSpawnOffsets.Add(vfxSpawnOffset);
    }

    public void BeginAction()
    {
        FantasyCombatManager.Instance.SetCurrentAction(this, false);
        isReflectAffinity = false;
        uiCounter = 0;
    }

    public virtual void TriggerCounterAttack(CharacterGridUnit target)
    {
        BeginAction();
    }

    public void DisplayUnitHealthUIComplete()
    {
        uiCounter++;

        int totalToCheck = isReflectAffinity ? 2 : 1;

        if(uiCounter >= totalToCheck)
        {
            EndAction();
        }
    }
    public void EndAction()
    {
        FantasyCombatManager.Instance.SetCurrentAction(this, true);
        FantasyCombatManager.Instance.ActionComplete?.Invoke();
    }

    public void PlayBumpAttackAnimation()
    {
        myUnit.unitAnimator.Counter();
    }

    protected void PlayCounterattackAnimation()
    {
        myUnit.unitAnimator.ReturnToNormalSpeed();
        myUnit.unitAnimator.SetSpeed(0);
        myUnit.unitAnimator.Counter();
        counterAttackFeedback?.PlayFeedbacks();
    }

    public void PlayTargetFeedback() //CALLED VIA FEEDBACK
    {
        targetFeedbackToPlay?.PlayFeedbacks();
    }

    public void DealKnockbackDamage(CharacterGridUnit target, PowerGrade powerGrade)
    {
        DealDamage(target, false, powerGrade);
    }

    protected void DealDamage(CharacterGridUnit target, bool allowKnockback = true, PowerGrade powerGrade = PowerGrade.D)
    {
        AttackData attackData = GetAttackData(target, allowKnockback, powerGrade);

        IDamageable damageable = target.GetComponent<IDamageable>();

        DamageData damageData = damageable.TakeDamage(attackData, DamageType.Default);
        Affinity affinity = damageData != null ? damageData.affinityToAttack : Affinity.None;

        if(affinity == Affinity.Reflect)
        {
            isReflectAffinity = true;
        }

        AffinityFeedback feedbacks = target.Health().GetDamageFeedbacks(CombatFunctions.GetVFXSpawnTransform(hitVFXSpawnOffsets, target), hitVFX);

        targetFeedbackToPlay = CombatFunctions.GetTargetFeedback(feedbacks, affinity);
    }


    //GETTERS
    protected AttackData GetAttackData(GridUnit target, bool allowKnockback = true, PowerGrade powerGrade = PowerGrade.D)
    {
        AttackData attackData = new AttackData(myUnit, GetAttackElement(), GetDamage(powerGrade), 1);

        int distance = allowKnockback ? knockbackDistance : 0;
        SkillForceData skillForceData = new SkillForceData();

        skillForceData.forceType = allowKnockback ? SkillForceType.KnockbackAll : SkillForceType.None;
        skillForceData.directionType = SkillForceDirectionType.UnitForward;
        skillForceData.forceDistance = distance;

        //attackData.attackItem = skillItem;
        attackData.canEvade = false;

        attackData.inflictedStatusEffects = CombatFunctions.TryInflictStatusEffects(myUnit, target, inflictedStatusEffects);
        attackData.forceData = skillForceData;

        attackData.isPhysical = !isMagical;
        attackData.isCritical = isCritical;
        attackData.isMultiAction = false;

        return attackData;
    }

    protected int GetDamage(PowerGrade powerGrade)
    {
        return TheCalculator.Instance.CalculateRawDamage(myUnit, isMagical, powerGrade, out isCritical);
    }

    public int GetRange()
    {
        return range;
    }

    public void PlayCounterUI()
    {
        myUnit.GetPhotoShootSet().PlayCounterUI();
    }

    public void DeactivateCounterUI()
    {
        myUnit.GetPhotoShootSet().DeactivateSet();
    }
    //SETTERS

    public Element GetAttackElement()
    {
        return myUnit.stats.GetAttackElement();
    }
}
