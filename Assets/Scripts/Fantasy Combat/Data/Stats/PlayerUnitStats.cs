using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUnitStats : UnitStats
{
    [Header("Player Data")]
    [SerializeField] BeingData mythicalFormData;
    [SerializeField] BeingData humanFormData;

    //Dictionary<Attribute, int> stats = new Dictionary<Attribute, int>();
    //Dictitonary of SubStat
    public void SetLevel(int newLevel)
    {
        level = newLevel;
    }

    public void ImproveAttribute(Attribute attribute, int increase)
    {
        switch (attribute)
        {
            case Attribute.Strength:
                baseStrength = baseStrength + increase;
                break;
            case Attribute.Finesse:
                baseFinesse = baseFinesse + increase;
                break;
            case Attribute.Endurance:
                baseEndurance = baseEndurance + increase;
                break;
            case Attribute.Agility:
                baseAgility = baseAgility + increase;
                break;
            case Attribute.Intelligence:
                baseIntelligence = baseIntelligence + increase;
                break;
            case Attribute.Wisdom:
                baseWisdom = baseWisdom + increase;
                break;
            default:
                baseCharisma = baseCharisma + increase;
                break;
        }

        UpdateSubAndMainAttributes();
    }

    public void RestoreAttribute(Attribute attribute, int newValue)
    {
        switch (attribute)
        {
            case Attribute.Strength:
                baseStrength = newValue;
                break;
            case Attribute.Finesse:
                baseFinesse = newValue;
                break;
            case Attribute.Endurance:
                baseEndurance = newValue;
                break;
            case Attribute.Agility:
                baseAgility = newValue;
                break;
            case Attribute.Intelligence:
                baseIntelligence = newValue;
                break;
            case Attribute.Wisdom:
                baseWisdom = newValue;
                break;
            default:
                baseCharisma = newValue;
                break;
        }

        UpdateSubAndMainAttributes();
    }

    public void OverrideBeingData(bool toMythicalForm)
    {
        data = toMythicalForm ? mythicalFormData : humanFormData;
    }
}
