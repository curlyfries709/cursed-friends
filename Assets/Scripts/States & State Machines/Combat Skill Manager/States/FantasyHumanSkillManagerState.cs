using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FantasyHumanSkillManagerState : SkillManagerBaseState
{
    public FantasyHumanSkillManagerState(CombatSkillManager skillManager) : base(skillManager){}

    public override void EnterState()
    {
        Victory.PlayerLevelledUp += OnPlayerLevelUp;
    }

    public override void UpdateState(){}
    public override void ExitState()
    {
        Victory.PlayerLevelledUp -= OnPlayerLevelUp;
    }

    private LevelUpResult OnPlayerLevelUp(PartyMemberData player, int newLevel)
    {
        /* 
         Return unlocked skills
         */

        return new SkillEarned(player);
    }

}
