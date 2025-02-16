using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class EventCondition 
{
    public ConditionType condition;
    [ShowIf("condition", ConditionType.Item)]
    public Item requiredItem;
    [ShowIf("condition", ConditionType.PastDecision)]
    public ChoiceReferences decisionReference;
    [ShowIf("condition", ConditionType.StatusEffect)]
    public StatusEffectData statusEffect;
    [ShowIf("condition", ConditionType.StatusEffect)]
    public StoryCharacter affectedCharacter;
    [ShowIf("condition", ConditionType.Talent)]
    [Range(1, 12)]
    public int minRequiredLevel = 1;
    [ShowIf("condition", ConditionType.Money)]
    public float requiredFunds = 0;
    [ShowIf("condition", ConditionType.Money)]
    public bool isFantasyMoney;
    [Space(10)]
    public bool boolToAchieveCondition = true;

    public enum ConditionType
    {
        PastDecision,
        Item,
        Money,
        StatusEffect,
        Talent
    }
}
