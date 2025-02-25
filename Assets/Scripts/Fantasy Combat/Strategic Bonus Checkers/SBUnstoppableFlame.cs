using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBUnstoppableFlame : StrategicBonusChecker
{
    [SerializeField] int countToAchieveBonus = 2;
    int counter = 0;

    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        counter = 0;
    }

    protected override void OnUnitHit(DamageData damageData)
    {
        if (damageData.attacker is PlayerGridUnit player && player.Health().isFiredUp && damageData.isKOHit)
        {
            counter++;

            if (counter == countToAchieveBonus)
            {
                BonusAchieved();
                counter = 0;
            }
        }
    }
}
