using AnotherRealm;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITeamSkill
{

    //ABSTRACT
    public ITeamSkill ITeamSkill();
    public List<CharacterGridUnit> GetAttackers();

    public List<GridPosition> GetAllyGridPositionsFromSkillOwnerCurrentPosition();
    //END ABSTRACT

    public bool CanSkillOwnerTriggerTeamSkill(CharacterGridUnit skillOwner)
    {
        return !StatusEffectManager.Instance.IsAfflictedWithNegativeEffect(skillOwner);
    }

    public bool CanTriggerTeamSkill(CharacterGridUnit skillOwner, List<string> requiredAllyNames)
    {
        if (!CanSkillOwnerTriggerTeamSkill(skillOwner)) //Check Skill Owner status
        {
            return false;
        }

        foreach (GridPosition gridPosition in GetAllyGridPositionsFromSkillOwnerCurrentPosition())
        {
            GridUnit unitAtPos = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
        
            if (!unitAtPos)
            {
                return false;
            }

            if (!IsValidAlly(skillOwner, unitAtPos, requiredAllyNames))
            {
                return false;
            }
        }

        return true;
    }

    public bool IsValidAlly(CharacterGridUnit skillOwner, GridUnit ally, List<string> requiredAllyNames)
    {
        //Ally must be character
        if(!(ally is CharacterGridUnit))
        {
            return false;
        }

        //CHECK TEAM RELATION BETWEEN SKILL OWRNER AND UNIT AT POS
        if (CombatFunctions.GetRelationWithTarget(skillOwner, ally) != FantasyCombatTarget.Ally)
        {
            return false;
        }

        //If any required allies, check they are one
        if (requiredAllyNames.Count > 0 && !requiredAllyNames.Contains(ally.unitName))
        {
            return false;
        }

        //Check they're not under negative status effect and not KOed.
        if (ally.Health().isKOed || StatusEffectManager.Instance.IsAfflictedWithNegativeEffect(ally))
        {
            return false;
        }

        return true;
    }

}
