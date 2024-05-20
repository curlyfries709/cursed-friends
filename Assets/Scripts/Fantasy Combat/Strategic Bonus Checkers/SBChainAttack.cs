using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBChainAttack : StrategicBonusChecker
{
    [SerializeField] int countToAchieveBonus = 3;

    private int counter = 0;
    bool subscribedToEndTurn = false;

    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        counter = 0;
        subscribedToEndTurn = false;

        PlayerBaseSkill.PlayerUsedSkill += OnChainAttackTriggered;
    }

    private void OnChainAttackTriggered(PlayerGridUnit player, PlayerBaseSkill skill)
    {
        if(!(skill is PlayerBaseChainAttack)) { return; }

        if (!subscribedToEndTurn)
        {
            FantasyCombatManager.Instance.GetCurrentTurnOwner().EndTurn += OnTurnEnd;
            subscribedToEndTurn = true;
        }

        counter++;

        if(counter == countToAchieveBonus)
        {
            BonusAchieved();
            counter = 0;
        }
    }

    private void OnTurnEnd()
    {
        counter = 0;

        if (subscribedToEndTurn)
        {
            FantasyCombatManager.Instance.GetCurrentTurnOwner().EndTurn -= OnTurnEnd;
            subscribedToEndTurn = false;
        }
    }

    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(battleResult, battleTrigger);
        PlayerBaseSkill.PlayerUsedSkill -= OnChainAttackTriggered;
    }
}
