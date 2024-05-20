using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AnotherRealm
{
    public class HandyFunctions
    {

        public static string GetAttributeAbbreviation(Attribute attribute)
        {
            switch (attribute)
            {
                case Attribute.Strength:
                    return "STR";
                case Attribute.Finesse:
                    return "FIN";
                case Attribute.Endurance:
                    return "END";
                case Attribute.Agility:
                    return "AG";
                case Attribute.Intelligence:
                    return "INT";
                case Attribute.Wisdom:
                    return "WIS";
                default:
                    return "CHR";      
            }
        }

        public static void Scroll(ScrollRect scrollRect, bool up)
        {
            if (up)
            {
                scrollRect.velocity = Vector2.down * GameManager.Instance.uiScrollSpeed;
            }
            else
            {
                scrollRect.velocity = Vector2.up * GameManager.Instance.uiScrollSpeed;
            }
        }


        public static bool CanUnlockConditionalEvent(List<EventCondition> eventConditions)
        {
            foreach (EventCondition condition in eventConditions)
            {
                if (EvaluateEventCondition(condition) == true)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public static bool EvaluateEventCondition(EventCondition condition)
        {
            EventCondition.ConditionType conditionType = condition.condition;

            switch (conditionType)
            {
                case EventCondition.ConditionType.PastDecision:
                    if (condition.boolToAchieveCondition == StoryManager.Instance.MadeDecision(condition.decisionReference)) { return true; }
                    return false;
                case EventCondition.ConditionType.StatusEffect:
                    if (condition.boolToAchieveCondition ==
                        StatusEffectManager.Instance.UnitHasStatusEffect(PartyData.Instance.GetPlayerUnitViaName(condition.affectedCharacter.characterName), condition.statusEffect)) { return true; }
                    return false;
                case EventCondition.ConditionType.Money:
                    if (condition.boolToAchieveCondition == InventoryManager.Instance.CanAfford(condition.requiredFunds, condition.isFantasyMoney)) { return true; }
                    return false;
                case EventCondition.ConditionType.Item:
                    if (condition.boolToAchieveCondition == InventoryManager.Instance.HasItem(condition.requiredItem)) { return true; }
                    return false;
                default:
                    return false;

            }
        }

    }
}
