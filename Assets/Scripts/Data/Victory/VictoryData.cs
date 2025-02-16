
using System.Collections.Generic;

public class LevelUpResult
{
    public LevelUpResult(PartyMemberData partyMember)
    {
        associatedPartyMember = partyMember;
    }

    public PartyMemberData associatedPartyMember;
}

public class SkillPointEarned : LevelUpResult
{
    public SkillPointEarned(PartyMemberData partyMember, int skillPointEarned) : base(partyMember)
    {
        this.skillPointEarned = skillPointEarned;
    }

    public int skillPointEarned;
}

public class SkillEarned : LevelUpResult
{
    public SkillEarned(PartyMemberData partyMember) : base(partyMember){}

    public List<PlayerSkillData> learnedSkill = new List<PlayerSkillData>();
}