using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBUntouched : StrategicBonusChecker
{
    bool playerDamaged = false;

    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        playerDamaged = false;
    }

    protected override void OnUnitHit(DamageData damageData)
    {
        if(damageData.target is PlayerGridUnit && damageData.HPChange > 0 && damageData.affinityToAttack != Affinity.Absorb)
        {
            playerDamaged = true;
        }
    }

    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if (battleResult != BattleResult.Victory) { return; }

        if (!playerDamaged)
        {
            BonusAchieved();
        }
    }

}
