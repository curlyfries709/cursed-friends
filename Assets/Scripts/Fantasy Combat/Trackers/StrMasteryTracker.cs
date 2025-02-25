using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StrMasteryTracker : BaseMasteryTracker
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
        if(allPlayersProgressionType[player] == MasteryProgression.ProgressionType.UsePhys)
        {
            PlayerOffensiveSkill offensiveSkill = skill as PlayerOffensiveSkill;

            if (offensiveSkill && !offensiveSkill.IsMagical())
            {
                playerCombatProgression[player] = playerCombatProgression[player] + 1;
            }
        }
    }

    private void OnUnitDamaged(DamageData damageData)
    {
        PlayerGridUnit player = damageData.attacker as PlayerGridUnit;

        if (player && allPlayersProgressionType[player] == MasteryProgression.ProgressionType.PhysKo && damageData.isKOHit)
        {
            PlayerOffensiveSkill offensiveSkill = player.lastUsedSkill as PlayerOffensiveSkill;

            if (offensiveSkill && !offensiveSkill.IsMagical())
            {
                playerCombatProgression[player] = playerCombatProgression[player] + 1;
            }
        }
    }
}
