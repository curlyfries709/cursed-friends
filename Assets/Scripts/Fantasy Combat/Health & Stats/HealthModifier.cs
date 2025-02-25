using System.Collections.Generic;

public class AttackData : HealthChangeData
{
    public GridUnit attacker = null;

    //Elements
    public Element attackElement = Element.None;
    public Item attackItem = null;

    //Damage Data
    public PowerGrade powerGrade = PowerGrade.C;
    public int rawDamage = 0;

    public int numOfTargets = 1;

    //Bools
    public bool isPhysical = false;
    public bool isMultiAction = false; //Does this skill attack multiple units individually

    //Can bools
    public bool canEvade = true;
    public bool canCrit = true;

    public AttackData(AttackData attackData)
    {
        attacker = attackData.attacker;
        
        attackElement = attackData.attackElement;
        attackItem = attackData.attackItem;

        powerGrade = attackData.powerGrade;
        rawDamage = attackData.rawDamage;

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
        this.attacker = attacker;
        this.rawDamage = rawDamage;
    }

    public AttackData(GridUnit attacker, Element attackElement, int rawDamage, int numOfTargets)
    {
        this.attacker = attacker;
        this.attackElement = attackElement;
        this.rawDamage = rawDamage;
        this.numOfTargets = numOfTargets;
    }

    public AttackData(GridUnit attacker, Item attackItem, int rawDamage, int numOfTargets)
    {
        this.attacker = attacker;
        this.attackItem = attackItem;
        this.rawDamage = rawDamage;
        this.numOfTargets = numOfTargets;
    }
}

public class DamageData : HealthChangeData
{
    public GridUnit target;
    public GridUnit attacker;
    public Health targetHealth;

    public Affinity affinityToAttack = Affinity.None;
    public AttackData hitByAttackData = null;
    public DamageType damageType = DamageType.Default;

    public int damageReceived = 0;

    //Bools
    public bool isBackstab = false;
    public bool isKOHit = false;
    public bool isTargetGuarding = false;
    public bool isKnockdownHit = false;

    public DamageData(GridUnit target, GridUnit attacker, AttackData hitByAttackData)
    {
        this.target = target;
        this.attacker = attacker;
        this.hitByAttackData = hitByAttackData;

        targetHealth = target?.Health();
    }

    public DamageData(GridUnit target, GridUnit attacker, Affinity affinityToAttack, int damageReceived)
    {
        this.target = target;
        this.attacker = attacker;

        this.affinityToAttack = affinityToAttack;
        this.damageReceived = damageReceived;

        targetHealth = target?.Health();
    }

    public DamageData(GridUnit target, GridUnit attacker, Affinity affinityToAttack, int damageReceived, AttackData hitByAttackData)
    {
        this.target = target;
        this.attacker = attacker;

        this.affinityToAttack = affinityToAttack;
        this.damageReceived = damageReceived;

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
            attacker = null;

        affinityToAttack = Affinity.None;
        hitByAttackData = null;
        damageType = DamageType.Default;

        damageReceived = 0;
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

public class HealData: HealthChangeData
{
    public GridUnit target = null;
    public CharacterGridUnit healer = null; //If null, then source of healing must be via item E.G Potion, Blessing.

    public int HPRestore = 0;
    public int SPRestore = 0;
    public int FPRestore = 0;

    //Bools
    public bool canRevive = false;
    public bool convertToDamage = false;

    public HealData(GridUnit target, int HPRestore)
    {
        this.target = target;
        this.HPRestore = HPRestore;
    }

    public HealData(GridUnit target, int HPRestore, int SPRestore, int FPRestore, bool canRevive)
    {
        this.target = target;
        this.HPRestore = HPRestore;
        this.SPRestore = SPRestore;
        this.FPRestore = FPRestore;
        this.canRevive = canRevive;
    }

    public HealData(GridUnit target, CharacterGridUnit healer, int HPRestore, bool canRevive)
    {
        this.target = target;
        this.healer = healer;
        this.HPRestore = HPRestore;
        this.canRevive = canRevive;
    }

    public bool IsOnlyFPRestore()
    {
        return FPRestore > 0 && HPRestore == 0 && SPRestore == 0;
    }
}

public abstract class HealthChangeData //Just a base class defining shared data
{
    public SkillForceData forceData = new SkillForceData(SkillForceType.None, SkillForceDirectionType.PositionDirection, 0);
    public List<InflictedStatusEffectData> inflictedStatusEffects = new List<InflictedStatusEffectData>();

    public List<HealthChangeModifier> appliedModifiers = new List<HealthChangeModifier>();

    public bool isCritical = false;
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

