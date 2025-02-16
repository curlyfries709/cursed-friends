using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUnitStats : UnitStats, ISaveable
{
    [Header("Player Data")]
    [SerializeField] PartyMemberData memberData;
    [Space(10)]
    [SerializeField] BeingData mythicalFormData;
    [SerializeField] BeingData humanFormData;

    PlayerStatsState attributeState = new PlayerStatsState();

    //Saving 
    bool isDataRestored = false;
    public void SetLevel(int newLevel)
    {
        level = newLevel;
    }

    private void OnEnable()
    {
        PartyManager.Instance.PlayerPartyDataSet += Setup;
    }

    public void Setup()
    {
        PlayerGridUnit myPlayerGridUnit = PartyManager.Instance.GetPlayerUnitViaName(memberData.memberName);
        equipment.SetWearer(myPlayerGridUnit);
        myPlayerGridUnit.SetPlayerUnitStats(this);
    }

    private void OnDisable()
    {
        PartyManager.Instance.PlayerPartyDataSet -= Setup;
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
    }

    public void OverrideBeingData(bool toMythicalForm)
    {
        data = toMythicalForm ? mythicalFormData : humanFormData;
    }

    public PartyMemberData GetPartyMemberData()
    {
        return memberData;
    }

    //SAVING
    [System.Serializable]
    public class PlayerStatsState
    {
        //Attribute
        public Dictionary<int, int> attributes = new Dictionary<int, int>();
    }


    public object CaptureState()
    {
        //Store Attributes
        foreach (int i in Enum.GetValues(typeof(Attribute)))
        {
            attributeState.attributes[i] = GetAttributeValueWithoutEquipmentBonuses((Attribute)i);
        }

        return attributeState;
    }

    public void RestoreState(object state)
    {
        isDataRestored = true;
        if(state == null)
        {
            return;
        }

        //Restore Attributes
        foreach (KeyValuePair<int, int> data in attributeState.attributes)
        {
            Attribute attribute = (Attribute)data.Key;
            RestoreAttribute(attribute, data.Value);
        }
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
