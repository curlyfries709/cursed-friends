using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBPowerOfFriendship : StrategicBonusChecker
{
    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        PowerOfFriendship.PowerOfFriendshipTriggered += OnPOFTriggered;
    }

    void OnPOFTriggered()
    {
        BonusAchieved();
    }

    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(battleResult, battleTrigger);
        PowerOfFriendship.PowerOfFriendshipTriggered -= OnPOFTriggered;
    }
}
