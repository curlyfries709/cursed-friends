using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CombatInteractableBaseSkill : BaseSkill
{

    protected CharacterGridUnit myInteractor;

    protected abstract void TriggerSkill(CharacterGridUnit myInteractor);

    public void ShowAffectedGridPositions(bool show)
    {
        if (show)
        {
            CalculateSelectedGridPos();
        }

        GridSystemVisual.Instance.ShowSelectedObjectAOEVisual(selectedGridPositions, selectedUnits, show);
    }
}
