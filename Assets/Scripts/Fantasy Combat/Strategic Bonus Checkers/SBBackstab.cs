using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBBackstab : StrategicBonusChecker
{
    [SerializeField] int countToAchieveBonus = 3;
    int counter = 0;

    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        counter = 0;
    }

    protected override void OnUnitHit(DamageData damageData)
    {
        if (damageData.mainInstigator is PlayerGridUnit && damageData.isBackstab)
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
