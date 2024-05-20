using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBBeatdownSurvivor : StrategicBonusChecker
{
    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        Beatdown.EnemyBeatdownSurvived += OnBeatdownSurvived;
    }


    private void OnBeatdownSurvived()
    {
        BonusAchieved();
    }

    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(battleResult, battleTrigger);
        Beatdown.EnemyBeatdownSurvived -= OnBeatdownSurvived;
    }
}
