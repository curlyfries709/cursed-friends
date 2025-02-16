using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgMasteryTracker : BaseMasteryTracker
{
    Dictionary<PlayerGridUnit, List<GridPosition>> turnStartGridPos = new Dictionary<PlayerGridUnit, List<GridPosition>>();

    protected override void ListenToEvents(bool listen)
    {
        ListenToTurnStartEvents(listen);

        if (listen)
        {
            PlayerBaseSkill.PlayerUsedSkill += OnPlayerUsedSkill;
            Again.Instance.UnitGoingAgain += OnUnitGoAgain;
        }
        else
        {
            PlayerBaseSkill.PlayerUsedSkill -= OnPlayerUsedSkill;
            Again.Instance.UnitGoingAgain -= OnUnitGoAgain;
        }
    }

    private void ListenToTurnStartEvents(bool listen)
    {
        if(listen)
            turnStartGridPos.Clear();

        foreach (var item in allPlayersProgressionType)
        {
            if(allPlayersProgressionType[item.Key] != MasteryProgression.ProgressionType.Move) { continue; }

            if (listen)
            {
                turnStartGridPos[item.Key] = new List<GridPosition>();
                item.Key.BeginTurn += OnPlayerTurnStart;
            }
            else
            {
                item.Key.BeginTurn -= OnPlayerTurnStart;
            }
        }
    }

    private void OnPlayerUsedSkill(PlayerGridUnit player, PlayerBaseSkill skill)
    {
        if (allPlayersProgressionType[player] == MasteryProgression.ProgressionType.UseChain)
        {
            PlayerBaseChainAttack chainAttack = skill as PlayerBaseChainAttack;

            if (chainAttack)
            {
                playerCombatProgression[player] = playerCombatProgression[player] + 1;
            }
        }
        else if (allPlayersProgressionType[player] == MasteryProgression.ProgressionType.Move)
        {
            if (turnStartGridPos[player].Count == 0) //In This Scenario, a chain attack has been triggered with a unit who has set to have their first actual turn.
            {
                turnStartGridPos[player] = new List<GridPosition>(player.GetGridPositionsOnTurnStart());
            }

            GridPosition startPos = turnStartGridPos[player][0];
            GridPosition endpos = player.GetGridPositionsOnTurnStart()[0];

            int moveDistance = PathFinding.Instance.GetPathLengthInGridUnits(startPos, endpos, player);

            playerCombatProgression[player] = playerCombatProgression[player] + moveDistance;
        }
    }

    private void OnPlayerTurnStart()
    {
        PlayerGridUnit player = FantasyCombatManager.Instance.GetActiveUnit() as PlayerGridUnit;

        if (turnStartGridPos.ContainsKey(player))
        {
            turnStartGridPos[player] = new List<GridPosition>(player.GetGridPositionsOnTurnStart());
        }
    }

    private void OnUnitGoAgain(CharacterGridUnit goingAgainUnit)
    {
        PlayerGridUnit player = goingAgainUnit as PlayerGridUnit;

        if (player && allPlayersProgressionType[player] == MasteryProgression.ProgressionType.GoAgain)
        {
            playerCombatProgression[player] = playerCombatProgression[player] + 1;
        }
    }
}
