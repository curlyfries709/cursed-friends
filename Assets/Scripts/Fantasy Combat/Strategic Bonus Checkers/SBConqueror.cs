using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBConqueror : StrategicBonusChecker
{
    [SerializeField] int countToAchieveBonus = 3;

    int counter = 0;
    string attackerName = "";

    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        counter = 0;
        attackerName = "";
    }

    protected override void OnUnitHit(DamageData damageData)
    {
        if (!damageData.mainInstigator || !(damageData.mainInstigator is PlayerGridUnit)) { return; }

        if (attackerName == "")
        {
            attackerName = damageData.mainInstigator.unitName;
        }

        //Ensure it's same attacker & is Player.
        if (damageData.mainInstigator.unitName == attackerName && damageData.isKOHit)
        {
            counter++; 
        }
    }

    protected override void OnActionComplete()
    {
        if (counter >= countToAchieveBonus)
        {
            BonusAchieved();
        }

        counter = 0;
        attackerName = "";
    }
}
