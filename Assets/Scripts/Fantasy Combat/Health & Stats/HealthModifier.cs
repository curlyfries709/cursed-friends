using System.Collections.Generic;

public class AttackData
{
    public CharacterGridUnit attacker = null;

    //Elements
    public Element attackElement = Element.None;
    public Item attackItem = null;

    public SkillForceData forceData = new SkillForceData(SkillForceType.None, SkillForceDirectionType.PositionDirection, 0);
    public List<InflictedStatusEffectData> inflictedStatusEffects = new List<InflictedStatusEffectData>();

    //Damage Data
    public PowerGrade powerGrade = PowerGrade.C;
    public int rawDamage = 0;

    public int numOfTargets = 1;

    //Bools
    public bool isPhysical = false;
    public bool isCritical = false;
    public bool isMultiAction = false; //Does this skill attack multiple units individually

    //Can bools
    public bool canEvade = true;
    public bool canCrit = true;

    public List<HealthChangeModifier> appliedModifiers = new List<HealthChangeModifier>();

    public AttackData(CharacterGridUnit attacker, int rawDamage)
    {
        this.attacker = attacker;
        this.rawDamage = rawDamage;
    }

    public AttackData(CharacterGridUnit attacker, Element attackElement, int rawDamage, int numOfTargets)
    {
        this.attacker = attacker;
        this.attackElement = attackElement;
        this.rawDamage = rawDamage;
        this.numOfTargets = numOfTargets;
    }

    public AttackData(CharacterGridUnit attacker, Item attackItem, int rawDamage, int numOfTargets)
    {
        this.attacker = attacker;
        this.attackItem = attackItem;
        this.rawDamage = rawDamage;
        this.numOfTargets = numOfTargets;
    }
}

public class DamageData
{
    public GridUnit target;
    public CharacterGridUnit attacker;

    public Affinity affinityToAttack = Affinity.None;
    public AttackData hitByAttackData = null;
    public DamageType damageType = DamageType.Default;

    public int damageReceived = 0;
    public List<InflictedStatusEffectData> afflictedStatusEffects = new List<InflictedStatusEffectData>();

    public List<HealthChangeModifier> appliedModifiers = new List<HealthChangeModifier>();

    //Bools
    public bool isBackstab = false;
    public bool isCritical = false;
    public bool isKOHit = false;
    public bool isTargetGuarding = false;
    public bool isKnockdownHit = false;

    public DamageData(GridUnit target, CharacterGridUnit attacker, AttackData hitByAttackData)
    {
        this.target = target;
        this.attacker = attacker;
        this.hitByAttackData = hitByAttackData;
    }

    public DamageData(GridUnit target, CharacterGridUnit attacker, Affinity affinityToAttack, int damageReceived)
    {
        this.target = target;
        this.attacker = attacker;

        this.affinityToAttack = affinityToAttack;
        this.damageReceived = damageReceived;
    }

    public DamageData(GridUnit target, CharacterGridUnit attacker, Affinity affinityToAttack, int damageReceived, AttackData hitByAttackData)
    {
        this.target = target;
        this.attacker = attacker;

        this.affinityToAttack = affinityToAttack;
        this.damageReceived = damageReceived;

        this.hitByAttackData = hitByAttackData;
    }

    public void Clear(bool clearAttacker = true, bool clearTarget = true)
    {
        if (clearTarget)
            target = null;

        if (clearAttacker)
            attacker = null;

        affinityToAttack = Affinity.None;
        hitByAttackData = null;
        damageType = DamageType.Default;

        damageReceived = 0;
        afflictedStatusEffects.Clear();

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

