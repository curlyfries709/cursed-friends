using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinMasteryTracker : BaseMasteryTracker
{
    protected override void ListenToEvents(bool listen)
    {
        if (listen)
        {
            Evade.Instance.UnitEvaded += OnUnitEvaded;
            IDamageable.UnitHit += OnUnitHit;
            Evade.Instance.CounterTriggered += OnUnitCounter;
        }
        else
        {
            Evade.Instance.UnitEvaded -= OnUnitEvaded;
            IDamageable.UnitHit -= OnUnitHit;
            Evade.Instance.CounterTriggered -= OnUnitCounter;
        }
    }

    private void OnUnitEvaded(CharacterGridUnit attacker, CharacterGridUnit evader)
    {
        PlayerGridUnit player = evader as PlayerGridUnit;

        if (player && allPlayersProgressionType[player] == MasteryProgression.ProgressionType.Evade)
        {
            playerCombatProgression[player] = playerCombatProgression[player] + 1;
        }
    }

    private void OnUnitHit(DamageData damageData) //Not Called if Unit Evaded.
    {
        PlayerGridUnit player = damageData.attacker as PlayerGridUnit;

        if (player && allPlayersProgressionType[player] == MasteryProgression.ProgressionType.Hit)
        {
            playerCombatProgression[player] = playerCombatProgression[player] + 1;
        }
    }

    private void OnUnitCounter(CharacterGridUnit counterattacker)
    {
        PlayerGridUnit player = counterattacker as PlayerGridUnit;

        if (player && allPlayersProgressionType[player] == MasteryProgression.ProgressionType.Counter)
        {
            playerCombatProgression[player] = playerCombatProgression[player] + 1;
        }
    }


}
