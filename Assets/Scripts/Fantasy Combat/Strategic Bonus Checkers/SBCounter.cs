using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBCounter : StrategicBonusChecker
{
    bool playerCounterTriggered = false;

    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        playerCounterTriggered = false;
        Evade.Instance.CounterTriggered += OnUnitCounter;
    }

    private void OnUnitCounter(CharacterGridUnit couterattacker)
    {
        PlayerGridUnit player = couterattacker as PlayerGridUnit;

        if (player)
        {
            playerCounterTriggered = true;
        }
    }

    protected override void OnUnitHit(DamageData damageData)
    {
        if(playerCounterTriggered && damageData.isKOHit)
        {
            BonusAchieved();
        }

        playerCounterTriggered = false;
    }

    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(battleResult, battleTrigger);
        Evade.Instance.CounterTriggered -= OnUnitCounter;
    }
}
