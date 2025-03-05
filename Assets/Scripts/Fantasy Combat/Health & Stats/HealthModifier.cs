using Sirenix.Utilities;
using System.Collections.Generic;
using UnityEngine;

public class AttackData : HealthChangeData
{
    //Elements
    public Element attackElement = Element.None;
    public Item attackItem = null;

    //Damage Data
    public PowerGrade powerGrade = PowerGrade.C;

    public int numOfTargets = 1;

    //Bools
    public bool isPhysical = false;
    public bool isMultiAction = false; //Does this skill attack multiple units individually

    //Can bools
    public bool canEvade = true;
    public bool canCrit = true;

    //HIT VFX
    public GameObject hitVFX = null;
    public Vector3 hitVFXPos;

    //CONSTUCTORS
    public AttackData(AttackData attackData)
    {
        mainInstigator = attackData.mainInstigator;
        
        attackElement = attackData.attackElement;
        attackItem = attackData.attackItem;

        powerGrade = attackData.powerGrade;
        HPChange = attackData.HPChange;

        numOfTargets = attackData.numOfTargets;
        isPhysical = attackData.isPhysical;
        isMultiAction = attackData.isMultiAction;

        canEvade = attackData.canEvade;
        canCrit = attackData.canCrit;

        //Inherited values
        appliedModifiers = attackData.appliedModifiers;
        inflictedStatusEffects = attackData.inflictedStatusEffects;
        forceData = attackData.forceData;

        isCritical = attackData.isCritical;
    }

    public AttackData(GridUnit attacker, int rawDamage)
    {
        this.mainInstigator = attacker;
        HPChange = rawDamage;
    }

    public AttackData(GridUnit attacker, Element attackElement, int rawDamage, int numOfTargets)
    {
        this.mainInstigator = attacker;
        this.attackElement = attackElement;
        HPChange = rawDamage;
        this.numOfTargets = numOfTargets;
    }
    public AttackData(GridUnit attackerThatTriggeredAttack, List<GridUnit> supportAttackers, Element attackElement, int rawDamage, int numOfTargets)
    {
        this.mainInstigator = attackerThatTriggeredAttack;
        SetSupportInstigators(supportAttackers);
        this.attackElement = attackElement;
        HPChange = rawDamage;
        this.numOfTargets = numOfTargets;
    }

    public AttackData(GridUnit attacker, Item attackItem, int rawDamage, int numOfTargets)
    {
        this.mainInstigator = attacker;
        this.attackItem = attackItem;
        HPChange = rawDamage;
        this.numOfTargets = numOfTargets;
    }
}

public class DamageData : HealthChangeData
{
    public Health targetHealth;

    public Affinity affinityToAttack = Affinity.None;
    public AttackData hitByAttackData = null;
    public DamageType damageType = DamageType.Default;

    //Bools
    public bool isBackstab = false;
    public bool isKOHit = false;
    public bool isTargetGuarding = false;
    public bool isKnockdownHit = false;

    public DamageData(GridUnit target, GridUnit attacker, AttackData hitByAttackData)
    {
        this.target = target;
        this.mainInstigator = this.mainInstigator = attacker;
        this.hitByAttackData = hitByAttackData;

        targetHealth = target?.Health();
    }

    public DamageData(GridUnit target, GridUnit attacker, Affinity affinityToAttack, int damageReceived)
    {
        this.target = target;
        this.mainInstigator = attacker;

        this.affinityToAttack = affinityToAttack;
        HPChange = damageReceived;

        targetHealth = target?.Health();
    }

    public DamageData(GridUnit target, GridUnit attacker, Affinity affinityToAttack, int damageReceived, AttackData hitByAttackData)
    {
        this.target = target;
        this.mainInstigator = attacker;

        this.affinityToAttack = affinityToAttack;
        HPChange = damageReceived;

        this.hitByAttackData = hitByAttackData;

        targetHealth = target?.Health();
    }

    public void Clear(bool clearAttacker = true, bool clearTarget = true)
    {
        if (clearTarget)
        {
            target = null;
            targetHealth = null;
        }

        if (clearAttacker)
        {
            mainInstigator = null;
            supportInstigators.Clear();
        }

        affinityToAttack = Affinity.None;
        hitByAttackData = null;
        damageType = DamageType.Default;

        HPChange = 0;
        SPChange = 0;
        inflictedStatusEffects.Clear();

        //Bools
        isBackstab = false;
        isCritical = false; 
        isKOHit = false;
        isTargetGuarding = false;
    }
}

public enum DamageType
{
    Default,
    StatusEffect,
    KnockbackBump,
    Reflect,
    Ultimate //This applies for Duofires, beatdowns & Power Of Friendship
}

public class HealData: HealthChangeData //If instigator null, then source of healing must be via item E.G Potion, Blessing.
{
    public int FPChange = 0;

    //Bools
    public bool canRevive = false;
    public bool convertToDamage = false;

    public HealData(GridUnit target, int HPRestore)
    {
        this.target = target;
        HPChange = HPRestore;
    }

    public HealData(GridUnit target, int HPRestore, int SPRestore, int FPRestore, bool canRevive)
    {
        this.target = target;
        HPChange = HPRestore;
        SPChange = SPRestore;
        FPChange = FPRestore;
        this.canRevive = canRevive;
    }

    public HealData(GridUnit target, CharacterGridUnit healer, int HPRestore, bool canRevive)
    {
        this.target = target;
        this.mainInstigator = healer;
        HPChange = HPRestore;
        this.canRevive = canRevive;
    }

    public bool IsOnlyFPRestore()
    {
        return FPChange > 0 && HPChange == 0 && SPChange == 0;
    }
}

public abstract class HealthChangeData //Just a base class defining shared data
{
    //Characters
    public GridUnit mainInstigator = null;
    public List<GridUnit> supportInstigators { get; protected set; } = new List<GridUnit> (); //Set when team skill used.

    public GridUnit target = null;

    //Vitals
    public int HPChange = 0;
    public int SPChange = 0;

    //Data
    public SkillForceData? forceData = null;
    public List<InflictedStatusEffectData> inflictedStatusEffects = new List<InflictedStatusEffectData>();

    //Modifier
    public List<HealthChangeModifier> appliedModifiers = new List<HealthChangeModifier>();

    //Bool
    public bool isCritical = false;


    //SETTERS 
    public void SetSupportInstigators(List<GridUnit> supportInstigators)
    {
        this.supportInstigators = new List<GridUnit>(supportInstigators);
        this.supportInstigators.Remove(mainInstigator);
    }

    //Getters
    public bool IsVitalsChanged()
    {
        return HPChange > 0 || SPChange > 0;
    }

    public bool IsSingleInstigator()
    {
        return supportInstigators.Count == 0;
    }

    public bool IsInstigatedByTeam()
    {
        return supportInstigators.Count > 1;
    }

    public List<GridUnit> GetInstigatorList()
    {
        return new List<GridUnit>(supportInstigators)
        {
            mainInstigator
        };
    }
}

public class HealthChangeModifier
{
    public float healthChangeMultiplier = 1; 
    public HealthModifier.Modifier<bool>? isCrit = null; // The "?" Nullable operator allows types like struct & ints to be null.

    public List<InflictedStatusEffectData> statusEffectsToInflict = new List<InflictedStatusEffectData>();

    public bool cancelEffect = false; //If true, this modifier would set damage or healing to 0. 
}

public class DamageModifier : HealthChangeModifier
{
    public HealthModifier.Modifier<Affinity>? newAffinity = null;
}

public class HealthModifier
{
    public struct Modifier<T>
    {
        public T valueOfType;
        public Priority priority;

        public Modifier(T valueOfType, Priority priority)
        {
            this.valueOfType = valueOfType;
            this.priority = priority;
        }
    }

    public enum Priority
    {
        Absolute,
        High,
        Mid,
        Low
    }
}

