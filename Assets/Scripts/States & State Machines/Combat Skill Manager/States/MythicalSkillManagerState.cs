using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MythicalSkillManagerState : SkillManagerBaseState
{
    public MythicalSkillManagerState(CombatSkillManager skillManager) : base(skillManager){}
    public override void EnterState()
    {
        //LISTEN TO LEVEL UP
        Victory.PlayerLevelledUp += OnPlayerLevelUp;
    }

    public override void UpdateState(){}

    public override void ExitState() 
    {
        //STOP LISTEN TO LEVEL UP
        Victory.PlayerLevelledUp -= OnPlayerLevelUp;
    }

    private LevelUpResult OnPlayerLevelUp(PartyMemberData player, int newLevel)
    {
        int pointsEarned = 1;

        PlayerSkillset playerLearnedSkills = PartyManager.Instance.GetPartyMemberLearnedSkill(player);

        //Earn 1 Skill point on new level
        playerLearnedSkills.EarnSkillPoint(pointsEarned);

        return new SkillPointEarned(player, pointsEarned);
    }


}
