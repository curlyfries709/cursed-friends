using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntMasteryTracker : BaseMasteryTracker
{
    protected override void ListenToEvents(bool listen)
    {
        if (listen)
        {
            PlayerBaseSkill.PlayerUsedSkill += OnPlayerUsedSkill;
            Health.UnitHit += OnUnitDamaged;
        }
        else
        {
            PlayerBaseSkill.PlayerUsedSkill -= OnPlayerUsedSkill;
            Health.UnitHit -= OnUnitDamaged;
        }
    }

    private void OnPlayerUsedSkill(PlayerGridUnit player, PlayerBaseSkill skill)
    {
        if (allPlayersProgressionType[player] == MasteryProgression.ProgressionType.UseMag)
        {
            PlayerOffensiveSkill offensiveSkill = skill as PlayerOffensiveSkill;

            if (offensiveSkill && offensiveSkill.IsMagical())
            {
                playerCombatProgression[player] = playerCombatProgression[player] + 1;
            }
        }
        else if(allPlayersProgressionType[player] == MasteryProgression.ProgressionType.UseOrb && skill is IOrb)
        {
            playerCombatProgression[player] = playerCombatProgression[player] + 1;
        }
    }

    private void OnUnitDamaged(DamageData damageData)
    {
        PlayerGridUnit player = damageData.attacker as PlayerGridUnit;

        if (player && allPlayersProgressionType[player] == MasteryProgression.ProgressionType.MagKo && damageData.isKOHit)
        {
            PlayerOffensiveSkill offensiveSkill = player.lastUsedSkill as PlayerOffensiveSkill;

            if (offensiveSkill && offensiveSkill.IsMagical())
            {
                playerCombatProgression[player] = playerCombatProgression[player] + 1;
            }
        }
    }
}
