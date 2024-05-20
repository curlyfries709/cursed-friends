using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBUpperHand : StrategicBonusChecker
{
    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        if(advantageType == BattleStarter.CombatAdvantage.PlayerAdvantage)
        {
            BonusAchieved();
        }
    }
}
