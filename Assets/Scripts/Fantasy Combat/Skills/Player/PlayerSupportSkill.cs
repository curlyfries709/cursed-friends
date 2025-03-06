using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using MoreMountains.Feedbacks;
using Cinemachine;
using AnotherRealm;

public class PlayerSupportSkill : PlayerBaseSkill
{
    [Title("SUPPORT SKILL DATA")]
    [SerializeField] protected string animationTriggerName;
    [SerializeField] protected float actionDisplayTime = 2f;
    [Space(10)]
    [Tooltip("All Status Effects automatically have a 100% infliction chance and duration is based on caster's wisdom")]
    [ListDrawerSettings(Expanded = true)]
    [SerializeField] List<ChanceOfInflictingStatusEffect> appliedStatusEffects;
    [Header("FEEDBACKS")]
    [SerializeField] MMF_Player feedbackToPlay;

    //Cache
    CinemachineBlendListCamera blendListComp;

    protected override void Awake()
    {
        base.Awake();
        blendListComp = blendListCamera.GetComponent<CinemachineBlendListCamera>();
    }

    public override void SkillCancelled(bool showActionMenu = true)
    {
        base.SkillCancelled(false);
        FantasyCombatCollectionManager.MenuSkillCancelled(player);
    }

    public override bool TryTriggerSkill()
    {
        if (CanTriggerSkill(true))
        {
            BeginSkill(0, 0, true);//Unit Position Updated here

            CastSupport();

            return true;
        }
        else
        {
            return false;
        }
    }

    public void ActivateEffects() //Called Via A feedback
    {
        foreach(GridUnit unit in actionTargets)
        {
            CharacterGridUnit targetChar = unit as CharacterGridUnit;
            StatusEffectManager.Instance.TriggerNewlyAppliedEffects(targetChar);
            targetChar.CharacterHealth().GetHealthUI().ShowBuffsOnly();
        }
    }

    private void CastSupport()
    {
        UpdateTargetCam();
        ActivateVisuals(true);

        //Apply Status Effects
        ApplyAllEffects();

        //Trigger Skill
        myCharacter.unitAnimator.TriggerSkill(animationTriggerName);

        //Play Feedback
        feedbackToPlay?.PlayFeedbacks();

        StartCoroutine(SupportSkillRoutine());
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if (battleResult != BattleResult.Restart) { return; }

        StopAllCoroutines();
        feedbackToPlay?.StopFeedbacks();
        ActivateVisuals(false);
    }

    private void ApplyAllEffects()
    {
        foreach (GridUnit unit in actionTargets)
        {
            CharacterGridUnit targetChar = unit as CharacterGridUnit;

            foreach(ChanceOfInflictingStatusEffect effect in appliedStatusEffects)
            {
                StatusEffectManager.Instance.ApplyStatusEffect(effect.statusEffect, targetChar, myUnit, myUnit.stats.SEDuration, effect.buffChange);
            }

            targetChar.CharacterHealth().SetBuffsToApplyVisual(appliedStatusEffects);
        }
    }

    private void UpdateTargetCam()
    {
        blendListComp.LookAt = actionTargets[0].camFollowTarget;
        blendListComp.Follow = actionTargets[0].camFollowTarget;
    }

    IEnumerator SupportSkillRoutine()
    {
        yield return new WaitForSeconds(actionDisplayTime);
        EndAction();
    }

    public override int GetSkillIndex()
    {
        return CombatFunctions.GetNonAffinityIndex(OtherSkillType.Support);
    }
}
