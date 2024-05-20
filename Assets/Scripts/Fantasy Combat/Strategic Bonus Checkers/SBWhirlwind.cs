using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBWhirlwind : StrategicBonusChecker
{
    [Header("Bonus Specific Variables")]
    [SerializeField] int maxTurnsAllowedToAchieveBonus = 3;

    int numOfTurns;

    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType){}

    protected override void OnAnyUnitTurnStart(CharacterGridUnit actingUnit, int turnNumber)
    {
        numOfTurns = turnNumber;
    }

    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(battleResult, battleTrigger);
        if (battleResult != BattleResult.Victory) { return; }

        if(numOfTurns <= maxTurnsAllowedToAchieveBonus)
        {
            BonusAchieved();
        }
    }
}
