using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public enum TargetingBehaviour
{
    Random,
    ClosestUnit,
    SameRandomUnitTillKo,
    UnitWithLowestCurrentHealth,
    UnitWithHighestCurrentHealth,
    UnitWithLowestAttribute,
    UnitWithHighestAttribute,
    SpecificCharacter,
    SpecificRace,
    MaximumUnitsWitthAreaSkills
}

public enum PositioningBehaviour
{
    ClosestPositionToStartingPos,
    FurthestPositionFromTarget,
    Random
}


namespace AnotherRealm
{
    public class AICombatEvaluation : MonoBehaviour
    {
        public static GridUnit GetPriorityTarget(CharacterGridUnit unitThatRequiresTarget, FantasyCombatTarget preferredTargetType, TargetingBehaviour targetingBehaviour, List<StatusEffectTarget> statusEffectsToPrioritise, List<StatusEffectTarget> statusEffectsToAvoid, Attribute targetAttribute, Race targetRace, string targetName)
        {
            //Filter By Preferred Target Type.
            List<GridUnit> allEligbleUnits = CombatFunctions.GetEligibleTargets(unitThatRequiresTarget, preferredTargetType);

            //Filter By Status Effects To Avoid.
            allEligbleUnits = FilterListByStatusEffects(unitThatRequiresTarget, allEligbleUnits, statusEffectsToAvoid, false);

            //Filter By Status Effects To Prioritse  If list empty return Original List.
            allEligbleUnits = FilterListByStatusEffects(unitThatRequiresTarget, allEligbleUnits, statusEffectsToPrioritise, true);

            //Filter By Behaviour //Can Return null
            GridUnit target = GetTargetBasedOnBehaviour(unitThatRequiresTarget, allEligbleUnits, targetingBehaviour, targetAttribute, targetRace, targetName);

            return target;
        }



        private static GridUnit GetTargetBasedOnBehaviour(CharacterGridUnit unitThatRequiresTarget, List<GridUnit> eligibleTargets, TargetingBehaviour targetingBehaviour, Attribute targetAttribute, Race targetRace, string targetName)
        {
            switch (targetingBehaviour)
            {
                case TargetingBehaviour.ClosestUnit:
                    return CombatFunctions.GetClosestUnit(eligibleTargets, unitThatRequiresTarget.transform);
                case TargetingBehaviour.SpecificRace:
                    List<GridUnit> filteredList = eligibleTargets.Where(((unit) => unit.stats.data.race == targetRace)).ToList();
                    return GetRandomUnit(filteredList);
                case TargetingBehaviour.SpecificCharacter:
                    return eligibleTargets.FirstOrDefault((unit) => unit.unitName == targetName);
                case TargetingBehaviour.UnitWithLowestCurrentHealth:
                    return eligibleTargets.OrderBy((unit) => unit.Health().currentHealth).FirstOrDefault();
                case TargetingBehaviour.UnitWithHighestCurrentHealth:
                    return eligibleTargets.OrderByDescending((unit) => unit.Health().currentHealth).FirstOrDefault();
                case TargetingBehaviour.UnitWithLowestAttribute:
                    return eligibleTargets.OrderBy((unit) => unit.stats.GetAttributeValue(targetAttribute)).FirstOrDefault();
                case TargetingBehaviour.UnitWithHighestAttribute:
                    return eligibleTargets.OrderByDescending((unit) => unit.stats.GetAttributeValue(targetAttribute)).FirstOrDefault();
                default:
                    //Random Unit
                    return GetRandomUnit(eligibleTargets);
            }
        }


        private static GridUnit GetRandomUnit(List<GridUnit> eligibleTargets)
        {
            if(eligibleTargets.Count == 0) { return null; }

            int randIndex = Random.Range(0, eligibleTargets.Count);
            return eligibleTargets[randIndex];
        }

        private static List<GridUnit> FilterListByStatusEffects(CharacterGridUnit skillOwner, List<GridUnit> listToFilter, List<StatusEffectTarget> statusEffectTargetingData, bool isPrioritizeList)
        {
            List<GridUnit> returnList = new List<GridUnit>();

            foreach (CharacterGridUnit unit in listToFilter)
            {
                if (!unit) { continue; }

                foreach (StatusEffectTarget SETargetData in statusEffectTargetingData)
                {
                    if (returnList.Contains(unit))
                    {
                        break;
                    }

                    bool hasStatusEffect = StatusEffectManager.Instance.UnitHasStatusEffect(unit, SETargetData.statusEffect);
                    bool isValidTarget = CombatFunctions.IsUnitValidTarget(SETargetData.unitType, skillOwner, unit);

                    if ((isPrioritizeList && hasStatusEffect && isValidTarget) || (!isPrioritizeList && !hasStatusEffect) || (!isPrioritizeList && hasStatusEffect && !isValidTarget))
                    {
                        returnList.Add(unit);
                    }
                }
            }

            if(returnList.Count == 0)
            {
                return listToFilter;
            }

            return returnList;
        }
    }

    [System.Serializable]
    public class StatusEffectTarget
    {
        public StatusEffectData statusEffect;
        public FantasyCombatTarget unitType;
    }
}

