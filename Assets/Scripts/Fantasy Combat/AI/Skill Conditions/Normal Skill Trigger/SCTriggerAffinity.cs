using System.Collections.Generic;
using UnityEngine;

public class SCTriggerAffinity : SkillTriggerCondition
{
    [SerializeField] List<Affinity> affinitiesToIgnore = new List<Affinity>();

    public override bool IsConditionMet(GridUnit attacker, GridUnit target, BaseSkill skill)
    {
        if (skill is IOffensiveSkill offensiveSkill)
        {
            Element skillElement = offensiveSkill.GetSkillElement();
            
            if(attacker is PlayerGridUnit) //Check data base and see if data unlocked for element
            {
                bool isAffinityUnlocked = EnemyDatabase.Instance.IsAffinityUnlocked(target as CharacterGridUnit, skillElement);

                if(!isAffinityUnlocked)
                {
                    return false;
                }
            }

            Affinity affinity = TheCalculator.Instance.GetAffinity(target, skillElement, offensiveSkill.GetSkillAttackItem());
            return !affinitiesToIgnore.Contains(affinity);
        }

        return true;
    }
}
