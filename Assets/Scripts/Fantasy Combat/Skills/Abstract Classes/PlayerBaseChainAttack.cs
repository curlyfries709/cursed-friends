using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class PlayerBaseChainAttack : PlayerOffensiveSkill
{
    Direction currentCalculatedDirection;
    protected PlayerGridUnit chainPasser;
    protected bool skillSelected = false;
    protected KnockdownEvent knockdownEvent;

    /*public List<GridUnit> DisplayAttackAOE(PlayerGridUnit chainPasser)
    {
        skillSelected = false;

        this.chainPasser = chainPasser;

        ShowMovementGridPos();

        CalculateAttackAffectedGridPositions();
        ShowSelectedSkillGridVisual();

        List<GridUnit> listToReturn = new List<GridUnit>(selectedUnits);
        listToReturn.Add(myUnit);

        return listToReturn;
    }*/

    protected virtual void CalculateAttackAffectedGridPositions()
    {
        if(RequireSkillDirectionSelection())
        {
            SetAffectedGridPositionsForAllDirections();
        }
        else
        {
            CalculateSelectedGridPos();
        }
    }

    private void SetAffectedGridPositionsForAllDirections()
    {
        int startingIndex = skillShape == SkillShape.Diagonal ? 4 : 0;
        List<GridPosition> affectedGridPositions = new List<GridPosition>();

        for (int i = startingIndex; i < startingIndex + 4; i++)
        {
            //Set Direction to calculate
            currentCalculatedDirection = (Direction)i;
            affectedGridPositions.AddRange(GetFilteredGridPosList());
        }

        affectedGridPositions = affectedGridPositions.Distinct().ToList();
        selectedGridPositions = affectedGridPositions;

        SetSelectedUnits();
    }

    protected void ShowMovementGridPos()
    {
        List<GridPosition> movementGridPos = new List<GridPosition>(myUnit.GetGridPositionsOnTurnStart());
        GridSystemVisual.Instance.ShowValidMovementGridPositions(movementGridPos, myUnit, true);
    }

    //Other Overrides
    public override bool TrySelectSkill()
    {
        //Must See If Valid Targets are Available.
        CalculateAttackAffectedGridPositions();

        if (selectedUnits.Count > 0)
        {
            skillSelected = true;
            skillTriggered = false;

            if (RequireSkillDirectionSelection())
            {
                HUDManager.Instance.UpdateSelectedSkill(skillName);
                FantasyCombatManager.Instance.BeginChainAttackAreaSelection(myUnit, this);
            }
            else
            {
                TryTriggerSkill();
            }

            return true;
        }

        return false;
    }

    public override void SkillCancelled()
    {
        skillSelected = false;
        HideSelectedSkillGridVisual();
        knockdownEvent.OnSelectedChainAttackCancelled();
    }

    public void SetKnockdownEvent(KnockdownEvent knockdownEvent)
    {
        this.knockdownEvent = knockdownEvent;
    }

    public bool HasValidTargets(PlayerGridUnit chainPasser)
    {
        this.chainPasser = chainPasser;
        CalculateAttackAffectedGridPositions();
        return selectedUnits.Count > 0;
    }

    private bool RequireSkillDirectionSelection()
    {
        return !(originateFromUnitCentre || skillShape == SkillShape.Cross);
    }

    //Direction Overrides.
    protected override Direction GetDirection()
    {
        if (skillSelected)
        {
            return base.GetDirection();
        }

        return currentCalculatedDirection;
    }

    protected override Direction GetDiagonalDirection()
    {
        if (skillSelected)
        {
            return base.GetDiagonalDirection();
        }

        return currentCalculatedDirection;
    }

    public List<GridPosition> GetAllShownGridPositions()
    {
        List<GridPosition> listToReturn = new List<GridPosition>(selectedGridPositions);
        listToReturn.AddRange(myUnit.GetGridPositionsOnTurnStart());

        return listToReturn;
    }
}
