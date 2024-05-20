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

    public override void SkillSelected()
    {
        if (!skillTriggered)
        {
            GridVisual();
        }
    }

    public override void SkillCancelled()
    {
        AudioManager.Instance.PlaySFX(SFXType.TabBack);

        FantasyCombatCollectionManager.MenuSkillCancelled(player);
        
        HideSelectedSkillGridVisual();//Contact Grid Visual To Reset the grid to Movement Only. 
    }

    public override bool TryTriggerSkill()
    {
        if (CanTriggerSkill(true))
        {
            BeginAction(0, 0, true);//Unit Position Updated here

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
        foreach(GridUnit unit in skillTargets)
        {
            CharacterGridUnit targetChar = unit as CharacterGridUnit;
            StatusEffectManager.Instance.TriggerNewlyAppliedEffects(targetChar);
            targetChar.Health().ActivateBuffHealthVisual();
        }
    }

    private void CastSupport()
    {
        UpdateTargetCam();
        ActivateVisuals(true);

        //Apply Status Effects
        ApplyAllEffects();

        //Trigger Skill
        myUnit.unitAnimator.TriggerSkill(animationTriggerName);

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
        foreach (GridUnit unit in skillTargets)
        {
            CharacterGridUnit targetChar = unit as CharacterGridUnit;

            foreach(ChanceOfInflictingStatusEffect effect in appliedStatusEffects)
            {
                StatusEffectManager.Instance.ApplyStatusEffect(effect.statusEffect, targetChar, myUnit, myUnit.stats.SEDuration, effect.buffChange);
            }

            targetChar.Health().SetBuffsToApplyVisual(appliedStatusEffects);
        }
    }

    private void UpdateTargetCam()
    {
        blendListComp.LookAt = skillTargets[0].camFollowTarget;
        blendListComp.Follow = skillTargets[0].camFollowTarget;
    }

    IEnumerator SupportSkillRoutine()
    {
        yield return new WaitForSeconds(actionDisplayTime);
        FantasyCombatManager.Instance.ActionComplete();
    }

    public override int GetSkillIndex()
    {
        return CombatFunctions.GetNonAffinityIndex(OtherSkillType.Support);
    }
}
