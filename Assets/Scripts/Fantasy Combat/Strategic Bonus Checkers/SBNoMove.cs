using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SBNoMove : StrategicBonusChecker
{
    Dictionary<PlayerGridUnit, List<GridPosition>> turnStartGridPos = new Dictionary<PlayerGridUnit, List<GridPosition>>();

    bool hasMoved = false;

    protected override void ResetDataOnCombatBegin(BattleStarter.CombatAdvantage advantageType)
    {
        hasMoved = false;
        turnStartGridPos.Clear();

        foreach (PlayerGridUnit player in FantasyCombatManager.Instance.GetPlayerCombatParticipants(true, true))
        {
            turnStartGridPos[player] = new List<GridPosition>(player.GetGridPositionsOnTurnStart());
        }
    }

    protected override void OnUnitHit(DamageData damageData)
    {
        PlayerGridUnit player = damageData.target as PlayerGridUnit;

        if (!player || hasMoved) { return; }

        hasMoved = damageData.hitByAttackData.knockbackDistance > 0;
    }

    protected override void OnPlayerUseSkill(PlayerGridUnit player, PlayerBaseSkill skill)
    {
        if (!hasMoved)
            hasMoved = !turnStartGridPos.ContainsKey(player) || !turnStartGridPos[player].All(player.GetGridPositionsOnTurnStart().Contains);
    }


    protected override void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        base.OnCombatEnd(battleResult, battleTrigger);

        if (battleResult == BattleResult.Victory && !hasMoved)
            BonusAchieved();
    }
}
