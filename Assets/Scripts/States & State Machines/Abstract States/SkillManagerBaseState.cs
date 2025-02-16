using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillManagerBaseState : State
{
    protected CombatSkillManager skillManager;
    protected SkillManagerBaseState(CombatSkillManager skillManager)
    {
        this.skillManager = skillManager;
    }

}
