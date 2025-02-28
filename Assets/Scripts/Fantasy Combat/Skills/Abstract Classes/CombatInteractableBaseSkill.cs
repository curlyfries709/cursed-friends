using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CombatInteractableBaseSkill : BaseSkill, IHighlightable
{
    protected IRespawnable respawnable;

    //ABSTRACT
    protected abstract void SetRespawnable();

    public abstract void ActivateHighlightedUI(bool activate, PlayerBaseSkill selectedBySkill);
    //END ABSTRACT

    protected void ShowAffectedGridPositions(bool show)
    {
        if (show)
        {
            CalculateSelectedGridPos();
            GridSystemVisual.Instance.ShowGridVisuals(null, selectedGridPositions, highlightableData, GridSystemVisual.VisualType.ObjectAOE);
        }
        else
        {
            GridSystemVisual.Instance.HideGridVisualsOfType(GridSystemVisual.VisualType.ObjectAOE);
        }
    }

    public void OnInteractableDestroyed()
    {
        SetRespawnable();

        if (respawnable != null)
        {
            respawnable.OnRemovedFromRealm();
        }
    }

    public GridUnit GetGridUnit()
    {
        return myUnit;
    }

    public IHighlightable GetHighlightable()
    {
        return this;
    }
}
