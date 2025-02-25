using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CombatInteractableBaseSkill : BaseSkill
{
    protected CharacterGridUnit myInteractor;


    public abstract void TriggerSkill(CharacterGridUnit myInteractor);
}
