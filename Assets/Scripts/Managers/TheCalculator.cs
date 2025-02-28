using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using AnotherRealm;
using Sirenix.Utilities;
using Sirenix.OdinInspector;

public struct AffinityDamage
{
    public int damage;
    public Affinity affinity;
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
    [Header("Fired Up Data")]
    [SerializeField] int fpBasicGainAmount = 5;
    [SerializeField] int fpEnhancedGainAmount = 10;
    [Space(10)]
    [SerializeField] int fpLossAmount = 25;

    //DAMAGE FORMULA:
    //((((Phys/Mag Attack + Weapon Attack) * Skill Power amplifier) – Armour) + Rand int between -3%damage - +3% damage)x Resistance Modifier.

    private void Awake()
    {
        if (!Instance)
            Instance = this;
    }

    public int CalculateRawDamage(GridUnit attacker, bool isMagicalAttack, PowerGrade skillPowerGrade, out bool isCritical, bool canCrit = true)
    {
        isCritical = false;

        //Method to call when skill deals damage
        int unitAttack = isMagicalAttack ? attacker.stats.MagAttack : attacker.stats.PhysAttack;

        int rawDamage;
        Weapon weapon = attacker.stats.Equipment().Weapon();

        if (weapon)
        {
            rawDamage = unitAttack + CalculateWeaponAttack(weapon, attacker.stats, isMagicalAttack);
        }
        else
        {
            rawDamage = unitAttack;
        }

        int varianceRange = Mathf.RoundToInt(rawDamage * (damageVariancePercentage / 100f));
        int variance = UnityEngine.Random.Range(-varianceRange, varianceRange + 1);

        float buffMultiplier = isMagicalAttack ? attacker.stats.buffINTmultiplier : attacker.stats.buffSTRmultiplier;

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

    public DamageData CalculateDamageReceived(AttackData attackData, GridUnit target, DamageType damageType, bool isTargetGuarding)
    {
        //SETUP
        GridUnit attacker = attackData.attacker;

        //Setup Damage Data
        DamageData damageData = ExtractDamageDataFromAttackData(target, attackData, damageType, false);

        damageData.isBackstab = IsAttackBackStab(attacker, target, damageType);
        damageData.isTargetGuarding = isTargetGuarding;

        //Evade Calculation
        if (damageType == DamageType.Default && !target.Health().IsObject())
        {
            if (!isTargetGuarding && attackData.canEvade && EvadeAttack(attacker, target as CharacterGridUnit, damageData.isBackstab))
            {
                //Cannot Evade if Guarding.
                damageData.Clear(false, false);
                damageData.affinityToAttack = Affinity.Evade;

                return damageData;
            }
        }

        //Get Affinity to attack
        damageData.affinityToAttack = GetAffinity(target, attackData.attackElement, attackData.attackItem);

        //Apply Pre Damage Calculated Modifiers
        int modifiedDamage = ApplyDamageModifiers(target, attacker, attackData, ref damageData);

        //Reduced modified Damage based on target's armour
        int damageReduced = ArmourReduction(target, modifiedDamage, damageType);

        //Apply affinity multiplier
        switch (damageData.affinityToAttack)
        {
            case Affinity.Immune:
                damageData.damageReceived = 0;
                break;
            case Affinity.Weak:
                ObjectHealth objectHealth = target.Health() as ObjectHealth;

                if(objectHealth && objectHealth.IsWeakHitKO())
                {
                    damageData.damageReceived = objectHealth.MaxHealth();
                }
                else
                {
                    damageData.damageReceived = Mathf.RoundToInt(damageReduced * weakDamageMultiplier);
                }
                break;
            case Affinity.Resist:
                damageData.damageReceived = Mathf.RoundToInt(damageReduced * (1f - (resistDamageReductionPercent / 100f)));
                break;
            case Affinity.Reflect:
                if (damageType == DamageType.Reflect)
                {
                    damageData.affinityToAttack = Affinity.Immune; //Update Affinity as to avoid looping reflects
                    damageData.damageReceived = 0;
                }
                else
                {
                    //Reflected Damage does not include armour reduction. 
                    damageData.damageReceived = modifiedDamage;
                }
                break;
            default:
                damageData.damageReceived = damageReduced;
                break;
        }

        if (isTargetGuarding)
        {
            //Further Reduce Damage by Guard Reduction
            damageData.damageReceived = Mathf.RoundToInt(damageData.damageReceived * (1f - (guardDamageReductionPercent / 100f)));
        }
        else if (damageData.isBackstab)
        {
            damageData.damageReceived = Mathf.RoundToInt(damageData.damageReceived * backStabMultiplier);
        }

        //Update isKnockdown Data
        if (damageType == DamageType.Default || damageType == DamageType.Reflect)
        {
            StatusEffectData knockdownEffect = StatusEffectManager.Instance.GetKnockdownEffectData();

            if (StatusEffectManager.Instance.CanApplyStatusEffect(target, knockdownEffect))
            {
                damageData.isKnockdownHit = damageData.affinityToAttack == Affinity.Weak || StatusEffectManager.Instance.IsKnockdownHit(damageData.inflictedStatusEffects, isTargetGuarding);

                if (damageData.isKnockdownHit)
                {
                    InflictedStatusEffectData knockdownEffectData = new InflictedStatusEffectData(knockdownEffect, target as CharacterGridUnit, attacker.stats.SEDuration, 0);

                    if (!damageData.inflictedStatusEffects.Any((effect) => effect.effectData == knockdownEffect))
                    {
                        damageData.inflictedStatusEffects.Add(knockdownEffectData);
                    }
                }
            }
            else
            {
                damageData.isKnockdownHit = false;
            }
        }

        //Try wounding if crit.
        if (TryWoundUnit(attacker, damageData.isCritical))
        {
            StatusEffectData woundedEffect = StatusEffectManager.Instance.GetWoundedEffectData();

            if (StatusEffectManager.Instance.CanApplyStatusEffect(target, woundedEffect))
            {
                InflictedStatusEffectData woundedEffectData = new InflictedStatusEffectData(woundedEffect, target as CharacterGridUnit, attacker.stats.SEDuration, 0);

                if (!damageData.inflictedStatusEffects.Any((effect) => effect.effectData == woundedEffect))
                {
                    damageData.inflictedStatusEffects.Add(woundedEffectData);
                }
            }
        }

        //Apply multipliers based on game Difficulty 
        if (damageType != DamageType.Reflect)
        {
            damageData.damageReceived = DamageDifficultyMultiplier(damageData.damageReceived, attacker is PlayerGridUnit);
        }

        //Check if KO hit
        damageData.isKOHit = damageData.affinityToAttack != Affinity.Absorb && damageData.damageReceived >= target.Health().GetPredictedCurrentHealth();

        /*Apply PostDamageCalculatedModifiers here. 
         * If we ever implement something like an endure where a KOHit leaves them with 1HP instead, as this depends on damage to be properly calculated to modify*/

        return damageData;
    }

    public DamageData CalculateStatusEffectDamage(CharacterGridUnit target, AttackData attackData, int healthPercent)
    {
        int health = healthPercent >= 100 ? target.stats.Vitality : target.stats.GetVitalityWithoutBonus();
        int amount = Mathf.RoundToInt((healthPercent / 100f) * health);
        attackData.rawDamage = amount;

        DamageData damageData = ExtractDamageDataFromAttackData(target, attackData, DamageType.StatusEffect);

        return damageData;
    }

    public HealData CalculateHealReceived(HealData healData)
    {
        if (healData.healer) //If null, then source of healing must be via item E.G Potion, Blessing.
        {
            int rawHealAmount = CalculateRawHeal(healData.healer, out healData.isCritical);
            healData.HPRestore = rawHealAmount;
        }

        healData.inflictedStatusEffects = FilterInvalidStatusEffects(healData.target, healData.inflictedStatusEffects);

        //Apply Modifiers
        ApplyHealModifiers(healData.target, healData.healer, ref healData);

        return healData;
    }

    public int CalculateRawHeal(CharacterGridUnit healer, out bool isCritical)
    {
        int varianceRange = Mathf.RoundToInt(healer.stats.HealEfficacy * (damageVariancePercentage / 100f));
        int variance = UnityEngine.Random.Range(-varianceRange, varianceRange + 1);

        int randNum = UnityEngine.Random.Range(0, 101);

        isCritical = randNum <= healer.stats.CritChance;

        if (isCritical)
        {
            return Mathf.RoundToInt((healer.stats.HealEfficacy * critDamageMultiplier) + variance);
        }

        return healer.stats.HealEfficacy + variance;
    }

    private int ApplyDamageModifiers(GridUnit target, GridUnit attacker, AttackData attackData, ref DamageData damageData)
    {
        float externalMultiplier = 1;
        bool wasOriginallyCrit = attackData.isCritical;

        //Now allow modifiers to modify the damage
        if (attacker.ModifyDamageDealt != null)
        {
            /* Add/Multiply Multipliers
             * Check if cancel effect
             Add Status Effects to damage data list of status effects
            Alter Affinity 
            Check if crit
             */

            /*foreach (Func<bool, DamageReceivedModifier> listener in attacker.ModifyDamageDealt.GetInvocationList())
            {
                DamageReceivedModifier damageReceivedAlteration = listener.Invoke(isBackStab);
                externalMultiplier = externalMultiplier * damageReceivedAlteration.multiplier;

                if (!isCrit && damageReceivedAlteration.isCritical) MOVE THIS PART TO AFTER THE LOOP IN CASE SOME OTHER MODIFIER SETS CRIT TO FALSE. ALSO REMOVE CRIT MULTIPLIER IF CANCELLING CRIT
                {
                    isCrit = true;
                    externalMultiplier = externalMultiplier * critDamageMultiplier;
                }
            }*/
        }

        if (target.ModifyDamageReceived != null)
        {
            /*foreach (Func<DamageReceivedModifier> listener in target.ModifyDamageReceived.GetInvocationList())
            {
                DamageReceivedModifier damageReceivedAlteration = listener.Invoke();
                externalMultiplier = externalMultiplier * damageReceivedAlteration.multiplier;

                if (!isCrit && damageReceivedAlteration.isCritical)
                {
                    isCrit = true;
                    externalMultiplier = externalMultiplier * critDamageMultiplier;
                }    
            }*/
        }

        //Update data based on modifiers
        //damageData.isCritical = isCrit;

        //Apply other multipliers
        return Mathf.RoundToInt(attackData.rawDamage * externalMultiplier);
    }

    private void ApplyHealModifiers(GridUnit target, CharacterGridUnit healer, ref HealData healData)
    {

    }

    private int ArmourReduction(GridUnit target, int rawDamage, DamageType damageType)
    {
        //Check if damage type allows armour reduction 
        if(damageType == DamageType.KnockbackBump)
        {
            return rawDamage;
        }

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

    private bool EvadeAttack(GridUnit attacker, CharacterGridUnit target, bool isBackStab)
    {
        int evasionChance;

        if (attacker.stats.UseHitChance(out int hitChance))
        {
            evasionChance = 100 - hitChance;
        }
        else
        {
            //Calculate Min Difference
            int targetEvasion = target.stats.Evasion;
            int attackerTechnique = attacker.stats.Technique;

            float minDifference = finesseDifferenceConstant + (minDifferenceIncreaseValue * (((float)attackerTechnique / finesseDifferenceConstant) - 1));

            int minDifferenceToInt = Mathf.FloorToInt(minDifference);

            int minEvasionReqForGuaranteedEvasion = minDifferenceToInt + attackerTechnique;
            int maxEvasionReqForNoEvasion = attackerTechnique - minDifferenceToInt;

            evasionChance = Mathf.RoundToInt(((float)targetEvasion - maxEvasionReqForNoEvasion) / ((float)minEvasionReqForGuaranteedEvasion - maxEvasionReqForNoEvasion) * 100);

        }

        if (isBackStab)
        {
            //Reduce the evasion chance 
            evasionChance = Mathf.RoundToInt(evasionChance * (1f - (backStabEvasionChanceReduction / 100f)));
        }

        //Calculate if should Evade attack based on evasion chance
        int randNum = UnityEngine.Random.Range(0, 101);

        return randNum <= evasionChance;
    }

    private bool TryWoundUnit(GridUnit attacker, bool isCrit)
    {
        if (!isCrit) { return false; }

        int randNum = UnityEngine.Random.Range(0, 101);

        bool applyWounded = randNum <= attacker.stats.StatusEffectInflictChance;

        return applyWounded;
    }

    public int CalculateFPGain(bool isEnhancedAction, int numOfSEApplied = 0)
    {
        int gain = isEnhancedAction ? fpEnhancedGainAmount : fpBasicGainAmount;
        int SEGain = numOfSEApplied * fpEnhancedGainAmount;

        if (isEnhancedAction) //A Special Hit that applied SE. So Earn FP For The Special Hit & Applied SE.
        {
            return gain + SEGain;
        }
        else if (numOfSEApplied > 0) //Normal Hit that applied SE. So Only Gain FP for applied SE.
        {
            return SEGain;
        }

        return gain;
    }

    public int CalculateNewFPAfterLoss(int currentFP)
    {
        return Mathf.Max(currentFP - fpLossAmount, 0);
    }

    public DamageData ExtractDamageDataFromAttackData(GridUnit target, AttackData attackData, DamageType damageType, bool updateDamage = true)
    {
        DamageData damageData = new DamageData(target, attackData.attacker, attackData);

        damageData.attacker = attackData.attacker;
        damageData.hitByAttackData = attackData;
        damageData.isCritical = attackData.isCritical;
        damageData.forceData = attackData.forceData;
        damageData.damageType = damageType;

        if (updateDamage)
        {
            damageData.damageReceived = attackData.rawDamage;
        }

        CharacterGridUnit character = target as CharacterGridUnit;

        if (character)
        {
            //Check if effect can even be applied 
            damageData.inflictedStatusEffects = FilterInvalidStatusEffects(character, attackData.inflictedStatusEffects);
        }

        return damageData;
    }

    private List<InflictedStatusEffectData> FilterInvalidStatusEffects(GridUnit target, List<InflictedStatusEffectData> listToFilter)
    {
        if (listToFilter.IsNullOrEmpty())
        {
            return listToFilter;
        }

        //Removes Status effects if they cannot be applied 
        return listToFilter.Where((effect) => StatusEffectManager.Instance.CanApplyStatusEffect(target, effect.effectData)).ToList();
    }

    public Affinity GetAffinity(GridUnit unit, Element attackElement,  Item attackItem)
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

    public bool CanCounter(GridUnit target, Element attackElement)
    {
        if(target is CharacterGridUnit character)
        {
            //Contact Monster Database and see if Element Data unlocked for Element. If Unlocked & will damage, counter.
            Affinity affinity = GetAffinity(character, attackElement, null);

            //Still worth countering an immune target as it could inflict status effect. HOWEVER, I HAVE REMOVED IT. DESIGN CHOICE.
            bool isAttackableAffinity = affinity == Affinity.None || affinity == Affinity.Weak || affinity == Affinity.Resist;
            bool isAffinityUnlocked = EnemyDatabase.Instance.IsAffinityUnlocked(character, attackElement);

            return isAttackableAffinity && isAffinityUnlocked;
        }

        return false;
    }

    public bool IsAttackBackStab(GridUnit attacker, GridUnit target, DamageType damageType)
    {
        if(damageType != DamageType.Default)
        {
            return false;
        }

        if (target.Health().IsObject())
        {
            return false; //Objects cannot be backstabbed
        }

        Direction targetBackDirection = CombatFunctions.GetDirectionFromVector(-target.transform.forward.normalized);
        return CombatFunctions.IsGridPositionInDirection(target.GetGridPositionsOnTurnStart()[0], attacker.GetGridPositionsOnTurnStart()[0], targetBackDirection);
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
