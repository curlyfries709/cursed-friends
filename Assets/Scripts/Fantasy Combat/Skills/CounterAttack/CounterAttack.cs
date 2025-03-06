using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

public class CounterAttack : BaseSkill, IOffensiveSkill
{
    [Title("OFFENSIVE SKILL DATA")]
    [SerializeField] protected OffensiveSkillData offensiveSkillData;
    [Title("COUNTER DATA")]
    [Tooltip("Optional conditions for counter to trigger. All must be true")]
    [SerializeField] protected List<SkillTriggerCondition> triggerConditions = new List<SkillTriggerCondition>();

    float? currentActionScore = null;

    public virtual void TriggerCounterAttack(CharacterGridUnit target)
    {
        BeginSkill();

        if (ShouldMoveToAttack())
        {
            //Update mover to ensure counter triggers after counter canvas complete
            AutoMover mover = offensiveSkillData.preAttackAutoMover;
            mover.SetMovementDuration(Evade.Instance.GetCounterCanvasDisplayTime());
            mover.SetCallbackOnComplete(true); //Just in case I forget to set it to true in inspector

            //Attack
            Attack();
        }
        else
        {
            StartCoroutine(WaitToAttackRoutine());
        }
    }

    IEnumerator WaitToAttackRoutine()
    {
        //Wait until counter canvas complete
        yield return new WaitForSeconds(Evade.Instance.GetCounterCanvasDisplayTime());
        Attack();
    }

    protected void BeginSkill()
    {
        BeginAction();
        PlayerGridUnit player = myCharacter as PlayerGridUnit;

        if(player)
            player.lastUsedSkill = this;

        //Set Times
        myCharacter.returnToGridPosTime = offensiveSkillData.returnToGridPosTime;
        myCharacter.delayBeforeReturn = offensiveSkillData.delayBeforeReturn;

        SetUnitsToShow();
        SetActionTargets(selectedUnits);

        //Call Event
        if(player)
            PlayerBaseSkill.PlayerUsedSkill?.Invoke(player, this);
    }

    public void OnDamageDealtToTarget(GridUnit target, DamageData damageData){}

    public bool CanTrigger(GridUnit targetToCounter)
    {
        if(!(targetToCounter is CharacterGridUnit)) { return false; } //Don't counter objects.

        bool isTargetInRange = IsTargetInRange(targetToCounter);

        if(!isTargetInRange || triggerConditions.Count == 0)
        {
            return isTargetInRange;
        }

        foreach(SkillTriggerCondition condition in triggerConditions)
        {
            if (!condition.IsConditionMet(myCharacter, targetToCounter, this))
            {
                return false;
            }
        }

        return true;
    }

    public float GetActionScore()
    {
        if(!currentActionScore.HasValue)
        {
            CalculateActionScore();
        }

        return currentActionScore.Value;
    }

    protected bool IsTargetInRange(GridUnit targetToCounter)
    {
        if(selectedUnits.Count == 0)
        {
            CalculateSelectedGridPos();
        }

        return selectedUnits.Contains(targetToCounter);
    }

    private void CalculateActionScore()
    {
        if (selectedUnits.Count == 0)
        {
            CalculateSelectedGridPos();
        }

        //Since the higher the power, the lower the int, we must do below. 
        float powerGradeScore = Enum.GetNames(typeof(PowerGrade)).Length - (int)offensiveSkillData.powerGrade;

        //Calculation prioritises power grade
        //11 + 1.1 = A++ targets 1 unit = 12.1
        //8 + 4.4 = B+ targets 4 units = 12.4
        currentActionScore = powerGradeScore + (selectedUnits.Count * 1.1f);
    }
    protected override void ResetData()
    {
        base.ResetData();
        currentActionScore = -1;
    }

    // KNOCKBACK ATTACK DATA. TO BE MOVED TO NEW SEPARATE CLASS
    public void DealKnockbackDamage(CharacterGridUnit target, PowerGrade powerGrade)
    {
        //DealDamage(target, false, powerGrade);
    }

    public void PlayBumpAttackAnimation()
    {
        //myUnit.unitAnimator.Counter();
    }

    //DOERS
    public void PlayCounterUI()
    {
        myCharacter.GetPhotoShootSet().PlayCounterUI();
    }

    public void DeactivateCounterUI()
    {
        myCharacter.GetPhotoShootSet().DeactivateSet();
    }

    //GETTERS
    public IOffensiveSkill IOffensiveSkill()
    {
        return this;
    }

    public OffensiveSkillData GetOffensiveSkillData()
    {
        return offensiveSkillData;
    }

    public bool ShouldMoveToAttack()
    {
        return offensiveSkillData.preAttackAutoMover;
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        throw new NotImplementedException();
    }
}
