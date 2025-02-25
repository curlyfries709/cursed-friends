using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaMasteryTracker : BaseMasteryTracker
{
    protected override void ListenToEvents(bool listen)
    {
        if (listen)
        {
            Health.UnitHit += OnUnitDamaged;
            StatusEffectManager.Instance.Unitafflicted += OnUnitAfflicted;
        }
        else
        {
            Health.UnitHit -= OnUnitDamaged;
            StatusEffectManager.Instance.Unitafflicted -= OnUnitAfflicted;
        }
    }


    private void OnUnitDamaged(DamageData damageData)
    {
        PlayerGridUnit player = damageData.attacker as PlayerGridUnit;

        if (player && allPlayersProgressionType[player] == MasteryProgression.ProgressionType.Crit && damageData.isCritical)
        {
            playerCombatProgression[player] = playerCombatProgression[player] + 1;
        }
    }

    private void OnUnitAfflicted(CharacterGridUnit target, GridUnit inflictor, StatusEffectData effectData)
    {
        if (!inflictor) { return; } //Inflictor could be null.

        PlayerGridUnit player = inflictor as PlayerGridUnit;

        if (player && allPlayersProgressionType[player] == MasteryProgression.ProgressionType.InflictSE && !(target is PlayerGridUnit))
        {
            playerCombatProgression[player] = playerCombatProgression[player] + 1;
        }
        else if (player && allPlayersProgressionType[player] == MasteryProgression.ProgressionType.Wound && effectData.name == "Wounded")
        {
            playerCombatProgression[player] = playerCombatProgression[player] + 1;
        }
    }
   
}
