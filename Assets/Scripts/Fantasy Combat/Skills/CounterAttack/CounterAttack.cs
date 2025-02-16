using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Sirenix.OdinInspector;
using AnotherRealm;
using MoreMountains.Feedbacks;

public abstract class CounterAttack : MonoBehaviour
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

    List<Transform> hitVFXSpawnOffsets = new List<Transform>();
    public Element attackElement { get; private set; }

    MMF_Player targetFeedbackToPlay;

    protected virtual void Awake()
    {
        myUnitTransform = myUnit.transform;

        if(vfxSpawnOffset)
            hitVFXSpawnOffsets.Add(vfxSpawnOffset);
    }

    public abstract void TriggerCounterAttack(CharacterGridUnit target);

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
        SetAttackElements();

        myUnit.unitAnimator.beginHealthCountdown = true;

        //Inflict Status Effect.
        int distance = allowKnockback ? knockbackDistance : 0;

        List<InflictedStatusEffectData> successfulInflictedStatusEffects = CombatFunctions.TryInflictStatusEffects(myUnit, target, inflictedStatusEffects);

        AttackData damageData = new AttackData(myUnit, attackElement, GetDamage(powerGrade), isCritical, successfulInflictedStatusEffects, distance, 1);
        damageData.canEvade = false;

        IDamageable damageable = target.GetComponent<IDamageable>();

        Affinity affinity = damageable.TakeDamage(damageData);

        
        AffinityFeedback feedbacks = target.Health().GetDamageFeedbacks(CombatFunctions.GetVFXSpawnTransform(hitVFXSpawnOffsets, target), hitVFX);

        targetFeedbackToPlay = CombatFunctions.GetTargetFeedback(feedbacks, affinity);
    }


    //GETTERS
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
    private void SetAttackElements()
    {
        attackElement = myUnit.stats.GetAttackElement();
    }



}
