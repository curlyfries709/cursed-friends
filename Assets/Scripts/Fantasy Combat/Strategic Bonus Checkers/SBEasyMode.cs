using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBEasyMode : StrategicBonusChecker
{
    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        
    }


    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(battleResult, battleTrigger);

        if(battleResult == BattleResult.Victory && GameManager.Instance.GetGameDifficulty() == GameManager.Difficulty.Easy)
        {
            BonusAchieved();
        }
    }
}
