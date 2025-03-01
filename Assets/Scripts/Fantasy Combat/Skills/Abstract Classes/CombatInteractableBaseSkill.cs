using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CombatInteractableBaseSkill : BaseSkill, IHighlightable
{
    [Header("Stats")]
    [SerializeField] ObjectStats myStats;
    protected IRespawnable respawnable;

    //ABSTRACT
    protected abstract void SetRespawnable();

    public abstract Dictionary<GridPosition, IHighlightable> ActivateHighlightedUI(bool activate, PlayerBaseSkill selectedBySkill);
    //END ABSTRACT

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

    public Element GetAttackElement()
    {
        if (!myStats)
        {
            return Element.None;
        }

        return myStats.GetAttackElement();
    }
}
