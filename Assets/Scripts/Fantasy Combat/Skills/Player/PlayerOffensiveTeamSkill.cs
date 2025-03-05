using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class PlayerOffensiveTeamSkill : PlayerOffensiveSkill, ITeamSkill
{
    [Title("TEAM SKILL DATA")]
    [SerializeField] protected Transform allyRelativeGridPositionHeader;
    [Space(10)]
    [Tooltip("List the required members needed for this skill. If can team with any ally, leave empty")]
    [SerializeField] protected List<PartyMemberData> requiredAllies = new List<PartyMemberData>();

    protected List<CharacterGridUnit> selectedAllies = new List<CharacterGridUnit>();
    protected List<GridPosition> selectedAllyGridPositions = new List<GridPosition>();


    /*public override bool TryTriggerSkill()
    {
    }*/

    protected override void CalculateSelectedGridPos()
    {
        selectedAllyGridPositions = GetGridPositionsFromRelativePositionOfSkillOwner(allyRelativeGridPositionHeader);
        base.CalculateSelectedGridPos(); //This calls SetSelectedUnits. So do ally logic before this
    }

    protected override void SetSelectedUnits()
    {
        base.SetSelectedUnits();

        selectedAllies.Clear();
        //Set Selected Allies
        foreach (GridPosition gridPosition in selectedAllyGridPositions)
        {
            GridUnit foundUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

            if (ITeamSkill().IsValidAlly(myCharacter, foundUnit, GetRequiredAllyNames()))
            {
                selectedAllies.Add(foundUnit as CharacterGridUnit);
                highlightableData[gridPosition] = foundUnit.GetHighlightable(); 
            }
        }
    }

    protected override bool CanTriggerSkill(bool requiresUnitSelection)
    {
        bool canUnitTrigger = base.CanTriggerSkill(requiresUnitSelection); //Checks if skill owner in valid grid pos and valid units selected. 

        if (!canUnitTrigger)
        {
            return false;
        }

        //Then check self and all allies can afford cost
        bool canSkillOwnwerAfford = base.CanAffordSkill();

        if (!canSkillOwnwerAfford)
        {
            return false;
        }

        foreach (CharacterGridUnit ally in selectedAllies)
        {
            if (!CanPlayerAffordSkill(ally))
            {
                return false;
            }
        }

        //Check that allies are valid and self nor allies are affected by status effect.

        return ITeamSkill().CanTriggerTeamSkill(myCharacter, GetRequiredAllyNames());
    }

    public override bool CanAffordSkill()
    {
        bool canSkillOwnwerAfford = base.CanAffordSkill(); 

        if (!canSkillOwnwerAfford || requiredAllies.Count == 0)
        {
            return canSkillOwnwerAfford;
        }

        //Check active party and see if they are one of required allies & can afford
        foreach(PlayerGridUnit ally in PartyManager.Instance.GetActivePlayerParty())
        {
            if(!requiredAllies.Contains(ally.partyMemberData) || !CanPlayerAffordSkill(ally))
            {
                return false;
            }
        }

        //If reached here, then they can all afford
        return true;
    }

    protected override void SpendSkillCost(CharacterGridUnit character)
    {
        base.SpendSkillCost(character); //Spend Skill Owner

        foreach(CharacterGridUnit ally in selectedAllies)
        {
            base.SpendSkillCost(ally); //Spend each ally cost
        }
    }

    protected override void ResetData()
    {
        base.ResetData();
        selectedAllies.Clear();
        selectedAllyGridPositions.Clear();
    }

    //GETTERS
    public ITeamSkill ITeamSkill()
    {
        return this;
    }

    public List<CharacterGridUnit> GetAttackers()
    {
        List<CharacterGridUnit> attackers = new List<CharacterGridUnit>(selectedAllies)
        {
            myCharacter
        };

        return attackers;
    }

    public List<GridPosition> GetAllyGridPositionsFromSkillOwnerCurrentPosition()
    {
        return selectedAllyGridPositions;
    }

    public List<string> GetRequiredAllyNames()
    {
        return requiredAllies.ConvertAll((data) => data.memberName);
    }
}
