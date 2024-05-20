using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBBumpDamage : StrategicBonusChecker
{

    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        
    }

    protected override void OnUnitHit(DamageData damageData)
    {
        if (!(damageData.attacker is PlayerGridUnit)) { return; }

        if (damageData.isKnockbackDamage && damageData.isKOHit)
        {
            BonusAchieved();
        }
    }
}
