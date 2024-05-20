using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBInflict : StrategicBonusChecker
{
    [SerializeField] int countToAchieveBonus = 2;
    int counter = 0;

    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        counter = 0;
        StatusEffectManager.Instance.Unitafflicted += OnUnitAfflicted;
    }

    private void OnUnitAfflicted(CharacterGridUnit target, CharacterGridUnit inflictor, StatusEffectData effectData)
    {
        PlayerGridUnit player = inflictor as PlayerGridUnit;

        if (player && !effectData.isStatBuffOrDebuff && !(target is PlayerGridUnit))
        {
            IncreaseCount();
        }  
    }

    private void IncreaseCount()
    {
        counter++;

        if (counter >= countToAchieveBonus)
        {
            BonusAchieved();
            counter = 0;
        }
    }

    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(battleResult, battleTrigger);
        StatusEffectManager.Instance.Unitafflicted -= OnUnitAfflicted;
    }
}
