

using System.Collections.Generic;

public interface IHighlightable 
{
    public Dictionary<GridPosition, IHighlightable> ActivateHighlightedUI(bool activate, PlayerBaseSkill selectedBySkill);

    public GridUnit GetGridUnit();
}
