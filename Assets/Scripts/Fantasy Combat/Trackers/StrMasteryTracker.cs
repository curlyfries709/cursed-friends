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
            IOffensiveSkill offensiveSkill = skill as IOffensiveSkill;

            if (offensiveSkill != null && !offensiveSkill.IsMagical())
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
            IOffensiveSkill offensiveSkill = player.lastUsedSkill as IOffensiveSkill;

            if (offensiveSkill != null && !offensiveSkill.IsMagical())
            {
                playerCombatProgression[player] = playerCombatProgression[player] + 1;
            }
        }
    }
}
