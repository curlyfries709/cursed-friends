using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct AffinityDamage
{
    public int damage;
    public Affinity affinity;
}


public struct DamageReceivedAlteration
{
    public float multiplier;
    public bool isCritical;

    public DamageReceivedAlteration(float multiplier)
    {
        this.multiplier = multiplier == 0 ? 1 :  multiplier;
        isCritical = false;
    }
}

public enum PowerGrade
{
    [InspectorName("A++")]
    A2,
    [InspectorName("A+")]
    A1,
    A,
    [InspectorName("B+")]
    B1,
    B,
    [InspectorName("C+")]
    C1,
    C,
    [InspectorName("D+")]
    D1,
    D,
    F,
    [InspectorName("F-")]
    F0

}

public class TheCalculator : MonoBehaviour
{
    public static TheCalculator Instance { get; private set; }
    [Header("TEST")]
    public bool printStats = false;
    [Header("Level 1 Values")]
    [SerializeField] int basePhysAttack = 8;
    [SerializeField] int baseWeight = 30;
    [Space(10)]
    [SerializeField] int baseVitality = 60;
    [SerializeField] int baseStamina = 26;
    [Space(10)]
    [SerializeField] int baseMagAttack = 32;
    [SerializeField] int baseMemory = 3;
    [Space(10)]
    [SerializeField] int baseHeal = 35;
    [SerializeField] int baseSEDuration = 2;
    [SerializeField] int baseScrollDuration = 4;
    [Header("Growth Rate")]
    [SerializeField] float physAttackGrowthRate = 5.5f;
    [SerializeField] float magAttackGrowthRate = 10f;
    [Space(10)]
    [SerializeField] float weightGrowthRate = 3f;
    [Space(10)]
    [SerializeField] float vitalityGrowthRate = 12f;
    [SerializeField] float staminaGrowthRate = 6f;
    [Space(10)]
    [SerializeField] float techniqueGrowthRate = 2f;
    [Space(10)]
    [SerializeField] float healConstant = 7;
    [SerializeField] float increaseHealConstantNValue = 75;
    [Space(10)]
    [SerializeField] float wisDurationConstant = 10;
    [Tooltip("Increase this value to increase the duration at higher Wisdom Values")]
    [SerializeField] float decreaseWisDurationConstantNValue = 24;
    [Header("Turn Growth")]
    [SerializeField] int memoryIncreaseNTurn = 3;
    [SerializeField] int speedIncreaseNTurn = 2;
    [SerializeField] int charismaSubattributeIncreaseNturns = 3;
    [Header("Finesse Constants")]
    [SerializeField] int minDifferenceIncreaseValue = 30;
    [Tooltip("Every this value of Technique or Evasion, the min difference will have increased by the above value. 50E-> 50 Diff; 100E -> 80 Diff")]
    [SerializeField] int finesseDifferenceConstant = 50;
    [Header("Multipliers")]
    [Range(1, 3)]
    [SerializeField] float weakDamageMultiplier;
    [Range(1, 3)]
    [SerializeField] float critDamageMultiplier;
    [Range(1, 3)]
    [SerializeField] float backStabMultiplier;
    [Header("Percentages")]
    [Range(50, 90)]
    [SerializeField] int guardDamageReductionPercent;
    [Range(50, 90)]
    [SerializeField] int resistDamageReductionPercent;
    [Range(1, 10)]
    [SerializeField] int damageVariancePercentage;
    [Range(50, 100)]
    [SerializeField] int backStabEvasionChanceReduction = 50;
    [Header("Constants")]
    [Range(0, 1)]
    [SerializeField] float weaponScalingConstant = 0.01f;
    [SerializeField] float backStabAngleCheck = 15f;
    [Header("Data")]
    [SerializeField] StatusEffectData wounded;


    //DAMAGE FORMULA:
    //((((Phys/Mag Attack + Weapon Attack) * Skill Power amplifier) – Armour) + Rand int between -3%damage - +3% damage)x Resistance Modifier.

    private void Awake()
    {
        if (!Instance)
            Instance = this;
    }

    public int CalculateRawDamage(CharacterGridUnit attacker, bool magicalAttack, PowerGrade skillPowerGrade, out bool isCritical, bool canCrit = true)
    {
        isCritical = false;

        //Method to call when skill deals damage
        int unitAttack = magicalAttack ? attacker.stats.MagAttack : attacker.stats.PhysAttack;

        int rawDamage;
        Weapon weapon = attacker.stats.Equipment().Weapon();

        if (weapon)
        {
            rawDamage = unitAttack + CalculateWeaponAttack(weapon, attacker.stats, magicalAttack);
        }
        else
        {
            rawDamage = unitAttack;
        }

        int varianceRange = Mathf.RoundToInt(rawDamage * (damageVariancePercentage / 100f));
        int variance = UnityEngine.Random.Range(-varianceRange, varianceRange + 1);

        float buffMultiplier = magicalAttack ? attacker.stats.buffINTmultiplier : attacker.stats.buffSTRmultiplier;

        //Calculate if critical.
        if (canCrit)
        {
            int randNum = UnityEngine.Random.Range(0, 101);

            isCritical = randNum <= attacker.stats.CritChance;

            if (isCritical)
            {
                return Mathf.RoundToInt(((rawDamage * GetPowerGradeMultiplier(skillPowerGrade) * critDamageMultiplier) + variance) * attacker.stats.blessingDamageMultiplier * buffMultiplier);
            }
        }

        return Mathf.RoundToInt(((rawDamage * GetPowerGradeMultiplier(skillPowerGrade)) + variance) * attacker.stats.blessingDamageMultiplier * buffMultiplier);
    }

    public AffinityDamage CalculateDamageReceived(AttackData attackData, CharacterGridUnit target, bool isTargetGuarding, out bool isBackStab, ref bool isCritical)
    {
        //SETUP
        AffinityDamage affinityDamage = new AffinityDamage();
        affinityDamage.affinity = Affinity.None;

        CharacterGridUnit attacker = attackData.attacker; 

        Element attackElement = attackData.attackElement;
        Item attackItem = attackData.attackIngridient;

        bool isCrit = attackData.isCritical;
        bool canEvade = attackData.canEvade;
        int rawDamage = attackData.damage;
        

        float externalMultiplier = 1;

        isBackStab = IsAttackBackStab(attacker, target);


        //Logic
        if (!isTargetGuarding && canEvade && EvadeAttack(attacker, target, isBackStab))
        {
            //Cannot Evade if Guarding.
            affinityDamage.affinity = Affinity.Evade;
            affinityDamage.damage = 0;
            return affinityDamage;
        }

        //Target Armour subtracted by Attacker Raw Damage
        int damageReduced = ArmourReduction(rawDamage, target);

        affinityDamage.affinity = GetAffinity(target, attackElement, attackItem);

        switch (affinityDamage.affinity)
        {
            case Affinity.Immune:
                affinityDamage.damage = 0;
                break;
            case Affinity.Weak:
                affinityDamage.damage = Mathf.RoundToInt(damageReduced * weakDamageMultiplier);
                break;
            case Affinity.Resist:
                affinityDamage.damage = Mathf.RoundToInt(damageReduced * (1f - (resistDamageReductionPercent / 100f)));
                break;
            default:
                affinityDamage.damage = damageReduced;
                break;
        }

        if(attacker.AlterDamageReductionAttack != null)
        {
            foreach (Func<bool, DamageReceivedAlteration> listener in attacker.AlterDamageReductionAttack.GetInvocationList())
            {
                DamageReceivedAlteration damageReceivedAlteration = listener.Invoke(isBackStab);
                externalMultiplier = externalMultiplier * damageReceivedAlteration.multiplier;

                if (!isCrit && damageReceivedAlteration.isCritical)
                {
                    isCrit = true;
                    externalMultiplier = externalMultiplier * critDamageMultiplier;
                }
            }
        }

        if (target.AlterDamageReceived != null)
        {
            foreach (Func<DamageReceivedAlteration> listener in target.AlterDamageReceived.GetInvocationList())
            {
                DamageReceivedAlteration damageReceivedAlteration = listener.Invoke();
                externalMultiplier = externalMultiplier * damageReceivedAlteration.multiplier;

                if (!isCrit && damageReceivedAlteration.isCritical)
                {
                    isCrit = true;
                    externalMultiplier = externalMultiplier * critDamageMultiplier;
                }    
            }
        }

        affinityDamage.damage = Mathf.RoundToInt(affinityDamage.damage * externalMultiplier);
        isCritical = isCrit;

        if (isTargetGuarding)
        {
            //Further Reduce Damage by Guard Reduction
            affinityDamage.damage = Mathf.RoundToInt(affinityDamage.damage * (1f - (guardDamageReductionPercent / 100f)));
        }
        else if (isBackStab)
        {
            affinityDamage.damage = Mathf.RoundToInt(affinityDamage.damage * backStabMultiplier);
        }

        TryWoundUnit(attacker, target, isCrit);

        //Difficulty Multipliers
        affinityDamage.damage = DamageDifficultyMultiplier(affinityDamage.damage, attacker is PlayerGridUnit);

        return affinityDamage;
    }

    public int CalculateBeatdownDamage(CharacterGridUnit attacker, CharacterGridUnit target, PowerGrade powerGrade)
    {
        int rawDamage = CalculateRawDamage(attacker, false, powerGrade, out bool isCritical, false);
        int damage = Mathf.RoundToInt(ArmourReduction(rawDamage, target) * attacker.stats.blessingDamageMultiplier);

        //Difficulty Multipliers
        damage = DamageDifficultyMultiplier(damage, attacker is PlayerGridUnit);

        return damage;
    }
    public int CalculatePOFDamage(List<PlayerGridUnit> playersPaticipating, CharacterGridUnit target, PowerGrade powerGrade)
    {
        int totalDamage = 0;

        foreach(PlayerGridUnit player in playersPaticipating)
        {
            bool isMagical = false;
            int rawDamage = CalculateRawDamage(player, isMagical, powerGrade, out bool isCritical, false);
            int damage = Mathf.RoundToInt(ArmourReduction(rawDamage, target) * player.stats.blessingDamageMultiplier);

            //Difficulty Multipliers
            damage = DamageDifficultyMultiplier(damage, true);

            totalDamage = totalDamage + damage;
        }

        return totalDamage;
    }

    private int ArmourReduction(int rawDamage, CharacterGridUnit target)
    {
        int targetArmour = target.stats.GetArmour();
        int damageReduced = rawDamage - targetArmour;
        damageReduced = Mathf.Max(0, Mathf.RoundToInt(damageReduced * target.stats.blessingDamageReductionMultiplier));
        return damageReduced;
    }

    private int DamageDifficultyMultiplier(int damage, bool isPlayerDealingDamage)
    {
        if (isPlayerDealingDamage)
        {
            return Mathf.Max(0, Mathf.RoundToInt(damage * GameManager.Instance.GetPlayerDifficultyDamageMultiplier()));
        }

        return Mathf.Max(0, Mathf.RoundToInt(damage * GameManager.Instance.GetEnemyDifficultyDamageMultiplier()));
    }
   

    private bool EvadeAttack(CharacterGridUnit attacker, CharacterGridUnit target, bool isBackStab)
    {
        //Calculate Min Difference
        int targetEvasion = target.stats.Evasion;
        int attackerTechnique = attacker.stats.Technique;

        float minDifference = finesseDifferenceConstant + (minDifferenceIncreaseValue * (((float)attackerTechnique / finesseDifferenceConstant) - 1));

        int minDifferenceToInt = Mathf.FloorToInt(minDifference);

        int minEvasionReqForGuaranteedEvasion = minDifferenceToInt + attackerTechnique;
        int maxEvasionReqForNoEvasion = attackerTechnique - minDifferenceToInt;

        int evasionChance = Mathf.RoundToInt(((float)targetEvasion - maxEvasionReqForNoEvasion) / ((float)minEvasionReqForGuaranteedEvasion - maxEvasionReqForNoEvasion) * 100);

        if (isBackStab)
        {
            //Reduce the evasion chance 
            evasionChance = Mathf.RoundToInt(evasionChance * (1f - (backStabEvasionChanceReduction / 100f)));
        }

        //Calculate if should Evade attack based on evasion chance
        int randNum = UnityEngine.Random.Range(0, 101);


        return randNum <= evasionChance;
    }

    private void TryWoundUnit(CharacterGridUnit attacker, CharacterGridUnit target, bool isCrit)
    {
        if (!isCrit) { return; }

        int randNum = UnityEngine.Random.Range(0, 101);

        bool applyWounded = randNum <= attacker.stats.StatusEffectInflictChance;

        if (applyWounded)
        {
            StatusEffectManager.Instance.ApplyStatusEffect(wounded, target, attacker, attacker.stats.SEDuration);
        }
    }

    public int CalculateHealAmount(CharacterGridUnit unit, out bool isCritical)
    {
        int varianceRange = Mathf.RoundToInt(unit.stats.HealEfficacy * (damageVariancePercentage / 100f));
        int variance = UnityEngine.Random.Range(-varianceRange, varianceRange + 1);

        int randNum = UnityEngine.Random.Range(0, 101);

        isCritical = randNum <= unit.stats.CritChance;

        if (isCritical)
        {
            return Mathf.RoundToInt((unit.stats.HealEfficacy * critDamageMultiplier) + variance);
        }

        return unit.stats.HealEfficacy + variance;
    }

    public Affinity GetAffinity(CharacterGridUnit unit, Element attackElement,  Item attackItem)
    {
        if(attackElement == Element.None && !attackItem)
        {
            Debug.Log("ATTACK ELEMENT IS NONE. IS THIS CORRECT?");
            return Affinity.None;
        }

        Affinity affinity = unit.stats.currentElementAffinities[attackElement];

        if(attackElement == Element.None && attackItem)
        {
            foreach (ItemAffinity itemAffinity in unit.stats.data.itemAffinities)
            {
                if(itemAffinity.item == attackItem)
                {
                    affinity = itemAffinity.affinity;
                    break;
                }
            }
        }

        return affinity;
    }


    public int CalculateWeaponAttack(Weapon weapon, UnitStats attackerStats, bool isMagicalAttack)
    {
        int scalingAttributeValue = weapon.scalingAttribute == Attribute.Intelligence ? attackerStats.Intelligence : attackerStats.Strength;
        int baseWeaponAttack = isMagicalAttack ? weapon.baseMagAttack : weapon.basePhysAttack;

        //Scaled Attack = base weapon attack *scaling percentage* strength *1 % (Constant)
        int scaledAttack = Mathf.RoundToInt(baseWeaponAttack * (weapon.scalingPercentage / 100) * scalingAttributeValue * weaponScalingConstant);

        bool includeScaledAttack = (isMagicalAttack && weapon.scalingAttribute == Attribute.Intelligence) || (!isMagicalAttack && weapon.scalingAttribute == Attribute.Strength);

        //Weapon Attack = Base weapon attack + scaled attack.
        if (includeScaledAttack)
        {
            return baseWeaponAttack + scaledAttack;
        }
        else
        {
            return baseWeaponAttack;
        }

    }

    public bool CanCounter(CharacterGridUnit target, Element attackElement)
    {
        //Contact Monster Database and see if Element Data unlocked for Element. If Unlocked & will damage, counter.
        Affinity affinity = GetAffinity(target, attackElement, null);

        //Still worth countering an immune target as it could inflict status effect. HOWEVER, I HAVE REMOVED IT. DESIGN CHOICE.
        bool isAttackableAffinity = affinity == Affinity.None || affinity == Affinity.Weak || affinity == Affinity.Resist;

        return isAttackableAffinity && EnemyDatabase.Instance.IsAffinityUnlocked(target, attackElement);
    }

    public bool IsAttackBackStab(CharacterGridUnit attacker, GridUnit target)
    {
        //Their forward transforms would need to be the same direction. However due to slight rotation discrepancy, I have done the below
        return Vector3.Angle(attacker.transform.forward.normalized, target.transform.forward.normalized) <= backStabAngleCheck;
    }

    public bool IsAttackBackStab(Transform attackerTransform, GridUnit target)
    {
        //Their forward transforms would need to be the same direction. However due to slight rotation discrepancy, I have done the below
        return Vector3.Angle(attackerTransform.forward.normalized, target.transform.forward.normalized) <= backStabAngleCheck;
    }

    public int GetCooldownHealRate()
    {
        return baseHeal;
    }

    public string GetWeaponScalingGrade(Weapon weapon)
    {
        int grade = weapon.scalingPercentage;

        if (grade >= 150)
        {
            return "A+";
        }
        else if (grade >= 125)
        {
            return "A";
        }
        else if (grade >= 100)
        {
            return "B";
        }
        else if (grade >= 75)
        {
            return "C";
        }
        else if (grade >= 50)
        {
            return "D";
        }
        else if (grade >= 25)
        {
            return "E";
        }
        else
        {
            return "F";
        }
    }

    public float GetPowerGradeMultiplier(PowerGrade grade)
    {
        switch (grade)
        {
            case PowerGrade.A2:
                return 5;
            case PowerGrade.A1:
                return 4.5f;
            case PowerGrade.A:
                return 4;
            case PowerGrade.B1:
                return 3.5f;
            case PowerGrade.B:
                return 3f;
            case PowerGrade.C1:
                return 2.5f;
            case PowerGrade.C:
                return 2;
            case PowerGrade.D1:
                return 1.5f;
            case PowerGrade.D:
                return 1;
            case PowerGrade.F:
                return 0.5f;
            case PowerGrade.F0:
                return 0.25f;
            default:
                return 1;
        }
    }

    //Sub Attribute Calculation

    //STR
    public int CalculatePhysAttack(int strength)
    {
        return Mathf.RoundToInt(basePhysAttack + (strength - 1) * physAttackGrowthRate);
    }

    public int CalculateInventoryCapacity(int strength)
    {
        return Mathf.RoundToInt(baseWeight + (strength - 1) * weightGrowthRate);
    }

    //FIN
    public int CalculateTechnique(int finesse)
    {
        return Mathf.RoundToInt(finesse * techniqueGrowthRate);
    }

    public int CalculateEvasion(int finesse)
    {
        return finesse;
    }

    //END
    public int CalculateVitality(int endurance)
    {
        return Mathf.RoundToInt(baseVitality + (endurance - 1) * vitalityGrowthRate);
    }

    public int CalculateStamina(int endurance)
    {
        return Mathf.RoundToInt(baseStamina + (endurance - 1) * staminaGrowthRate);
    }

    //AG
    public int CalculateSpeed(int agility)
    {
        return Mathf.Max(Mathf.FloorToInt((float)agility / speedIncreaseNTurn), 1);
    }

    //INT
    public int CalculateMagAttack(int intelligence)
    {
        return Mathf.RoundToInt(baseMagAttack + (intelligence - 1) * magAttackGrowthRate);
    }

    public int CalculateMemory(int intelligence)
    {
        return Mathf.Max(Mathf.FloorToInt(baseMemory + ((float)intelligence / memoryIncreaseNTurn)), baseMemory); ;
    }

    //WIS
    public int CalculateHealAmount(int wisdom)
    {
        return Mathf.RoundToInt(baseHeal + (wisdom - 1) * (healConstant + Mathf.Round(wisdom / increaseHealConstantNValue)));
    }

    public int CalculateScrollDuration(int wisdom)
    {
        return Mathf.RoundToInt(baseScrollDuration + (wisdom - 1) / (wisDurationConstant + (wisdom / decreaseWisDurationConstantNValue)));
    }

    public int CalculateSEDuration(int wisdom)
    {
        return Mathf.FloorToInt(baseSEDuration + (wisdom - 1) / (wisDurationConstant + (wisdom / decreaseWisDurationConstantNValue)));
    }

    //CHR
    public int CalculateCritChance(int charisma)
    {
        return Mathf.Max(Mathf.FloorToInt(1 + ((charisma + 1) / charismaSubattributeIncreaseNturns)), 1);

    }

    public int CalculateStatusEffectInflictChance(int charisma)
    {
        return Mathf.Max(Mathf.FloorToInt(1 + (charisma / charismaSubattributeIncreaseNturns)), 1);
    }



}
