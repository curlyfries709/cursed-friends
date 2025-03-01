using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnotherRealm;
using MoreMountains.Feedbacks;
using DG.Tweening;
using System.Linq;
using Sirenix.OdinInspector;

public abstract class AIOffensiveSkill : AIBaseSkill, IOffensiveSkill
{
    [Title("OFFENSIVE SKILL DATA")]
    [SerializeField] protected OffensiveSkillData offensiveSkillData;
    [Title("VFX & Spawn Points")]
    [SerializeField] protected Transform hitVFXPoolHeader;
    [SerializeField] protected List<Transform> hitVFXSpawnOffsets;
    [Space(10)]
    [SerializeField] protected Transform magicalAttackVFXDestination;
    [Title("ATTACK JUMP ANIMATION DATA")] 
    [SerializeField] float attackTriggerDelay = 0.3f;
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

    public override void Setup(SkillPrefabSetter skillPrefabSetter, SkillData skillData)
    {
        base.Setup(skillPrefabSetter, skillData);
        offensiveSkillData.SetupData(this, myUnit);
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

            Affinity targetAffinity = IOffensiveSkill().DamageTarget(target, skillTargets.Count, ref anyTargetsWithReflectAffinity);
            allTargetsAffinity.Add(targetAffinity);

            GameObject hitVFX = hitVFXPool.Count > 0 ? hitVFXPool[targetIndex] : null;

            AffinityFeedback feedbacks = target.Health().GetDamageFeedbacks(CombatFunctions.GetVFXSpawnTransform(hitVFXSpawnOffsets, target), hitVFX);
            targetFeedbacksToPlay.Add(CombatFunctions.GetTargetFeedback(feedbacks, targetAffinity));

            //Update Affinity
            myAI.UpdateAffinities(target, targetAffinity, CombatFunctions.GetElement(myCharacter, offensiveSkillData.skillElement, offensiveSkillData.isMagical));
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

        CombatFunctions.PlayAttackFeedback(attackAffinity, offensiveSkillData.attackFeedbacks);
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
        Invoke("DeactiveCam", offensiveSkillData.delayBeforeReturn);
    }

    IEnumerator MoveToTargetThenAttack()
    {
        //Vector3 destinationWithOffset = target.GetClosestPointOnColliderToPosition(LevelGrid.Instance.gridSystem.GetWorldPosition(myUnit.GetCurrentGridPositions()[0])) - (myUnitTransform.forward * animationAttackDistance);
        //Vector3 destination = new Vector3(destinationWithOffset.x, myUnitTransform.position.y, destinationWithOffset.z);

        if (attackStartTransform)
        {
            myUnit.transform.DOJump(attackStartTransform.position, jumpHeight, 1, jumpTime);
            yield return new WaitForSeconds(jumpTime + postJumpDelay);
        }

        IOffensiveSkill().MoveToAttack(target, GetSkillOwnerMoveTransform(), myUnitTransform.forward);

        if(attackTriggerDelay > 0)
        {
            myCharacter.unitAnimator.SetMovementSpeed(myCharacter.moveSpeed);
            yield return triggerAnimWaitTime;
        }

        myCharacter.unitAnimator.SetMovementSpeed(0);
        myCharacter.unitAnimator.TriggerSkill(offensiveSkillData.animationTriggerName);

        SkillComplete();//Must be called before FantasyCombatManager Action Complete to avoid bug where enemy doesnt act next turn.
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
        myCharacter.unitAnimator.ShowDamageFeedback(1);
    }

    protected override void SetUnitsToShow()
    {
        IOffensiveSkill().SetUnitsToShow(selectedUnits, forceDistance);
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if (battleResult != BattleResult.Restart) { return; }
        //STOP ALL FEEDBACKS

        foreach (var feedback in targetFeedbacksToPlay)
        {
            feedback?.StopFeedbacks();
        }

        IOffensiveSkill().StopAllSkillFeedbacks();
    }

    public OffensiveSkillData GetOffensiveSkillData()
    {
        return offensiveSkillData;
    }

    public IOffensiveSkill IOffensiveSkill()
    {
        return this;
    }
}
