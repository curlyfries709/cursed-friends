using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class AIOffensiveSkill : AIBaseSkill, IOffensiveSkill
{
    [Title("OFFENSIVE SKILL DATA")]
    [SerializeField] protected OffensiveSkillData offensiveSkillData;

    public override void Setup(SkillPrefabSetter skillPrefabSetter, SkillData skillData)
    {
        base.Setup(skillPrefabSetter, skillData);
        offensiveSkillData.SetupData(this, myUnit);
    }

    //CALLED VIA FEEDBACKS
    public override void Attack()
    { 
        base.Attack();

        SkillComplete();//Must be called before FantasyCombatManager Action Complete to avoid bug where enemy doesnt act next turn.
        FantasyCombatManager.Instance.ActionComplete += PrepareToDeactivateActionCam;
    }

    //END CALLED VIA FEEDBACKS

    public void OnDamageDealtToTarget(GridUnit target, DamageData damageData)
    {
        //Update Affinity
        myAI.UpdateAffinities(target, damageData.affinityToAttack, IOffensiveSkill().GetSkillElement());
    }

    private void PrepareToDeactivateActionCam()
    {
        FantasyCombatManager.Instance.ActionComplete -= PrepareToDeactivateActionCam;
        Invoke("DeactiveCam", offensiveSkillData.delayBeforeReturn);
    }

    private void DeactiveCam()
    {
        ActivateActionCamList(false);
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if (battleResult != BattleResult.Restart) { return; }
        //STOP ALL FEEDBACKS
        //FIND A WAY TO STOP TARGET FEEDBACKS IF PLAYING Maybe just loop through targets and tell them to stop. 
        //Of Health subscribes to battle restart on stops feedback.
        IOffensiveSkill().StopAllSkillFeedbacks();
    }

    //GETTERS
    public OffensiveSkillData GetOffensiveSkillData()
    {
        return offensiveSkillData;
    }

    public IOffensiveSkill IOffensiveSkill()
    {
        return this;
    }
}
