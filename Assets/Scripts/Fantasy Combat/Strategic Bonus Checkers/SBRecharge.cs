using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBRecharge : StrategicBonusChecker
{
    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        FantasyCombatCollectionManager.ItemCharged += OnItemCharged;
    }

    private void OnItemCharged(PlayerGridUnit charger, float normalizedHealth)
    {
        if (normalizedHealth >= 1)
        {
            BonusAchieved();
        }
    }

    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(battleResult, battleTrigger);
        FantasyCombatCollectionManager.ItemCharged -= OnItemCharged;
    }
}
