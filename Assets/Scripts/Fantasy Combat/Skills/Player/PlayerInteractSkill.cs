
using UnityEngine;

public class PlayerInteractSkill : PlayerBaseSkill
{
    CombatDirectInteractable selectedInteractable = null;

    public override bool TryTriggerSkill()
    {
        if (InteractionManager.Instance.HandleInteraction(true))
        {
            BeginSkill(0.25f, 0, false);
        }

        return false;
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        throw new System.NotImplementedException();
    }

    public override void SkillCancelled(bool showActionMenu = true)
    {
        base.SkillCancelled(showActionMenu);
        selectedInteractable = null;
    }

    protected override void SetSelectedUnits()
    {
        highlightableData.Clear();

        bool foundInteractable = false;

        foreach (GridPosition gridPosition in selectedGridPositions)
        {
            if(LevelGrid.Instance.TryGetObstacleAtPosition(gridPosition, out Collider obstacleData))
            {
                CombatDirectInteractable interactable = obstacleData.transform.parent.GetComponentInChildren<CombatDirectInteractable>();

                if (interactable && InteractionManager.Instance.CanInteractWith(interactable))
                {
                    foundInteractable = true;

                    if (selectedInteractable != interactable)
                    {
                        selectedInteractable = interactable;
                        selectedInteractable?.GetInteractableSkill().SetInteractorData(myCharacter, myUnitMoveTransform);
                        highlightableData[gridPosition] = selectedInteractable?.GetInteractableSkill().GetHighlightable(); 
                    }
                }
            }
        }

        if (!foundInteractable)
        {
            selectedInteractable = null;
        }
    }

    protected override bool IsGridPositionValid(GridPosition gridPosition)
    {
        bool isValidGridPos = LevelGrid.Instance.gridSystem.IsValidGridPosition(gridPosition);

        if(!isValidGridPos)
        {
            return false;
        }

        if(LevelGrid.Instance.TryGetObstacleAtPosition(gridPosition, out Collider obstacleData))
        {
            CombatDirectInteractable interactable = obstacleData.transform.parent.GetComponentInChildren<CombatDirectInteractable>();
            return interactable && interactable.IsInteractionEnabled();
        }

        GridUnit unit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

        if (unit)
        {
            CombatDirectInteractable interactable = unit.transform.parent.GetComponentInChildren<CombatDirectInteractable>();
            return interactable && interactable.IsInteractionEnabled();
        }

        return false;
    }

    protected override bool ShowInteractCanvasWhileSkillSelected()
    {
        return true;
    }

    protected override ICombatAction GetSkillAction()
    {
        return null;
    }
}
