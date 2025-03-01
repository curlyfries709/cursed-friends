using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SelectInteractableSkill : CombatInteractableBaseSkill, ITurnEndEvent
{
    [Title("Components")]
    [SerializeField] ObjectHealth myHealth; 
    [Title("Trigger Condition")]
    [SerializeField] bool triggerOnAnyHit = false;
    [SerializeField] bool triggerOnWeaknessHit = false;
    [SerializeField] bool triggerOnKO = false;

    Tween currentRumbleTween;

    protected override void Awake()
    {
        base.Awake();

        myUnitMoveTransform = myUnit.transform;
        moveTransformGridCollider = myUnit.gridCollider;
    }

    private void OnEnable()
    {
        if (triggerOnAnyHit)
            Health.UnitHit += OnHit;

        if (triggerOnWeaknessHit)
            myHealth.WeaknessHit += OnHit;

        if (triggerOnKO)
            Health.UnitKOed += OnKO;
    }

    public void PlayTurnEndEvent()
    {
        TriggerSkill();
    }

    protected virtual void OnKO(GridUnit unit)
    {
        if(unit != myUnit) { return; }

        EnterRumbleState();
    }

    protected virtual void OnHit(DamageData damageData)
    {
        if(damageData.target != myUnit) { return; }

        EnterRumbleState();
    }

    protected virtual void EnterRumbleState() //This occurs when trigger condition met but waiting for turn end event to trigger skill.
    {
        currentRumbleTween = myUnitMoveTransform.DOShakeRotation(5, new Vector3(0, 0, 5), 15, 90, false).SetLoops(-1, LoopType.Yoyo);
        FantasyCombatManager.Instance.AddTurnEndEventToQueue(this);
    }

    protected virtual void TriggerSkill()
    {
        currentRumbleTween?.Kill();
        currentRumbleTween = null;

        BeginAction();
    }

    public override Dictionary<GridPosition, IHighlightable> ActivateHighlightedUI(bool activate, PlayerBaseSkill selectedBySkill)
    {
        myUnit.Health().ActivateHealthVisual(activate);

        if (activate)
        {
            CalculateSelectedGridPos();
        }
        else
        {
            return highlightableData;
        }
            
        bool canShowAOE = false;

        if (triggerOnAnyHit || triggerOnKO)
        {
            canShowAOE = true;
        }
        else if (triggerOnWeaknessHit)
        {
            Element skillElement;
            Item skillItem;

            if (selectedBySkill is PlayerOffensiveSkill attack)
            {
                skillElement = attack.GetSkillElement();
                skillItem = attack.GetSkillAttackItem();
            }
            else if (selectedBySkill is PlayerInteractSkill interact)
            {
                skillElement = interact.GetSkillElement();
                skillItem = interact.GetSkillItem();
            }
            else
            {
                return null;
            }

            Affinity affinity = TheCalculator.Instance.GetAffinity(myUnit, skillElement, skillItem);
            canShowAOE = affinity == Affinity.Weak;
        }

        return canShowAOE ? highlightableData : null;
    }

    protected override void SetRespawnable()
    {
        respawnable = myUnit as IRespawnable;
    }

    private void OnDisable()
    {
        if (triggerOnAnyHit)
            Health.UnitHit -= OnHit;

        if (triggerOnWeaknessHit)
            myHealth.WeaknessHit -= OnHit;

        if (triggerOnKO)
            Health.UnitKOed -= OnKO;
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        throw new System.NotImplementedException();
    }

    public void OnEventCancelled()
    {
        throw new NotImplementedException();
    }

    public float GetTurnEndEventOrder()
    {
        return FantasyCombatManager.Instance.GetSelectionInteractionEventPriority();
    }

    public List<Type> GetEventTypesThatCancelThis()
    {
        return new List<Type>();
    }
}
