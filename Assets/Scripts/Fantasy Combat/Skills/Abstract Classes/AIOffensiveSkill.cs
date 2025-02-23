using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnotherRealm;
using MoreMountains.Feedbacks;
using DG.Tweening;
using System.Linq;
using Sirenix.OdinInspector;

public abstract class AIOffensiveSkill : AIBaseSkill
{
    [Title("Skill Data")]
    public bool isMagical;
    public PowerGrade powerGrade = PowerGrade.D;
    [Space(10)]
    public Element element;
    [Space(10)]
    [SerializeField] List<ChanceOfInflictingStatusEffect> inflictedStatusEffects;
    [Title("VFX & Spawn Points")]
    [SerializeField] protected Transform hitVFXPoolHeader;
    [SerializeField] protected List<Transform> hitVFXSpawnOffsets;
    [Space(10)]
    [ShowIf("isMagical")]
    [SerializeField] protected Transform magicalAttackVFXDestination;
    [Title("ATTACK ANIMATION DATA")] 
    [SerializeField] protected string animationName;
    [Space(5)]
    [SerializeField] bool moveToAttack = true;
    [ShowIf("moveToAttack")]
    [SerializeField] float animationAttackDistance = 1f;
    [Space(10)]
    [SerializeField] float attackTriggerDelay = 0.3f;
    [ShowIf("moveToAttack")]
    [SerializeField] float moveToTargetTime = 0.25f;
    [Space(10)]
    [SerializeField] protected float delayBeforeReturn = 0.1f;
    [SerializeField] protected float returnToGridPosTime = 0.5f;
    [Space(10)]
    [SerializeField] Transform attackStartTransform;
    [Space(5)]
    [ShowIf("attackStartTransform")]
    [SerializeField] bool jumpToStartTransform;
    [ShowIf("jumpToStartTransform")]
    [SerializeField] float jumpTime;
    [ShowIf("jumpToStartTransform")]
    [SerializeField] float jumpHeight;
    [ShowIf("jumpToStartTransform")]
    [SerializeField] float postJumpDelay = 0;
    [Title("FEEDBACKS")]
    [SerializeField] AffinityFeedback attackFeedbacks;

    //Storage
    protected List<MMF_Player> targetFeedbacksToPlay = new List<MMF_Player>();
    private List<GameObject> hitVFXPool = new List<GameObject>();

    protected WaitForSeconds triggerAnimWaitTime;

    GridUnit target;

    protected override void Awake()
    {
        base.Awake();
        triggerAnimWaitTime = new WaitForSeconds(attackTriggerDelay);

        //Generate Pool Reference
        if (hitVFXPoolHeader)
        {
            foreach (Transform child in hitVFXPoolHeader)
            {
                hitVFXPool.Add(child.gameObject);
            }
        }
    }

    protected void Attack()
    { 
        targetFeedbacksToPlay.Clear();
        Affinity attackAffinity = Affinity.None;

        List<Affinity> allTargetsAffinity = new List<Affinity>();

        target = skillTargets[0];

        foreach (GridUnit target in skillTargets)
        {
            int targetIndex = skillTargets.IndexOf(target);

            Affinity targetAffinity = DamageTarget(target);
            allTargetsAffinity.Add(targetAffinity);

            GameObject hitVFX = hitVFXPool.Count > 0 ? hitVFXPool[targetIndex] : null;

            AffinityFeedback feedbacks = target.GetDamageable().GetDamageFeedbacks(CombatFunctions.GetVFXSpawnTransform(hitVFXSpawnOffsets, target), hitVFX);
            targetFeedbacksToPlay.Add(CombatFunctions.GetTargetFeedback(feedbacks, targetAffinity));

            //Update Affinity
            myAI.UpdateAffinities(target, targetAffinity, CombatFunctions.GetElement(myUnit, element, isMagical));
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
            if (allTargetsAffinity.Distinct().Count() == 1 && allTargetsAffinity[0] == Affinity.Evade)
            {
                attackAffinity = Affinity.Evade;
            }
        }

        FantasyCombatManager.Instance.ActionComplete += PrepareToDeactivateActionCam;

        //Move Unit to Target & Attack
        StartCoroutine(MoveToTargetThenAttack());

        CombatFunctions.PlayAttackFeedback(attackAffinity, attackFeedbacks);
    }

    private void DeactiveCam()
    {
        //myUnit.transform.DOMove(gridSystem.GetWorldPosition(myUnit.GetGridPositionsOnTurnStart()[0]), returnToGridPosTime);
        ActivateActionCamList(false);
    }

    private void PrepareToDeactivateActionCam()
    {
        FantasyCombatManager.Instance.ActionComplete -= PrepareToDeactivateActionCam;
        //myUnit.unitAnimator.ReturnToNormalSpeed();
        Invoke("DeactiveCam", delayBeforeReturn);
    }

    IEnumerator MoveToTargetThenAttack()
    {
        Vector3 destinationWithOffset = target.GetClosestPointOnColliderToPosition(LevelGrid.Instance.gridSystem.GetWorldPosition(myUnit.GetCurrentGridPositions()[0])) - (myUnitTransform.forward * animationAttackDistance);
        Vector3 destination = new Vector3(destinationWithOffset.x, myUnitTransform.position.y, destinationWithOffset.z);

        if (attackStartTransform)
        {
            myUnit.transform.DOJump(attackStartTransform.position, jumpHeight, 1, jumpTime);
            yield return new WaitForSeconds(jumpTime + postJumpDelay);
        }

        if(moveToAttack)
            myUnit.transform.DOMove(destination, moveToTargetTime);

        if(attackTriggerDelay > 0)
        {
            myUnit.unitAnimator.SetSpeed(myUnit.moveSpeed);
            yield return triggerAnimWaitTime;
        }

        myUnit.unitAnimator.SetSpeed(0);
        myUnit.unitAnimator.TriggerSkill(animationName);

        SkillComplete();//Must be called before FantasyCombatManager Action Complete to avoid bug where enemy doesnt act next turn.
    }

    protected Affinity DamageTarget(GridUnit target)
    {
        AttackData attackData = GetAttackData(target);
        IDamageable damageable = target.GetComponent<IDamageable>();

        DamageData damageData = damageable.TakeDamage(attackData, DamageType.Default);

        if (damageData != null)
        {
            Affinity affinity = damageData.affinityToAttack;

            if (affinity == Affinity.Reflect)
            {
                anyTargetsWithReflectAffinity = true;
            }

            return affinity; //(However Damage dealt & Status effects visual only shown much later)
        }
        
        return Affinity.None;
    }

    public void PlayTargetsAffinityFeedback()
    {
        //Called Via A feedback
        foreach (var feedback in targetFeedbacksToPlay)
        {
            feedback?.PlayFeedbacks();
        }
    }

    public void DisplaySkillFeedback() //Called Via a feedback for skills that don't call this via their animation.
    {
        myUnit.unitAnimator.ShowDamageFeedback(1);
    }

    protected override void SetUnitsToShow()
    {
        List<GridUnit> targetedUnits = CombatFunctions.SetOffensiveSkillUnitsToShow(myUnit, selectedUnits, forceDistance);
        FantasyCombatManager.Instance.SetUnitsToShow(targetedUnits);
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if (battleResult != BattleResult.Restart) { return; }
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

    protected AttackData GetAttackData(GridUnit target)
    {
        if (!isMagical)
        {
            element = myUnit.stats.GetAttackElement();
        }

        AttackData attackData = new AttackData(myUnit, element, GetDamage(), skillTargets.Count);
        attackData.powerGrade = powerGrade;

        attackData.attackItem = null;

        attackData.inflictedStatusEffects = CombatFunctions.TryInflictStatusEffects(myUnit, target, inflictedStatusEffects);
        attackData.forceData = GetSkillForceData(target);

        attackData.isPhysical = !isMagical;
        attackData.isCritical = isCritical;
        attackData.isMultiAction = false;

        attackData.canEvade = true;
        attackData.canCrit = true;
        
        return attackData;
    }

    protected int GetDamage()
    {
        return TheCalculator.Instance.CalculateRawDamage(myUnit, isMagical, powerGrade, out isCritical);
    }
}
