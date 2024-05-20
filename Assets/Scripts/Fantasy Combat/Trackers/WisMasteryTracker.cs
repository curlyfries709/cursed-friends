using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WisMasteryTracker : BaseMasteryTracker
{
    protected override void ListenToEvents(bool listen)
    {
        //ADD CHARGE LATER
        if (listen)
        {
            PlayerBaseSkill.PlayerUsedSkill += OnPlayerUsedSkill;
            FantasyCombatCollectionManager.BlessingUsed += OnPartyBlessed;
            FantasyCombatCollectionManager.ItemCharged += OnItemCharged;
        }
        else
        {
            PlayerBaseSkill.PlayerUsedSkill -= OnPlayerUsedSkill;
            FantasyCombatCollectionManager.BlessingUsed -= OnPartyBlessed;
            FantasyCombatCollectionManager.ItemCharged -= OnItemCharged;
        }
    }

    private void OnPlayerUsedSkill(PlayerGridUnit player, PlayerBaseSkill skill)
    {
        if (allPlayersProgressionType[player] == MasteryProgression.ProgressionType.UseSupport && skill is PlayerSupportSkill)
        {
            playerCombatProgression[player] = playerCombatProgression[player] + 1;
        }
    }

    private void OnPartyBlessed(PlayerGridUnit blesser)
    {
        if (allPlayersProgressionType[blesser] == MasteryProgression.ProgressionType.UseBlessing)
        {
            playerCombatProgression[blesser] = playerCombatProgression[blesser] + 1;
        }
    }

    private void OnItemCharged(PlayerGridUnit charger, float normalizedHealth)
    {
        if (allPlayersProgressionType[charger] == MasteryProgression.ProgressionType.ChargeItems)
        {
            playerCombatProgression[charger] = playerCombatProgression[charger] + 1;
        }
    }
}
