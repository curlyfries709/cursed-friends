using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SBValiantParty : StrategicBonusChecker
{
    [Range(5, 35)]
    [SerializeField] int thresholdToAchieveBonus = 25;

    int partyMembersAtBattleStart = 0;
    bool playedKOED = false;

    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        partyMembersAtBattleStart = PartyManager.Instance.GetActivePlayerParty().Count;
        FantasyHealth.CharacterUnitKOed += OnUnitKOed;
        playedKOED = false;
    }

    private void OnUnitKOed(CharacterGridUnit character)
    {
        if (!playedKOED)
            playedKOED = character is PlayerGridUnit;
    }

    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(battleResult, battleTrigger);
        FantasyHealth.CharacterUnitKOed -= OnUnitKOed;

        if (battleResult != BattleResult.Victory || FantasyCombatManager.Instance.GetPlayerCombatParticipants(true, true).Count < partyMembersAtBattleStart || playedKOED) { return; }

        foreach (PlayerGridUnit player in FantasyCombatManager.Instance.GetPlayerCombatParticipants(true, true))
        {
            if (Mathf.RoundToInt(player.Health().currentHealth) > thresholdToAchieveBonus)
            {
                return;
            }
        }

        BonusAchieved();
    }
}
