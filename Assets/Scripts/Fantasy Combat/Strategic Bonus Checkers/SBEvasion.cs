using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBEvasion : StrategicBonusChecker
{
    [SerializeField] int countToAchieveBonus = 3;
    int counter = 0;

    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        counter = 0;
        Evade.Instance.UnitEvaded += OnUnitEvaded;
    }

    private void OnUnitEvaded(CharacterGridUnit attacker, CharacterGridUnit evader)
    {
        PlayerGridUnit player = evader as PlayerGridUnit;

        if (player)
        {
            counter++;

            if (counter == countToAchieveBonus)
            {
                BonusAchieved();
                counter = 0;
            }
        }
    }

    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(battleResult, battleTrigger);
        Evade.Instance.UnitEvaded -= OnUnitEvaded;
    }
}
