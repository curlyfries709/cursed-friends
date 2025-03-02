using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


public  abstract class PlayerOffensiveSkill : PlayerBaseSkill, IOffensiveSkill
{
    [Title("OFFENSIVE SKILL DATA")]
    [SerializeField] protected OffensiveSkillData offensiveSkillData;

    public override void Setup(SkillPrefabSetter skillPrefabSetter, SkillData skillData)
    {
        base.Setup(skillPrefabSetter, skillData);
        offensiveSkillData.SetupData(this, myUnit);
    }

    public override bool TryTriggerSkill()
    {
        if (CanTriggerSkill(true))
        {
            BeginSkill(offensiveSkillData.returnToGridPosTime, offensiveSkillData.delayBeforeReturn, true);//Unit Position Updated here

            ActivateVisuals(true);

            //Attack
            Attack(); //Damage Target Called in here.

            return true;
        }
        else
        {
            return false;
        }
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if (battleResult != BattleResult.Restart) { return; }
        //STOP ALL FEEDBACKS
        //FIND A WAY TO STOP TARGET FEEDBACKS IF PLAYING Maybe just loop through targets and tell them to stop. 
        //Of Health subscribes to battle restart on stops feedback.


        IOffensiveSkill().StopAllSkillFeedbacks();
    }

    public virtual void OnDamageDealtToTarget(GridUnit target, DamageData damageData)
    {
        //Do nothing
    }

    //GETTERS
    public override int GetSkillIndex()
    {
        switch (IOffensiveSkill().GetSkillElement())
        {
            case Element.Silver: 
                return 0;
            case Element.Steel: 
                return 1;
            case Element.Gold:
                return 2;
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
            case Element.Curse: 
                return 8;
            default:
                Debug.Log("SKILL INDEX NOT SET FOR ELEMENT");
                return 0;
        }
    }

    public OffensiveSkillData GetOffensiveSkillData()
    {
        return offensiveSkillData;
    }

    public IOffensiveSkill IOffensiveSkill()
    {
        return this;
    }

    public virtual bool ShouldMoveToAttack()
    {
        return offensiveSkillData.preAttackAutoMover;
    }
}
