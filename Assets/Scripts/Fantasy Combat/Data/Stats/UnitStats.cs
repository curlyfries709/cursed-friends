using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

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
    [Header("Overrides")]
    [SerializeField] protected bool hasForceImmunity = false;
    [Tooltip("Should a hit chance be used over finesse?")]
    [SerializeField] protected bool useHitChance = false;
    [ShowIf("useHitChance")]
    [SerializeField] protected int hitChance = 0;

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
        Strength = baseStrength + (equipment ? equipment.GetEquipmentAttributeBonus(Attribute.Strength) : 0);
        Finesse = baseFinesse + (equipment ? equipment.GetEquipmentAttributeBonus(Attribute.Finesse) : 0);
        Agility = baseAgility + (equipment ? equipment.GetEquipmentAttributeBonus(Attribute.Agility) : 0);
        Endurance = baseEndurance + (equipment ? equipment.GetEquipmentAttributeBonus(Attribute.Endurance) : 0);
        Intelligence = baseIntelligence + (equipment ? equipment.GetEquipmentAttributeBonus(Attribute.Intelligence) : 0);
        Wisdom = baseWisdom + (equipment ? equipment.GetEquipmentAttributeBonus(Attribute.Wisdom) : 0);
        Charisma = baseCharisma + (equipment ? equipment.GetEquipmentAttributeBonus(Attribute.Charisma) : 0);

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
        InventoryWeight = TheCalculator.Instance.CalculateInventoryCapacity(Mathf.RoundToInt(Strength * buffSTRmultiplier)) + (equipment ? equipment.GetEquipmentSubAttributeBonus(SubStats.InventoryWeight) : 0);

        //FIN
        Technique = Mathf.RoundToInt((TheCalculator.Instance.CalculateTechnique(Finesse) + (equipment ? equipment.GetEquipmentSubAttributeBonus(SubStats.Technique) : 0)) * buffFINmultiplier * blessingTechniqueMultiplier);
        Evasion = Mathf.RoundToInt((TheCalculator.Instance.CalculateEvasion(Finesse) + (equipment ? equipment.GetEquipmentSubAttributeBonus(SubStats.Evasion) : 0)) * buffFINmultiplier * blessingEvasionMultiplier);

        //END
        Vitality = TheCalculator.Instance.CalculateVitality(Endurance) + (equipment ? equipment.GetEquipmentSubAttributeBonus(SubStats.Vitality) : 0);
        Stamina = TheCalculator.Instance.CalculateStamina(Endurance) + (equipment ? equipment.GetEquipmentSubAttributeBonus(SubStats.Stamina) : 0);

        //AG
        Speed = Mathf.RoundToInt((TheCalculator.Instance.CalculateSpeed(Agility) + (equipment ? equipment.GetEquipmentSubAttributeBonus(SubStats.Speed) : 0)) * buffAGmultiplier * blessingSpeedMultiplier);

        //WIS
        HealEfficacy = Mathf.RoundToInt((TheCalculator.Instance.CalculateHealAmount(Wisdom) + (equipment ? equipment.GetEquipmentSubAttributeBonus(SubStats.HealEfficacy) : 0)) * buffWISmultiplier * blessingHealEfficacyMultiplier);
        //ScrollDuration = TheCalculator.Instance.CalculateScrollDuration(Mathf.RoundToInt(Wisdom * buffWISmultiplier)) + equipment.GetEquipmentSubAttributeBonus(SubStats.ScrollDuration);
        SEDuration = TheCalculator.Instance.CalculateSEDuration(Mathf.RoundToInt(Wisdom * buffWISmultiplier)) + (equipment ? equipment.GetEquipmentSubAttributeBonus(SubStats.StatusEffectDuration) : 0) + blessingSEDurationIncrease;

        //INT
        MagAttack = TheCalculator.Instance.CalculateMagAttack(Intelligence);//Buff Multiplier applied in raw damage calculation
        Memory = TheCalculator.Instance.CalculateMemory(Intelligence) + (equipment ? equipment.GetEquipmentSubAttributeBonus(SubStats.Memory) : 0);

        //CHR
        CritChance = Mathf.RoundToInt(((TheCalculator.Instance.CalculateCritChance(Charisma) + (equipment ? equipment.GetEquipmentSubAttributeBonus(SubStats.CritChance) : 0)) * buffCHRmultiplier) + blessingCritChanceIncrease);
        StatusEffectInflictChance = Mathf.RoundToInt(((TheCalculator.Instance.CalculateStatusEffectInflictChance(Charisma) + (equipment ? equipment.GetEquipmentSubAttributeBonus(SubStats.StatusEffectChance) : 0)) * buffCHRmultiplier) + blessingStatusEffectInflictChanceIncrease);

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

    protected void SetAffinities()//CALLED OUTSIDE COMBAT.
    {
        List<ElementAffinity> elementAffinities = new List<ElementAffinity>();

        if (equipment) 
        {
            elementAffinities = equipment.GetElementAlteration().Concat(GetDefaultElementAffinities()).ToList();
        }
        else
        {
            elementAffinities = GetDefaultElementAffinities();
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

    protected virtual List<ElementAffinity> GetDefaultElementAffinities()
    {
        return data.elementAffinities;
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
        return blessingMovementIncrease + (equipment ? equipment.GetEquipmentSubAttributeBonus(SubStats.Movement) : 0);
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
        return hasForceImmunity || equipment.HasForceImmunity();
    }

    public bool IsImmuneToStatusEffect(StatusEffectData statusEffect)
    {
        return data.statusEffectsNullified.Contains(statusEffect) || equipment.GetStatusEffectImmunity().Contains(statusEffect);
    }

    public bool UseHitChance(out int hitChance)
    {
        hitChance = this.hitChance;

        return useHitChance;
    }

    public Equipment Equipment()
    {
        return equipment;
    }
}
