using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBExposed : StrategicBonusChecker
{
    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        //Method gets called on COmbat begin
        EnemyDatabase.Instance.AllEnemyAffinitiesUnlocked += IncreaseCounter;
    }

    private void IncreaseCounter()
    {
        BonusAchieved();
    }

    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(battleResult, battleTrigger);
        EnemyDatabase.Instance.AllEnemyAffinitiesUnlocked -= IncreaseCounter;
    }
}
