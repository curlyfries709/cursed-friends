using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class UnitStats : MonoBehaviour
{
    [Header("Profile")]
    public BeingData data;
    [Range(1, 99)]
    public int level = 1;
    [Header("Attributes")]
    [SerializeField] protected int baseStrength;
    [SerializeField] protected int baseFinesse;
    [SerializeField] protected int baseEndurance;
    [SerializeField] protected int baseAgility;
    [SerializeField] protected int baseIntelligence;
    [SerializeField] protected int baseWisdom;
    [SerializeField] protected int baseCharisma;
    [Header("Default Data")]
    [SerializeField] protected Equipment equipment;
    public Element defaultAttackElement;
    [Space(10)]
    [SerializeField] protected bool forceImmunity = false;

    //Current Attributes {Includes Equipment Bonuses}
    public int Strength { get; protected set; }
    public int Finesse { get; protected set; }
    public int Endurance { get; protected set; }
    public int Agility { get; protected set; }
    public int Intelligence{ get; protected set; }
    public int Wisdom { get; protected set; }
    public int Charisma{ get; protected set; }

    //Sub Attributes
    public int PhysAttack { get; protected set; }
    public int MagAttack { get; protected set; }

    public int Technique { get; protected set; }
    public int Evasion { get; protected set; }

    public int Vitality { get; protected set; }
    public int Stamina { get; protected set; }

    public int Speed { get; protected set; }
    public int Memory { get; protected set; }

    public int InventoryWeight { get; protected set; }
    public int HealEfficacy { get; protected set; }

    public int SEDuration { get; protected set; }
    //public int ScrollDuration { get; protected set; }

    public int CritChance { get; protected set; }
    public int StatusEffectInflictChance { get; protected set; }

    //protected int physAttackPadding = 0;
    //protected int magAttackPadding = 0;

    //Sub Attributes Buffs

    [HideInInspector] public float buffSTRmultiplier = 1;
    [HideInInspector] public float buffINTmultiplier = 1;

    [HideInInspector] public float buffARMmultiplier = 1;

    [HideInInspector] public float buffFINmultiplier = 1;

    [HideInInspector] public float buffAGmultiplier = 1;

    [HideInInspector] public float buffWISmultiplier = 1;

    [HideInInspector] public float buffCHRmultiplier = 1;

    
    //Sub Attribute Damage Blessings
    [HideInInspector] public float blessingDamageMultiplier = 1;
    [HideInInspector] public float blessingDamageReductionMultiplier = 1;

    //Other Sub Attribute Blessings
    [HideInInspector] public float blessingTechniqueMultiplier = 1;
    [HideInInspector] public float blessingEvasionMultiplier = 1;
    [HideInInspector] public float blessingSpeedMultiplier = 1;
    [HideInInspector] public float blessingHealEfficacyMultiplier = 1;
    //Increase
    [HideInInspector] public int blessingSEDurationIncrease = 0;
    [HideInInspector] public int blessingMovementIncrease = 0;

    [HideInInspector] public int blessingCritChanceIncrease = 0;
    [HideInInspector] public int blessingStatusEffectInflictChanceIncrease = 0;

    //Affinities
    Dictionary<Element, Affinity> defaultElementAffinities = new Dictionary<Element, Affinity>();

    public Dictionary<Element, Affinity> currentElementAffinities { get; protected set; }

    protected virtual void Awake()
    {
        currentElementAffinities = new Dictionary<Element, Affinity>();
    }

    private void Start()
    {
        SetData();
    }

    protected void SetData()
    {
        SetAffinities();
        UpdateSubAndMainAttributes();
    }

    //Current Stats
    private void UpdateAttributes()
    {
        Strength = baseStrength + equipment.GetEquipmentAttributeBonus(Attribute.Strength);
        Finesse = baseFinesse + equipment.GetEquipmentAttributeBonus(Attribute.Finesse);
        Agility = baseAgility + equipment.GetEquipmentAttributeBonus(Attribute.Agility);
        Endurance = baseEndurance + equipment.GetEquipmentAttributeBonus(Attribute.Endurance);
        Intelligence = baseIntelligence + equipment.GetEquipmentAttributeBonus(Attribute.Intelligence);
        Wisdom = baseWisdom + equipment.GetEquipmentAttributeBonus(Attribute.Wisdom);
        Charisma = baseCharisma + equipment.GetEquipmentAttributeBonus(Attribute.Charisma);

        if (TheCalculator.Instance.printStats)
        {
            Debug.Log(data.Key() + " STR: " + Strength.ToString());
            Debug.Log(data.Key() + " FIN: " + Finesse.ToString());
            Debug.Log(data.Key() + " AG: " + Agility.ToString());
            Debug.Log(data.Key() + " END: " + Endurance.ToString());
            Debug.Log(data.Key() + " INT: " + Intelligence.ToString());
            Debug.Log(data.Key() + " WIS: " + Wisdom.ToString());
            Debug.Log(data.Key() + " CHR: " + Charisma.ToString());
        }
    }



    private void UpdateSubStats()
    {
        //STR
        PhysAttack = TheCalculator.Instance.CalculatePhysAttack(Strength); //Buff Multiplier applied in raw damage calculation
        InventoryWeight = TheCalculator.Instance.CalculateInventoryCapacity(Mathf.RoundToInt(Strength * buffSTRmultiplier)) + equipment.GetEquipmentSubAttributeBonus(SubStats.InventoryWeight);

        //FIN
        Technique = Mathf.RoundToInt((TheCalculator.Instance.CalculateTechnique(Finesse) + equipment.GetEquipmentSubAttributeBonus(SubStats.Technique)) * buffFINmultiplier * blessingTechniqueMultiplier);
        Evasion = Mathf.RoundToInt((TheCalculator.Instance.CalculateEvasion(Finesse) + equipment.GetEquipmentSubAttributeBonus(SubStats.Evasion)) * buffFINmultiplier * blessingEvasionMultiplier);

        //END
        Vitality = TheCalculator.Instance.CalculateVitality(Endurance) + equipment.GetEquipmentSubAttributeBonus(SubStats.Vitality);
        Stamina = TheCalculator.Instance.CalculateStamina(Endurance) + equipment.GetEquipmentSubAttributeBonus(SubStats.Stamina);

        //AG
        Speed = Mathf.RoundToInt((TheCalculator.Instance.CalculateSpeed(Agility) + equipment.GetEquipmentSubAttributeBonus(SubStats.Speed)) * buffAGmultiplier * blessingSpeedMultiplier);

        //WIS
        HealEfficacy = Mathf.RoundToInt((TheCalculator.Instance.CalculateHealAmount(Wisdom) + equipment.GetEquipmentSubAttributeBonus(SubStats.HealEfficacy)) * buffWISmultiplier * blessingHealEfficacyMultiplier);
        //ScrollDuration = TheCalculator.Instance.CalculateScrollDuration(Mathf.RoundToInt(Wisdom * buffWISmultiplier)) + equipment.GetEquipmentSubAttributeBonus(SubStats.ScrollDuration);
        SEDuration = TheCalculator.Instance.CalculateSEDuration(Mathf.RoundToInt(Wisdom * buffWISmultiplier)) + equipment.GetEquipmentSubAttributeBonus(SubStats.StatusEffectDuration) + blessingSEDurationIncrease;

        //INT
        MagAttack = TheCalculator.Instance.CalculateMagAttack(Intelligence);//Buff Multiplier applied in raw damage calculation
        Memory = TheCalculator.Instance.CalculateMemory(Intelligence) + equipment.GetEquipmentSubAttributeBonus(SubStats.Memory);

        //CHR
        CritChance = Mathf.RoundToInt(((TheCalculator.Instance.CalculateCritChance(Charisma) + equipment.GetEquipmentSubAttributeBonus(SubStats.CritChance)) * buffCHRmultiplier) + blessingCritChanceIncrease);
        StatusEffectInflictChance = Mathf.RoundToInt(((TheCalculator.Instance.CalculateStatusEffectInflictChance(Charisma) + equipment.GetEquipmentSubAttributeBonus(SubStats.StatusEffectChance)) * buffCHRmultiplier) + blessingStatusEffectInflictChanceIncrease);

        if (TheCalculator.Instance.printStats)
        {
            Debug.Log(data.Key() + " PHYS ATTACK: " + PhysAttack.ToString());
            Debug.Log(data.Key() + " MAG ATTACK: " + MagAttack.ToString());
            Debug.Log(data.Key() + " EVASION: " + Evasion.ToString());
            Debug.Log(data.Key() + " HEAL: " + HealEfficacy.ToString());
            Debug.Log(data.Key() + " WEIGHT: " + InventoryWeight.ToString());
            //Debug.Log(data.Key() + " SCROLL DURATION: " + ScrollDuration.ToString());
            Debug.Log(data.Key() + " CRIT CHANCE: " + CritChance.ToString());
            Debug.Log(data.Key() + " SPEED: " + Speed.ToString());
        }

    }

    //Affinity Alteration


    public void AlterElementAffinity(ElementAffinity newAffinity)
    {
        currentElementAffinities[newAffinity.element] = newAffinity.affinity;
    }


    public void ResetElementAffinity(Element element)
    {
        currentElementAffinities[element] = defaultElementAffinities[element];
    }

    private void SetAffinities()//CALLED OUTSIDE COMBAT.
    {
        List<ElementAffinity> elementAffinities = new List<ElementAffinity>();

        if (equipment) 
        {
            elementAffinities = equipment.GetElementAlteration().Concat(data.elementAffinities).ToList();
        }
        else
        {
            elementAffinities = data.elementAffinities;
        }
         
        //Set Elements
        foreach (int i in Enum.GetValues(typeof(Element)))
        {
            if ((Element)i != Element.None)
            {
                Affinity affinity = Affinity.None;
                foreach (ElementAffinity elementAffinity in elementAffinities)
                {
                    //Is The Target Element a match
                    if ((Element)i == elementAffinity.element)
                    {
                        affinity = elementAffinity.affinity;
                        break;
                    }
                }

                currentElementAffinities[(Element)i] = affinity;
            }
        }

        defaultElementAffinities = currentElementAffinities.ToDictionary(entry => entry.Key, entry => entry.Value);
    }


    //Stat Changers
    public void UpdateBuffMultiplier(ref float buffMultiplier, float multiplierValue, bool removeBuff)
    {
        if (removeBuff)
        {
            buffMultiplier = buffMultiplier / multiplierValue;
        }
        else
        {
            buffMultiplier = buffMultiplier * multiplierValue;
        }
        
        UpdateSubStats();
    }

    public void UpdateBlessingMultiplier(out float multiplier, float newValue)
    {
        multiplier = newValue;
        UpdateSubStats();
    }

    public void UpdateBlessingAddition(out int increaser, int newValue)
    {
        increaser = newValue;
        UpdateSubStats();
    }

    public void UpdateSubAndMainAttributes()
    {
        UpdateAttributes();
        UpdateSubStats();

        if(equipment)
            equipment.AdjustWearerHealth();
    }

    //Equipment
    public void EquipWeapon(Weapon weapon, bool isRestore = false)
    {
        if (!isRestore)
        {
            if (!CanEquip(weapon) || weapon == equipment.Weapon())
            {
                return;
            }
        }

        equipment.ChangeWeapon(weapon);
        UpdateSubAndMainAttributes();
    }

    public void EquipArmour(Armour armour, bool isRestore = false)
    {
        if (!isRestore && !CanEquip(armour)) { return; }

        equipment.ChangeArmour(armour);
        UpdateSubAndMainAttributes();
        SetAffinities();
    }

    public bool CanUseItem(Item item)
    {
        return level >= item.requiredLevel;
    }

    public bool CanEquip(Weapon weapon)
    {
        return weapon.category == data.proficientWeaponCategory && CanUseItem(weapon);
    }

    public bool CanEquip(Armour armour)
    {
        return armour.raceRestriction.Contains(data.race) && CanUseItem(armour);
    }
    

    public bool IsEquipped(Item item)
    {
        if (!item) { return false; }

        return equipment.IsEquipped(item);
    }


    //Other Getters
    public int MovementBuff()
    {
        return blessingMovementIncrease + equipment.GetEquipmentSubAttributeBonus(SubStats.Movement);
    }

    public int GetAttributeValue(Attribute attribute)
    {
        switch (attribute)
        {
            case Attribute.Strength:
                return Strength;
            case Attribute.Finesse:
                return Finesse;
            case Attribute.Endurance:
                return Endurance;
            case Attribute.Agility:
                return Agility;
            case Attribute.Intelligence:
                return Intelligence;
            case Attribute.Wisdom:
                return Wisdom;
            default:
                return Charisma;
        }
    }

    public int GetAttributeValueWithoutEquipmentBonuses(Attribute attribute)
    {
        return GetAttributeValue(attribute) - equipment.GetEquipmentAttributeBonus(attribute);
    }

    public virtual int GetArmour()
    {
        int armour = equipment.Armour() ? equipment.Armour().armour : 0;
        return Mathf.RoundToInt(armour * buffARMmultiplier);
    }

    public int GetVitalityWithoutBonus()
    {
        return TheCalculator.Instance.CalculateVitality(Endurance - equipment.GetEquipmentAttributeBonus(Attribute.Endurance));
    }

    public int GetStaminaWithoutBonus()
    {
        return TheCalculator.Instance.CalculateStamina(Endurance - equipment.GetEquipmentAttributeBonus(Attribute.Endurance));
    }

    public bool HasRequiredAttributeValue(Attribute attribute, int requiredValue, bool includeEquipmentBonuses)
    {
        if (includeEquipmentBonuses)
        {
            return GetAttributeValue(attribute) >= requiredValue;
        }

        return GetAttributeValueWithoutEquipmentBonuses(attribute) >= requiredValue;
    }

    public Element GetAttackElement()
    {
        Weapon weapon = equipment.Weapon();

        if (weapon)
        {
            return weapon.element;
        }

        return defaultAttackElement;
    }

    public bool IsImmuneToForces()
    {
        return forceImmunity || equipment.HasForceImmunity();
    }

    public bool IsImmuneToStatusEffect(StatusEffectData statusEffect)
    {
        return data.statusEffectsNullified.Contains(statusEffect) || equipment.GetStatusEffectImmunity().Contains(statusEffect);
    }

    public Equipment Equipment()
    {
        return equipment;
    }
}
