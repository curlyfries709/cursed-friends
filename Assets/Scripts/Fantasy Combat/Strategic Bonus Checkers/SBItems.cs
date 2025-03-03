using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBItems : StrategicBonusChecker
{
    [SerializeField] int countToAchieveBonus = 3;
    int counter = 0;

    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        counter = 0;
        FantasyCombatCollectionManager.BlessingUsed += OnUseBless;
    }

    protected override void OnPlayerUseSkill(PlayerGridUnit player, BaseSkill skill)
    {
        if (skill is PlayerUsePotion || skill is IOrb)
        {
            IncreaseCount();
        }
    }

    private void OnUseBless(PlayerGridUnit blesser)
    {
        IncreaseCount();
    }

    private void IncreaseCount()
    {
        counter++;

        if(counter >= countToAchieveBonus)
        {
            BonusAchieved();
            counter = 0;
        }
    }

    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(battleResult, battleTrigger);
        FantasyCombatCollectionManager.BlessingUsed -= OnUseBless;
    }
}
