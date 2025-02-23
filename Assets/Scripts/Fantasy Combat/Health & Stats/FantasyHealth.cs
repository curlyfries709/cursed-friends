
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

    //Bools
    bool subscribedToHealthUIEvent = false;
    bool subscribedToHealthChangeEvent = false;

    public CharacterGridUnit attacker { get; private set; }
    public int currentHealth { get; set; }
    public int currentSP { get; set; }
    public int currentFP { get; set; }

    //Data
    List<HealthChangeData> currentHealthChangeDatas = new List<HealthChangeData>();

    DamageData currentDamageData = null;
    HealData currentHealData = null;

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
    public DamageData TakeDamage(AttackData attackData, DamageType damageType)
    {
        if (isKOed) { return null; }

        return FinalizeDamageData(attackData, damageType);
    }

    public void TakeStatusEffectDamage(AttackData attackData, int healthPercentToRemove)
    {
        if (isKOed) { return; }

        FinalizeDamageData(attackData, DamageType.StatusEffect, healthPercentToRemove);
    }

    public void TakeBumpDamage(int damage)
    {
        if (isKOed) { return; }

        FinalizeDamageData(null, DamageType.KnockbackBump,  damage);
    }

    private DamageData FinalizeDamageData(AttackData attackData, DamageType damageType, int optionalNumValue = 0)
    {
        DamageData newDamageData;
        attacker = attackData != null ? attackData.attacker : null;

        //Setup Damage
        if (damageType == DamageType.Default)
        {
            attackData.canEvade = attackData.canEvade && !StatusEffectManager.Instance.IsUnitDisabled(myUnit);
        }

        //Calculate Damage
        switch (damageType)
        {
            case DamageType.StatusEffect:
                newDamageData = TheCalculator.Instance.CalculateStatusEffectDamage(myUnit, attackData, optionalNumValue);
                break;
            case DamageType.KnockbackBump:
                newDamageData = new DamageData(myUnit, null, null);
                newDamageData.damageReceived = optionalNumValue;
                break;
            default:
                newDamageData = TheCalculator.Instance.CalculateDamageReceived(attackData, myUnit, damageType, isGuarding);
                break;
        }

        Affinity currentAffinity = newDamageData.affinityToAttack;

        //Subscribe or prep events
        if(currentAffinity == Affinity.Evade)
        {
            Evade.Instance.PrepUnitToEvade(attacker, myUnit);
            currentHealthChangeDatas.Add(null); //Pad the list with null data. 
            Debug.Log("Adding Null Evade Data for : " + myUnit.unitName);

            return newDamageData;
        }
        else
        {
            //Subscribe to event if not already
            ListenToHealthChangeEvent(true);

            if (currentAffinity == Affinity.Reflect)
            {
                //Call Attacker take damage so their damage taken is display at same time of reflect 
                ReflectDamage(newDamageData);
            }
        }
        
        //Knockback or suction
        if (CanApplyForces(newDamageData))
        {
            SkillForce.Instance.PrepareToApplyForceToUnit(attacker, myUnit, newDamageData.hitByAttackData.forceData, attackData.rawDamage, damageType == DamageType.Reflect);
        }

        //Add to list
        currentHealthChangeDatas.Add(newDamageData);

        return newDamageData;
    }

    private void ReflectDamage(DamageData damageData)
    {
        //Update attack data
        AttackData attackData = new AttackData(damageData.hitByAttackData);

        attackData.rawDamage = damageData.damageReceived;
        attackData.attacker = myUnit;
        attackData.numOfTargets = 1;
        attackData.canEvade = false;
        attackData.inflictedStatusEffects = damageData.inflictedStatusEffects;
        
        attackData.appliedModifiers.Clear();

        attacker.Health().TakeDamage(attackData, DamageType.Reflect);
    }

    //Heal

    public void Heal(HealData healData)
    {
        //Subscribe to event if not already
        ListenToHealthChangeEvent(true);

        HealData newHealData = TheCalculator.Instance.CalculateHealReceived(healData);

        //Subscribe to damage event instead
        if (newHealData.convertToDamage)
        {
            //Create damage data
            DamageData newDamageData = new DamageData(myUnit, null, Affinity.None, newHealData.HPRestore);
            currentHealthChangeDatas.Add(newDamageData);
            return;
        }

        if (newHealData.IsOnlyFPRestore() && StatusEffectManager.Instance.ProhibitUnitFPGain(myUnit)) { return; }

        //Suction only
        if (newHealData.forceData.forceType == SkillForceType.KnockbackAll)
            Debug.LogError("HEALING SKILLS SHOULD NOT HAVE FORCE TYPE OF KNOCKBACK ALL. PLEASE FIX");

        if (CanApplyForces(newHealData))
        {
            SkillForce.Instance.PrepareToApplyForceToUnit(attacker, myUnit, newHealData.forceData, 0, false);
        }

        currentHealthChangeDatas.Add(newHealData);
    }

    //EVENTS
    private void TriggerHealthChangeEvent(bool triggerEvent)
    {
        if (!triggerEvent) //Means event was cancelled.
        {
            ClearAllData();
            return;
        }

        Debug.Log("Triggering health change Event for: " + myUnit.unitName);
        //UnSubscribe from event
        ListenToHealthChangeEvent(false);

        if (currentHealthChangeDatas[0] is DamageData damageData)
        {
            currentDamageData = damageData;
        }
        else if(currentHealthChangeDatas[0] is HealData healData)
        {
            currentHealData = healData;
        }

        //Remove from list
        currentHealthChangeDatas.RemoveAt(0);

        ListenToHealthUICompleteEvent(currentHealthChangeDatas.Count > 0);

        //Trigger relevant event
        if(currentDamageData != null)
        {
            TriggerDamageEvent();
        }
        else if(currentHealData != null)
        {
            TriggerHealEvent();
        }
        else //Else means it's null, so it's an evade
        {
            Debug.Log("Evade Detected for: " + myUnit.unitName);
        }
    }

    private void TriggerDamageEvent()
    {
        //Update Damage Display Time
        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(currentDamageData.affinityToAttack, currentDamageData.isKOHit, currentDamageData.isKnockdownHit);
        
        Affinity currentAffinity = currentDamageData.affinityToAttack;

        //Update Health
        UpdateHealthFromAffinity(currentAffinity);

        //Update Enemy Database if enemy.
        if (!isPlayer)
        {
            EnemyDatabase.Instance.UpdateEnemyData(myUnit.stats.data, currentDamageData.hitByAttackData);
        }

        //Raise Hit Event
        IDamageable.UnitHit(currentDamageData);

        switch (currentAffinity)
        {
            case Affinity.Evade: //Affinity shouldn't be evade if this function is being called.
                Debug.LogError("Affinity of evade found in trigger damage event. That's a problem!");
                return;
            case Affinity.Immune:
            case Affinity.Reflect:
            case Affinity.Absorb:
                ShowHealthUI(currentAffinity, currentDamageData);
                break;
            case Affinity.Weak:
            case Affinity.Resist:
            case Affinity.None:
            default:
                OnDamageTaken();
                break;
               
        }

        //Clear current Damage data
        if (isKOed)
        {
            ClearAllData();
        }
        else
        {
            currentDamageData = null;
        } 
    }
    public void TriggerEvadeEvent() //Called via Evade class
    {
        Debug.Log("Triggering Evade Event for: " + myUnit.unitName);
        currentHealthChangeDatas.RemoveAt(0); //Remove the null data from the list. 

        //Update Damage Display Time
        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(Affinity.Evade, false, false);

        //Gain FP
        GainFP(CalculateFPGain(true));

        ShowHealthUI(Affinity.Evade, null);
    }

    private void TriggerHealEvent()
    {
        //Update display time
        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(Affinity.None, false, false);

        int SPRestore = currentHealData.SPRestore;
        int HPRestore = currentHealData.HPRestore;
        int FPRestore = currentHealData.FPRestore;

        //Prepare Data to be shown.
        if (SPRestore > 0)
        {
            healthUI.SetSPChangeNumberText(SPRestore);
            currentSP = Mathf.Min(currentSP + SPRestore, MaxSP());
        }

        if (HPRestore > 0)
        {
            healthUI.SetHPChangeNumberText(HPRestore);
            currentHealth = Mathf.Min(currentHealth + HPRestore, MaxHealth());
        }

        if(FPRestore > 0)
        {
            GainFP(FPRestore, false);
        }

        //Revive Functionality
        if (isKOed && currentHealData.canRevive)
        {
            isKOed = false;

            if (isPlayer)
                (myUnit as PlayerGridUnit).ActivateGridCollider(true);

            myUnit.unitAnimator.SetTrigger(myUnit.unitAnimator.animIDRevive);

            CharacterUnitRevived(myUnit);
        }

        //Apply Status Effects
        ApplyAndActivateStatusEffects(currentHealData.inflictedStatusEffects);

        //Show Health
        ShowHealthUI(Affinity.None, currentHealData);

        //Clear current heal data
        currentHealData = null;
    }

    private void UpdateHealthFromAffinity(Affinity currentAffinity)
    {
        if (currentAffinity == Affinity.Evade)
        {
            //Do nothing
            return;
        }
        else if (currentAffinity == Affinity.Absorb)
        {
            //Heal By Damage Dealt
            currentHealth = currentHealth + currentDamageData.damageReceived;
            currentHealth = Mathf.Min(currentHealth, MaxHealth());
        }
        else
        {
            //Else Deduct Damage.
            currentHealth = currentHealth - currentDamageData.damageReceived;
        }

        //Prepare Data to be shown.
        healthUI.SetHPChangeNumberText(currentDamageData.damageReceived);

        //Clamp Health
        currentHealth = Mathf.Max(currentHealth, 0);
    }

    private void OnDamageTaken()
    {
        if (currentHealth <= 0)
        {
            KO();
            return;
        }

        Hit();
    }

    private void Hit()
    {
        //Apply Status Effects
        int numberOfSEApplied = ApplyAndActivateStatusEffects(currentDamageData.inflictedStatusEffects);

        if (currentDamageData.isKnockdownHit)
        {
            Knockdown();
        }
        else
        {
            //Trigger hit animation.
            unitAnimator.Hit();
        }

        //Give Attacker FP
        if (currentDamageData.affinityToAttack != Affinity.Resist && attacker)
        {
            attacker.Health().GainFP(CalculateFPGain(currentDamageData.isCritical || currentDamageData.isBackstab || currentDamageData.isKnockdownHit || currentDamageData.isKOHit, numberOfSEApplied));
        }

        ShowHealthUI(currentDamageData.affinityToAttack, currentDamageData);
    }

    private void KO()
    {
        ShowHealthUI(currentDamageData.affinityToAttack, currentDamageData);
        isKOed = true;
        
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

        CharacterUnitKOed(myUnit);
    }

    private int ApplyAndActivateStatusEffects(List<InflictedStatusEffectData> statusEffectsToApply)
    {
        if (currentHealth > 0)
        {
            CharacterGridUnit inflictor = currentDamageData != null ? currentDamageData.attacker : currentHealData.healer;
            List<ChanceOfInflictingStatusEffect> statusEffects = new List<ChanceOfInflictingStatusEffect>();

            //Apply Status Effects if still alive
            foreach (InflictedStatusEffectData inflictedStatusEffect in statusEffectsToApply)
            {
                if (StatusEffectManager.Instance.ApplyAndActivateStatusEffect(inflictedStatusEffect.effectData, myUnit, inflictor, inflictedStatusEffect.numOfTurns, inflictedStatusEffect.buffChange))
                {
                    ChanceOfInflictingStatusEffect statusEffect = new ChanceOfInflictingStatusEffect();

                    statusEffect.buffChange = inflictedStatusEffect.buffChange;
                    statusEffect.statusEffect = inflictedStatusEffect.effectData;
                    statusEffect.numOfTurns = inflictedStatusEffect.numOfTurns;

                    statusEffects.Add(statusEffect);
                }
            }

            if (statusEffects.Count > 0)
                SetBuffsToApplyVisual(statusEffects);

            return statusEffects.Count;
        }

        return 0;
    }

    private void ListenToHealthChangeEvent(bool listen)
    {
        if (listen)
        {
            if (!subscribedToHealthChangeEvent)
            {
                IDamageable.TriggerHealthChangeEvent += TriggerHealthChangeEvent;
                subscribedToHealthChangeEvent = true;
            }
        }
        else
        {
            if (subscribedToHealthChangeEvent)
            {
                IDamageable.TriggerHealthChangeEvent -= TriggerHealthChangeEvent;
                subscribedToHealthChangeEvent = false;
            }
        }
    }

    private void ListenToHealthUICompleteEvent(bool listen)
    {
        if (listen)
        {
            if (!subscribedToHealthUIEvent)
            {
                healthUI.HealthUIComplete += TriggerHealthChangeEvent;
                subscribedToHealthUIEvent = true;
            }
        }
        else
        {
            if (subscribedToHealthUIEvent)
            {
                healthUI.HealthUIComplete -= TriggerHealthChangeEvent;
                subscribedToHealthUIEvent = false;
            }
        }
    }

    public void TakeSPLoss(int percentage)
    {
        int amount = Mathf.RoundToInt((percentage / 100f) * myUnit.stats.GetStaminaWithoutBonus());
        currentSP = Mathf.Max(0, currentSP - amount);

        //Update Health UI Number.
        healthUI.SetSPChangeNumberText(amount);
    }
    public void OuterCombatRestore(int hpGain, int spGain, int fpGain)
    {
        currentHealth = Mathf.Min(currentHealth + hpGain, MaxHealth());
        currentSP = Mathf.Min(currentSP + spGain, MaxSP());
        currentFP = Mathf.Min(currentFP + fpGain, maxFP);

        UpdateHUD();
    }

    public void Guard(bool beginGuarding)
    {
        isGuarding = beginGuarding;
        unitAnimator.SetBool(unitAnimator.animIDGuarding, isGuarding);

        UpdateHUD();
    }

    private void Knockdown()
    {
        if (!isKnockedDown && !isKOed && !currentDamageData.isKOHit)
        {
            //Raise knocked down event
            isKnockedDown = true;
            KnockdownEvent.UnitKnockdown(attacker);
        }
    }

    private void ShowHealthUI(Affinity affinity, HealthChangeData healthChangeData)
    {
        bool isHealing = healthChangeData == null ? false: 
            !(healthChangeData is DamageData) || (healthChangeData as DamageData).affinityToAttack == Affinity.Absorb;

        healthUI.DisplayHealthChangeUI(affinity, healthChangeData, GetHealthNormalized(), isHealing, currentHealthChangeDatas.Count > 0);
        UpdateHUD();
    }

    private void ClearAllData()
    {
        //Clear list
        currentHealthChangeDatas.Clear();

        //Unsubscribe from events
        ListenToHealthChangeEvent(false);
        ListenToHealthUICompleteEvent(false);

        //Reset data.
        attacker = null;
        currentDamageData = null;
        currentHealData = null;
    }

    public void GainFP(int gain, bool updateHUD = true)
    {
        if (StatusEffectManager.Instance.ProhibitUnitFPGain(myUnit)) { return; }

        currentFP = Mathf.Min(currentFP + gain, maxFP);

        if(updateHUD)
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

    protected bool CanApplyForces(HealthChangeData healthChangeData)
    {
        bool immuneToForces = myUnit.stats.IsImmuneToForces();

        if(immuneToForces)
        {
            return false;
        }

        SkillForceType forceType = healthChangeData.forceData.forceType;

        if (healthChangeData is DamageData damageData)
        {
            if (damageData.affinityToAttack == Affinity.Evade || damageData.affinityToAttack == Affinity.Reflect)
            {
                return false;
            }
            
            /* //UNCOMMENT IF YOU DO NOT WANT SUCTION TO BE REFLECTED
             * if(damageData.damageType == DamageType.Reflect && forceType == SkillForceType.SuctionAll)
            {
                //Cannot reflect suction but can reflect knockback. 
                return false;
            }*/
        }

        return forceType != SkillForceType.None;
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

    public int GetPredictedCurrentHealth()
    {
        int predictedHealth = currentHealth;

        foreach(HealthChangeData healthChangeData in currentHealthChangeDatas)
        {
            if (healthChangeData is DamageData damageData)
            {
                if(damageData.affinityToAttack == Affinity.Absorb)
                {
                    predictedHealth = predictedHealth + damageData.damageReceived;
                }
                else
                {
                    predictedHealth = predictedHealth - damageData.damageReceived;
                }
            }
            else if (healthChangeData is HealData healData)
            {
                predictedHealth = predictedHealth + healData.HPRestore;
            }

            if(predictedHealth <= 0) //If it ever drops to 0 or below, immediately retun 0 cos at that point they are KOed. 
            {
                return 0; 
            }
        }

        return predictedHealth;
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

    public UnitHealthUI GetHealthUI()
    {
        return healthUI;
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

        ClearAllData();

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
