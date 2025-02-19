using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.Serialization;

public class FantasyHealth : MonoBehaviour, IDamageable, ISaveable
{
    [Range(25, 100)]
    [SerializeField] int maxFP;
    [Space(10)]
    [SerializeField] Transform unitStatusEffectHeader;
    [Header("UI")]
    [SerializeField] GameObject healthCanvas;
    [SerializeField] Transform statusEffectVisualHeader;
    [Header("DAMAGE FEEDBACKS")]
    [SerializeField] AffinityFeedback damageFeedbacks;

    //Saving Data
    [SerializeField, HideInInspector]
    private HealthState healthState = new HealthState();
    bool isDataRestored = false;

    //Caches

    GridUnitAnimator unitAnimator;
    CharacterGridUnit myUnit;
    UnitHealthUI healthUI;
    IDamageable IDamageable;

    bool isPlayer = false;

    //Variables
    //int maxHealth;
    //int maxSP;

    public CharacterGridUnit attacker { get; private set; }
    public int currentHealth { get; set; }
    public int currentSP { get; set; }
    public int currentFP { get; set; }

    bool isGuardBreakingAttack = false;

    Affinity currentAffinity = Affinity.None;

    bool criticalDamage = false;
    bool isBackstab = false;
    bool isKOHit = false;
    bool isKnockdownHit = false;
    bool isSEDamage = false;
    bool isKnockbackDamage = false;

    bool beginHealthCountdown = false;

    //State Bools
    public bool isKOed { get; private set; }
    public bool isGuarding { get; private set; }
    public bool isKnockedDown { get; private set; }

    public bool isFiredUp { get; private set; }

    //Events

    public static Action<CharacterGridUnit> CharacterUnitKOed;
    public static Action<CharacterGridUnit> CharacterUnitRevived;

    public enum Vital
    {
        HP,
        SP,
        FP
    }


    private void Awake()
    {
        SetIntializationData();
    }

    private void OnEnable()
    {
        SavingLoadingManager.Instance.EnteringNewTerritory += OnEnterNewTerritory;
    }

    private void OnEnterNewTerritory()
    {
        if (isPlayer) { return; }
        
        ResetVitals();
    }


    private void OnDisable()
    {
        SavingLoadingManager.Instance.EnteringNewTerritory -= OnEnterNewTerritory;
    }
    //Damage
    public Affinity TakeDamage(AttackData attackData)
    {
        if (isKOed) { return Affinity.None; }

        ClearDamageData();

        //Subscribe to event
        IDamageable.unitAttackComplete += DisplayDamageData;

        attackData.canEvade = attackData.canEvade && !StatusEffectManager.Instance.IsUnitDisabled(myUnit);
        bool canApplyForces = !myUnit.stats.IsImmuneToForces();

        criticalDamage = attackData.isCritical;
        attacker = attackData.attacker;

        AffinityDamage affinityDamage = TheCalculator.Instance.CalculateDamageReceived(attackData, myUnit, isGuarding, out isBackstab, ref criticalDamage);

        currentAffinity = affinityDamage.affinity;
        isKnockdownHit = StatusEffectManager.Instance.IsKnockdownHit(this, attackData.inflictedStatusEffects) || currentAffinity == Affinity.Weak;

        if (currentAffinity == Affinity.Absorb)
        {
            //Heal By Damage Dealt
            currentHealth = currentHealth + affinityDamage.damage;
            currentHealth = Mathf.Min(currentHealth, MaxHealth());
        }
        else if(currentAffinity == Affinity.Reflect)
        {
            //Call Reflect Damage Event
            canApplyForces = false;
            Reflect.DamageReflected(attacker, myUnit, attackData);
        }
        else if(currentAffinity == Affinity.Evade)
        {
            canApplyForces = false;
            Evade.Instance.UnitEvaded(attacker, myUnit, attackData.numOfTargets);
        }
        else
        {
            //Else Deduct Damage.
            isGuardBreakingAttack = true;
            currentHealth = currentHealth - affinityDamage.damage;

            if(currentHealth > 0)
            {
                List<ChanceOfInflictingStatusEffect> statusEffects = new List<ChanceOfInflictingStatusEffect>();

                //Apply Status Effects if still alive
                foreach (InflictedStatusEffectData inflictedStatusEffect in attackData.inflictedStatusEffects)
                {
                    StatusEffectManager.Instance.ApplyStatusEffect(inflictedStatusEffect.effectData, myUnit, attacker, inflictedStatusEffect.numOfTurns, inflictedStatusEffect.buffChange);

                    ChanceOfInflictingStatusEffect statusEffect = new ChanceOfInflictingStatusEffect();

                    statusEffect.buffChange = inflictedStatusEffect.buffChange;
                    statusEffect.statusEffect = inflictedStatusEffect.effectData;
                    statusEffect.numOfTurns = inflictedStatusEffect.numOfTurns;

                    statusEffects.Add(statusEffect);
                }

                if (statusEffects.Count > 0)
                    SetBuffsToApplyVisual(statusEffects);
            }
        }

        FinalizeDamageData(attackData, affinityDamage.damage, canApplyForces, false, false);

        return currentAffinity;
    }


    public void TakeReflectDamage(AttackData attackData)
    {
        if (isKOed) { return; }

        ClearDamageData();

        Element attackElement = attackData.attackElement;

        attacker = attackData.attacker;

        int damage = attackData.damage;
        bool isCritical = attackData.isCritical;

        List<InflictedStatusEffectData> inflictedStatusEffects = attackData.inflictedStatusEffects;

        //Subscribe to event
        IDamageable.unitAttackComplete += DisplayDamageData;

        currentAffinity = TheCalculator.Instance.GetAffinity(myUnit, attackElement, attackData.attackIngredient);
        criticalDamage = isCritical;

        isKnockdownHit = StatusEffectManager.Instance.IsKnockdownHit(this, attackData.inflictedStatusEffects) || currentAffinity == Affinity.Weak;

        if (currentAffinity == Affinity.Absorb)
        {
            //Heal By Damage Dealt
            currentHealth = currentHealth + damage;
            currentHealth = Mathf.Min(currentHealth, MaxHealth());
        }
        else if (currentAffinity == Affinity.Reflect || currentAffinity == Affinity.Immune)
        {
            //As it's already reflected just change affinity to immune
            currentAffinity = Affinity.Immune;
            damage = 0;
        }
        else
        {
            //Else Deduct Damage.
            currentHealth = currentHealth - damage;

            if(currentHealth > 0)
            {
                //Apply Status Effects if still alive.
                List<ChanceOfInflictingStatusEffect> statusEffects = new List<ChanceOfInflictingStatusEffect>();

                foreach (InflictedStatusEffectData inflictedStatusEffect in attackData.inflictedStatusEffects)
                {
                    StatusEffectManager.Instance.ApplyStatusEffect(inflictedStatusEffect.effectData, myUnit, attacker, inflictedStatusEffect.numOfTurns, inflictedStatusEffect.buffChange);

                    ChanceOfInflictingStatusEffect statusEffect = new ChanceOfInflictingStatusEffect();

                    statusEffect.buffChange = inflictedStatusEffect.buffChange;
                    statusEffect.statusEffect = inflictedStatusEffect.effectData;
                    statusEffect.numOfTurns = inflictedStatusEffect.numOfTurns;

                    statusEffects.Add(statusEffect);
                }

                if (statusEffects.Count > 0)
                    SetBuffsToApplyVisual(statusEffects);
            }
        }

        FinalizeDamageData(attackData, damage, !myUnit.stats.IsImmuneToForces(), true, false);
    }
    public void TakeStatusEffectDamage(CharacterGridUnit inflictor, int percentage)
    {
        int health = percentage >= 100 ? MaxHealth() : myUnit.stats.GetVitalityWithoutBonus();
        int amount = Mathf.RoundToInt((percentage / 100f) * health);
        TakeDamageBasic(inflictor, amount, false, isSEDamage: true);
    }

    public void TakeBumpDamage(int damage)
    {
        TakeDamageBasic(attacker, damage, false, isKnockbackDamage: true);
    }

    public void TakeDamageBasic(CharacterGridUnit attacker, int amount, bool isCritical, bool isSEDamage = false, bool isKnockbackDamage = false)
    {
        if (isKOed) { return; }

        ClearDamageData();

        this.attacker = attacker;
        criticalDamage = isCritical;
        this.isSEDamage = isSEDamage;

        //Subscribe to event
        IDamageable.unitAttackComplete += DisplayDamageData;

        currentHealth = currentHealth - amount;

        AttackData attackData = new AttackData();
        attackData.forceData.forceDistance = 0;

        FinalizeDamageData(attackData, amount, false, false, isKnockbackDamage);
    }

    public void TakeSPLoss(int percentage)
    {
        int amount = Mathf.RoundToInt((percentage / 100f) * myUnit.stats.GetStaminaWithoutBonus());
        currentSP = Mathf.Max(0, currentSP - amount);

        //Update Health UI Number.
        healthUI.SetSPChangeNumberText(amount);
    }


    private void FinalizeDamageData(AttackData attackData, int damageReceived, bool canApplyForces, bool isReflectDamage, bool isKnockbackDamage)
    {
        this.isKnockbackDamage = isKnockbackDamage;

        //Prepare Data to be shown.
        healthUI.SetHPChangeNumberText(damageReceived);

        currentHealth = Mathf.Max(currentHealth, 0);

        if (currentHealth == 0 && !isKOed)
        {
            isKOHit = true;
        }

        //Knockback
        if (canApplyForces && attackData.forceData.forceType != SkillForceType.None)
        {
            SkillForce.Instance.PrepareToApplyForceToUnit(attacker, myUnit, attackData.forceData, attackData.damage);
        }

        if (!isReflectDamage && !isKnockbackDamage)
        {
            //Update Enemy Database if enemy.
            if (!(myUnit is PlayerGridUnit) && currentAffinity != Affinity.Evade)
            {
                EnemyDatabase.Instance.UpdateEnemyData(myUnit.stats.data, attackData);
            }
        }

        if(currentAffinity != Affinity.Evade)
            IDamageable.unitHit(GetDamageData(attackData, damageReceived, isKnockbackDamage));

        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(currentAffinity, isKOHit, isKnockdownHit);
        //KO Logic must occur once damage data has been displayed.
    }


    //Heal

    public void Heal(int HPAmount, int SPAmount, bool updateDataDisplayTime = true, bool passiveHeal = false)
    {
        //Prepare Data to be shown.
        if(SPAmount > 0)
        {
            healthUI.SetSPChangeNumberText(SPAmount);
            currentSP = Mathf.Min(currentSP + SPAmount, MaxSP());
        }
        
        if(HPAmount > 0)
        {
            healthUI.SetHPChangeNumberText(HPAmount);
            currentHealth = Mathf.Min(currentHealth + HPAmount, MaxHealth());
        }

        if (updateDataDisplayTime)
        {
            FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(Affinity.None, false, false);
        }

        UpdateHUD();
        healthUI.ShowHealing(GetHealthNormalized());

        if(passiveHeal)
        {
            FantasyCombatManager.Instance.BeginPassiveHealRoutine(myUnit);
        }
        else
        {
            IDamageable.BeginHealthUICountdown(true);
        }
    }

    public void Revive(int newHealth, int SPGain, bool updateDataDisplayTime) //Only Caled During Combat
    {
        isKOed = false;

        if (isPlayer)
            (myUnit as PlayerGridUnit).ActivateGridCollider(true);

        myUnit.unitAnimator.SetTrigger(myUnit.unitAnimator.animIDRevive);
        Heal(newHealth, SPGain, updateDataDisplayTime);

        CharacterUnitRevived(myUnit);
    }

    public void OuterCombatRestore(int hpGain, int spGain, int fpGain)
    {
        currentHealth = Mathf.Min(currentHealth + hpGain, MaxHealth());
        currentSP = Mathf.Min(currentSP + spGain, MaxSP());
        currentFP = Mathf.Min(currentFP + fpGain, maxFP);

        UpdateHUD();
    }


    private void DisplayDamageData(bool beginHealthCountdown)
    {
        //Debug.Log("Displaying Damage for: " + myUnit.unitName);

        this.beginHealthCountdown = beginHealthCountdown;

        //UnSubscribe to event
        IDamageable.unitAttackComplete -= DisplayDamageData;

        TriggerAffinityEvent();
    }

    private void ClearDamageData()
    {
        isKOHit = false;
        isKnockdownHit = false;
        isSEDamage = false;

        currentAffinity = Affinity.None;
        criticalDamage = false;
        isBackstab = false;
        isKnockbackDamage = false;
    }

    private DamageData GetDamageData(AttackData attackData, int damageTaken, bool isKnockbackDamage)
    {
        DamageData damageData = new DamageData(myUnit, attacker, currentAffinity, damageTaken);

        damageData.isBackstab = isBackstab;
        damageData.isCritical = criticalDamage;
        damageData.isKOHit = isKOHit;
        damageData.isGuarding = isGuarding;
        damageData.isKnockbackDamage = isKnockbackDamage;
        damageData.hitByAttackData = attackData;

        return damageData;
    }


    private void TriggerAffinityEvent()
    {
        switch (currentAffinity)
        {
            case Affinity.Evade:
                //Evade UI & Animation Triggered By Evade Script
                //IDamageable.BeginHealthUICountdown(beginHealthCountdown);
                break;
            case Affinity.Absorb:
                OnAbsorbDamage();
                break;
            case Affinity.Immune:
                OnDamageNullified();
                break;
            case Affinity.Reflect:
                OnReflect();
                break;
            case Affinity.Weak:
                OnWeak();
                break;
            case Affinity.Resist:
                //GuardBreak();
                OnResist();
                break;
            default:
                //GuardBreak();
                OnNormalDamage();
                break;
        }
    }

    private void OnNormalDamage()
    {
        if (currentHealth <= 0)
        {
            KO();
            return;
        }

        Hit();
    }

    private void OnResist()
    {
        //Show Resist Text
        healthUI.Resist();

        if (currentHealth <= 0)
        {
            KO();
            return;
        }

        Hit();
    }

    private void OnDamageNullified()
    {
        ShowBackstab();

        //Show Immune UI
        healthUI.Immune();
        IDamageable.BeginHealthUICountdown(beginHealthCountdown);
    }

    private void OnReflect()
    {
        ShowBackstab();

        healthUI.Reflect();
        IDamageable.BeginHealthUICountdown(beginHealthCountdown);
    }

    /*public void OnEvade()
    {
        //Gain FP
        GainFP(CalculateFPGain(true));

        healthUI.Evade();
    }*/



    public void OnEvade(bool startCountdown)
    {
        beginHealthCountdown = startCountdown;

        //Gain FP
        GainFP(CalculateFPGain(true));

        healthUI.Evade();

        if (startCountdown)
        {
            attacker.unitAnimator.CancelDisplaySkillFeedbackEvent(true);
            FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(Affinity.Evade, false, false);
        }   

        IDamageable.BeginHealthUICountdown(beginHealthCountdown);
    }



    private void OnAbsorbDamage()
    {
        //Show Absorb UI
        healthUI.Absorb(GetHealthNormalized());
        UpdateHUD();

        ShowCritical();
        ShowBackstab();

        IDamageable.BeginHealthUICountdown(beginHealthCountdown);
    }

    private void OnWeak()
    {
        healthUI.Weak();

        if (currentHealth <= 0)
        {
            KO();
            return;
        }

        Hit();
    }


    private void Hit()
    {
        ShowGuard();
        ShowCritical();
        ShowBackstab();
        ShowBump();

        UpdateHUD();
        SetHealthBar(GetHealthNormalized());

        if (isKnockdownHit)
        {
            Knockdown();
        }
        else
        {
            //Trigger hit animation.
            unitAnimator.Hit();
        }

        //Contact Status Effect Manager
        int numberOfSEApplied = StatusEffectManager.Instance.TriggerNewlyAppliedEffects(myUnit);

        //Give Attacker FP
        if (currentAffinity != Affinity.Resist && attacker)
        {
            attacker.Health().GainFP(CalculateFPGain(criticalDamage || isBackstab || isKnockdownHit || isKOHit, numberOfSEApplied));
        }

        IDamageable.BeginHealthUICountdown(beginHealthCountdown);
    }


    private void KO()
    {
        isKOed = true;
        
        CharacterUnitKOed(myUnit);

        ShowCritical();
        ShowBackstab();
        ShowBump();

        UpdateHUD();
        SetHealthBar(GetHealthNormalized());

        unitAnimator.KO();  

        //Give FP, then Deplete FP
        if (attacker)
        {
            attacker.Health().GainFP(CalculateFPGain(true));
        }

        PlayerGridUnit player = myUnit as PlayerGridUnit;

        if (player)
            player.ActivateGridCollider(false);
            
        LoseFP(true);
        isGuarding = false;
        IDamageable.BeginHealthUICountdown(beginHealthCountdown);
    }


    public void Guard(bool beginGuarding)
    {
        isGuarding = beginGuarding;
        unitAnimator.SetBool(unitAnimator.animIDGuarding, isGuarding);

        UpdateHUD();
    }

    public void Knockdown()
    {
        if (!isKnockedDown && !isKOed && !isKOHit)
        {
            //Apply Knockdown Effect.
            isKnockedDown = true;
            KnockdownEvent.UnitKnockdown(attacker);
            StatusEffectManager.Instance.UnitKnockedDown(myUnit);
        }
    }

    private void ShowCritical()
    {
        if (criticalDamage)
        {
            healthUI.CritHit();
        }
    }

    private void ShowGuard()
    {
        if (isGuarding && !isSEDamage)
        {
            healthUI.Guard();
        }
    }

    private void ShowBump()
    {
        if(isKnockbackDamage)
            healthUI.Bump();
    }

    private void ShowBackstab()
    {
        if (isBackstab)
        {
            healthUI.BackStab();
        }
    }

    public void GainFP(int gain)
    {
        if (StatusEffectManager.Instance.ProhibitUnitFPGain(myUnit)) { return; }

        currentFP = Mathf.Min(currentFP + gain, maxFP);

        UpdateHUD();
    }

    public void LoseFP(bool depleteAll = false)
    {
        if (depleteAll)
        {
            currentFP = 0;
        }
        else
        {
            currentFP = Mathf.Max(currentFP - FantasyCombatManager.Instance.fpLossAmount, 0);
        }

        UpdateHUD();
    }


    private int CalculateFPGain(bool isEnhancedAction, int numOfSEApplied = 0)
    {
        int gain = isEnhancedAction ? FantasyCombatManager.Instance.fpEnhancedGainAmount : FantasyCombatManager.Instance.fpBasicGainAmount;
        int SEGain = numOfSEApplied * FantasyCombatManager.Instance.fpEnhancedGainAmount;

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

    //Gain
    public void GainSPInstant(int amount)
    {
        currentSP = Mathf.Min(currentSP + amount, MaxSP());
        UpdateHUD();
    }


    //Spend

    public void SpendSP(int amount)
    {
        currentSP = currentSP - amount;
        UpdateHUD();
    }

    public void SpendHP(int amount)
    {
        currentHealth = currentHealth - amount;

        UpdateHUD();
    }

    public void SpendFP(int amount)
    {
        currentFP = currentFP - amount;
        UpdateHUD();
    }


    //SETUPS
    private void NewGameSetup()
    {
        SetIntializationData();

        myUnit.stats.UpdateSubAndMainAttributes();

        currentHealth = MaxHealth();
        currentSP = MaxSP();
        currentFP = 0;

        isKOed = false;
        isGuarding = false;

        SetupHealthUI();
    }

    public void BattleComplete() //Called By Victory & Fled
    {
        isGuarding = false;
        if (!isKOed) { return; }

        isKOed = false;
        currentHealth = 1;

        PlayerGridUnit player = myUnit as PlayerGridUnit;

        if (player)
            player.ActivateGridCollider(true);

        UpdateHUD();
    }

    public void ResetStateToBattleStart(int healthAtStart, int spAtStart, int fpAtStart)
    {
        PlayerGridUnit player = myUnit as PlayerGridUnit;

        if (player)
            player.ActivateGridCollider(true);

        isKOed = false;
        isGuarding = false;
        isFiredUp = false;
        isKnockedDown = false;

        currentHealth = healthAtStart;
        currentSP = spAtStart;
        currentFP = fpAtStart;

        myUnit.ActivateUnit(true);

        myUnit.unitAnimator.ResetAnimatorToCombatState();
        SetupHealthUI();
    }

    public void ResetVitals()
    {
        isKOed = false;
        isGuarding = false;
        isFiredUp = false;
        isKnockedDown = false;

        currentHealth = MaxHealth();
        currentSP = MaxSP();
        currentFP = 0;

        myUnit.unitAnimator.ResetAnimatorToRoamState();
        SetupHealthUI();
    }

    //Getters
    public AffinityFeedback GetDamageFeedbacks(Transform transformToPlayVFX, GameObject VFXToPlay)
    {
        IDamageable.SetVFXToPlay(myUnit, damageFeedbacks, transformToPlayVFX, VFXToPlay);
        return damageFeedbacks;
    }

    public bool CanTriggerFiredUp()
    {
        return currentFP >= maxFP && !StatusEffectManager.Instance.IsUnitDisabled(myUnit);
    }


    public GameObject GetStatusEffectHeader()
    {
        return unitStatusEffectHeader.gameObject;
    }

    public Transform GetStatusEffectUIHeader()
    {
        return healthUI.StatusEffectHeader();
    }

    public float GetHealthNormalized()
    {
        return (float)currentHealth / MaxHealth();
    }

    public float GetStaminaNormalized()
    {
        return (float)currentSP / MaxSP();
    }

    public float GetFPNormalized()
    {
        return (float)currentFP / maxFP;
    }

    public int MaxHealth()
    {
        return myUnit.stats.Vitality;
    }

    public int MaxSP()
    {
        return myUnit.stats.Stamina;
    }

    public bool CanTriggerAssistAttack() //Such as a bump attack or bond ability
    {
        return !isGuarding && !StatusEffectManager.Instance.IsUnitDisabled(myUnit) && !isKOed;
    }

    //Setters
    public void ActivateHealthVisual(bool show)
    {
        healthUI.Fade(show);
    }

    public void DeactivateHealthVisualImmediate()
    {
        healthUI.DeactivateImmediate();
    }

    public void ActivateStatusEffectHealthVisual(bool show)
    {
        healthUI.NameOnlyMode();
        healthUI.Fade(show);
    }

    public void SetBuffsToApplyVisual(List<ChanceOfInflictingStatusEffect> buffs)
    {
        healthUI.SetBuffsToDisplay(buffs);
    }

    public void ActivateBuffHealthVisual()
    {
        healthUI.ShowBuffsOnly();
    }

    public void SetupHealthUI()
    {
        healthUI.Setup(myUnit, GetHealthNormalized());
    }

    private void SetIntializationData()
    {
        if (myUnit) { return; } //Means Data grabbed Already

        unitAnimator = GetComponentInChildren<GridUnitAnimator>();
        myUnit = GetComponent<CharacterGridUnit>();
        healthUI = healthCanvas.GetComponent<UnitHealthUI>();
        IDamageable = this;

        isPlayer = myUnit as PlayerGridUnit;
    }

    private void SetHealthBar(float value)
    {
        healthUI.ShowDamage(value);
    }

    private void UpdateHUD()
    {
        PlayerGridUnit player = myUnit as PlayerGridUnit;

        if (player && !FantasyCombatManager.Instance.restartingBattle)
        {
            HUDManager.Instance.UpdateUnitHealth(player);
            HUDManager.Instance.UpdateUnitSP(player);
            HUDManager.Instance.UpdateUnitFP(player);
        }
    }

    public void SetKnockDown(bool value)
    {
        isKnockedDown = value;
    }

    public void SetFiredUp(bool value)
    {
        isFiredUp = value;
        PlayerGridUnit player = myUnit as PlayerGridUnit;

        if (player)
        {
            HUDManager.Instance.ShowFiredUpSun(player, value);
        }
    }

    public void ShowKnockdownText()
    {
        healthUI.KnockDown();
    }

    public bool WillDamageKO(int damage)
    {
        return currentHealth - damage <= 0;
    }

    public int MaxFP()
    {
        return maxFP;
    }

    public int GetVitalValueFromPercentage(float percentage, Vital vital)
    {
        switch (vital)
        {
            case Vital.HP:
                return Mathf.RoundToInt((percentage / 100) * MaxHealth());
            case Vital.SP:
                return Mathf.RoundToInt((percentage / 100) * MaxSP());
            case Vital.FP:
                return Mathf.RoundToInt((percentage / 100) * MaxFP());
            default:
                return Mathf.RoundToInt((percentage / 100) * MaxHealth());
        }
    }

    public void AdjustCurrentVitals()
    {
        currentHealth = Mathf.Min(currentHealth, MaxHealth());
        currentSP = Mathf.Min(currentSP, MaxSP());
    }

    //Saving
    [System.Serializable]
    public class HealthState
    {
        //vitals
        public int currentHealth;
        public int currentSP;
        public int currentFP;
    }


    public object CaptureState()
    {
        healthState.currentHealth = currentHealth;
        healthState.currentSP = currentSP;
        healthState.currentFP = currentFP;

        return SerializationUtility.SerializeValue(healthState, DataFormat.Binary);
    }


    public void RestoreState(object state)
    {
        isDataRestored = true;

        if (state == null) 
        {
            NewGameSetup();
            return; 
        }

        byte[] bytes = state as byte[];
        healthState = SerializationUtility.DeserializeValue<HealthState>(bytes, DataFormat.Binary);

        ClearDamageData();

        currentHealth = healthState.currentHealth;
        currentSP = healthState.currentSP;
        currentFP = healthState.currentFP;

        isGuarding = false;
        isKnockedDown = false;
        isKOed = currentHealth <= 0;

        if (!isPlayer && isKOed)
        {
            myUnit.ActivateUnit(false);
        }
        else if(isPlayer)
        {
            myUnit.ActivateUnit(true);
        }
        else if (TryGetComponent(out EnemyStateMachine enemyStateMachine) && !enemyStateMachine.enemyGroup.disableMembersOnStart)
        {
            myUnit.ActivateUnit(true);
            enemyStateMachine.SwitchState(enemyStateMachine.patrollingState);
        }

        //UpdateHUD(); Health UI updated when Combat Manager Intializes Unit for battle.
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
