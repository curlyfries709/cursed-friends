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
        if (damageData.mainInstigator is PlayerGridUnit player && player.CharacterHealth().isFiredUp && damageData.isKOHit)
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
