using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StrategicBonusChecker : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] protected StrategicBonus myStrategicBonus;
    [Header("Events")]
    [SerializeField] bool listenToUnitHit;
    [SerializeField] bool listenToAnyUnitTurnStart;
    [SerializeField] bool listenToOnCombatEnd;
    [SerializeField] bool listenToCombatManagerActionComplete;
    [SerializeField] bool listenToPlayerUseSkill;

    protected virtual void OnEnable()
    {
        FantasyCombatManager.Instance.CombatBegun += ResetDataOnCombatBegin;

        if (listenToUnitHit)
            Health.UnitHit += OnUnitHit;

        if (listenToAnyUnitTurnStart)
            FantasyCombatManager.Instance.OnNewTurn += OnAnyUnitTurnStart;

        if (listenToOnCombatEnd)
            FantasyCombatManager.Instance.CombatEnded += OnCombatEnd;

        if (listenToCombatManagerActionComplete)
            FantasyCombatManager.Instance.ActionComplete += OnActionComplete;

        if (listenToPlayerUseSkill)
            PlayerBaseSkill.PlayerUsedSkill += OnPlayerUseSkill;
    }

    protected abstract void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType);

    protected virtual void OnUnitHit(DamageData damageData)
    {

    }

    protected virtual void OnAnyUnitTurnStart(CharacterGridUnit actingUnit, int turnNumber)
    {

    }

    protected virtual void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
    }

    protected virtual void OnActionComplete()
    {
    }

    protected virtual void OnPlayerUseSkill(PlayerGridUnit player, BaseSkill skill)
    {

    }

    protected virtual void OnDisable()
    {
        FantasyCombatManager.Instance.CombatBegun -= ResetDataOnCombatBegin;

        if (listenToUnitHit)
            Health.UnitHit -= OnUnitHit;
        if (listenToAnyUnitTurnStart)
            FantasyCombatManager.Instance.OnNewTurn -= OnAnyUnitTurnStart;
        if (listenToOnCombatEnd)
            FantasyCombatManager.Instance.CombatEnded -= OnCombatEnd;
        if (listenToCombatManagerActionComplete)
            FantasyCombatManager.Instance.ActionComplete -= OnActionComplete;
        if (listenToPlayerUseSkill)
            PlayerBaseSkill.PlayerUsedSkill -= OnPlayerUseSkill;
    }

    protected void BonusAchieved()
    {
        ProgressionManager.Instance.OnBonusAchieved(myStrategicBonus);
    }
}
