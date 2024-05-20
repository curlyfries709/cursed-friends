using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndMasteryTracker : BaseMasteryTracker
{
    protected override void ListenToEvents(bool listen)
    {
        if (listen)
        {
            PlayerBaseSkill.PlayerUsedSkill += OnPlayerUsedSkill;
            IDamageable.unitHit += OnUnitDamaged;
            FiredUpEvent.UnitFiredUp += OnUnitFiredUp;
        }
        else
        {
            PlayerBaseSkill.PlayerUsedSkill -= OnPlayerUsedSkill;
            IDamageable.unitHit -= OnUnitDamaged;
            FiredUpEvent.UnitFiredUp -= OnUnitFiredUp;
        }
    }

    private void OnPlayerUsedSkill(PlayerGridUnit player, PlayerBaseSkill skill)
    {
        PlayerUsePotion potionSkill = skill as PlayerUsePotion;

        if (potionSkill)
        {
            PlayerGridUnit potionDrinker = potionSkill.potionDrinker as PlayerGridUnit;

            if (allPlayersProgressionType[potionDrinker] == MasteryProgression.ProgressionType.UsePotion)
            {
                playerCombatProgression[potionDrinker] = playerCombatProgression[potionDrinker] + 1;
            }
        }
    }

    private void OnUnitDamaged(DamageData damageData) //Not called when unit evades.
    {
        PlayerGridUnit player = damageData.target as PlayerGridUnit;

        if (player && allPlayersProgressionType[player] == MasteryProgression.ProgressionType.Survive && !damageData.isKOHit)
        {
            playerCombatProgression[player] = playerCombatProgression[player] + 1;
        }
    }

    private void OnUnitFiredUp(CharacterGridUnit unit)
    {
        PlayerGridUnit player = unit as PlayerGridUnit;

        if (player && allPlayersProgressionType[player] == MasteryProgression.ProgressionType.Fired)
        {
            playerCombatProgression[player] = playerCombatProgression[player] + 1;
        }
    }
}
