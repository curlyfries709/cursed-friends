
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

    public CharacterGridUnit attacker { get; private set; }
    public int currentHealth { get; set; }
    public int currentSP { get; set; }
    public int currentFP { get; set; }

    DamageData currentDamageData = null;

    //bool beginHealthCountdown = false;

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
        ClearDamageData();

        FinalizeDamageData(attackData, damageType);

        return currentDamageData;
    }

    public void TakeStatusEffectDamage(AttackData attackData, int healthPercentToRemove)
    {
        if (isKOed) { return; }
        ClearDamageData();

        FinalizeDamageData(attackData, DamageType.StatusEffect, healthPercentToRemove);
    }

    public void TakeBumpDamage(int damage)
    {
        if (isKOed) { return; }

        currentDamageData = new DamageData(myUnit, null, null);
        currentDamageData.damageReceived = damage;

        FinalizeDamageData(null, DamageType.KnockbackBump);
    }

    private void FinalizeDamageData(AttackData attackData, DamageType damageType, int healthPercent = 0)
    {
        //Setup Damage
        if (damageType == DamageType.Default)
        {
            attackData.canEvade = attackData.canEvade && !StatusEffectManager.Instance.IsUnitDisabled(myUnit);
        }

        attacker = attackData.attacker;
 
        //Calculate Damage
        switch (damageType)
        {
            case DamageType.StatusEffect:
                currentDamageData = TheCalculator.Instance.CalculateStatusEffectDamage(myUnit, attackData, healthPercent);
                break;
            default:
                currentDamageData = TheCalculator.Instance.CalculateDamageReceived(attackData, myUnit, damageType, isGuarding);
                break;
        }

        Affinity currentAffinity = currentDamageData.affinityToAttack;

        //Subscribe or prep events
        if(currentAffinity == Affinity.Evade)
        {
            Evade.Instance.PrepUnitToEvade(attacker, myUnit);
        }
        else
        {
            IDamageable.TriggerHealthChangeEvent += TriggerDamageEvent;
        }
        
        //Knockback or suction
        if (CanApplyForces(currentDamageData))
        {
            SkillForce.Instance.PrepareToApplyForceToUnit(attacker, myUnit, currentDamageData.hitByAttackData.forceData, attackData.rawDamage);
        }
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
        healthUI.DisplayUI(null, GetHealthNormalized(), true);

        if(passiveHeal)
        {
            FantasyCombatManager.Instance.BeginPassiveHealRoutine(myUnit);
        }
        else
        {
            //IDamageable.BeginHealthUICountdown(true);
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

    private void TriggerDamageEvent(bool triggerEvent)
    {
        //UnSubscribe from event
        IDamageable.TriggerHealthChangeEvent -= TriggerDamageEvent;

        if (!triggerEvent) //Means event was cancelled.
        {
            ClearDamageData();
            return;
        }

        //Update Damage Display Time
        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(currentDamageData.affinityToAttack, currentDamageData.isKOHit, currentDamageData.isKnockdownHit);
        
        Affinity currentAffinity = currentDamageData.affinityToAttack;

        //Update Health; Trigger Reflect; Apply & Activate Status Effects
        UpdateHealthFromAffinity(currentDamageData.hitByAttackData, currentAffinity); //Status Effects Applied here & Reflect triggered.

        //Update Enemy Database if enemy.
        if (!isPlayer)
        {
            EnemyDatabase.Instance.UpdateEnemyData(myUnit.stats.data, currentDamageData.hitByAttackData);
        }

        switch (currentAffinity)
        {
            case Affinity.Evade://Evade UI & Animation Triggered By Evade Script
                return;
            case Affinity.Immune:
            case Affinity.Reflect:
            case Affinity.Absorb:
                ShowHealthUI();
                break;
            case Affinity.Weak:
            case Affinity.Resist:
            case Affinity.None:
            default:
                OnDamageTaken();
                break;
               
        }

        //Raise Hit Event
        IDamageable.UnitHit(currentDamageData);
    }
    public void TriggerEvadeEvent()
    {
        //Gain FP
        GainFP(CalculateFPGain(true));

        healthUI.Evade();

        //Update Damage Display Time
        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(Affinity.Evade, false, false);
    }

    private void UpdateHealthFromAffinity(AttackData attackData, Affinity currentAffinity)
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
        else if (currentAffinity == Affinity.Reflect)
        {
            //Call Reflect Damage Event
            Reflect.DamageReflected?.Invoke(attacker, myUnit, attackData);
        }
        else
        {
            //Else Deduct Damage.
            currentHealth = currentHealth - currentDamageData.damageReceived;
            ApplyAndActivateStatusEffects(currentDamageData.afflictedStatusEffects);
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
        //Contact Status Effect Manager
        int numberOfSEApplied = ApplyAndActivateStatusEffects(currentDamageData.afflictedStatusEffects);

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

        ShowHealthUI();
    }

    private void KO()
    {
        ShowHealthUI();
        isKOed = true;
        
        CharacterUnitKOed(myUnit);

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
    }

    private int ApplyAndActivateStatusEffects(List<InflictedStatusEffectData> statusEffectsToApply)
    {
        if (currentHealth > 0)
        {
            List<ChanceOfInflictingStatusEffect> statusEffects = new List<ChanceOfInflictingStatusEffect>();

            //Apply Status Effects if still alive
            foreach (InflictedStatusEffectData inflictedStatusEffect in statusEffectsToApply)
            {
                if (StatusEffectManager.Instance.ApplyAndActivateStatusEffect(inflictedStatusEffect.effectData, myUnit, attacker, inflictedStatusEffect.numOfTurns, inflictedStatusEffect.buffChange))
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

    public void TakeSPLoss(int percentage)
    {
        int amount = Mathf.RoundToInt((percentage / 100f) * myUnit.stats.GetStaminaWithoutBonus());
        currentSP = Mathf.Max(0, currentSP - amount);

        //Update Health UI Number.
        healthUI.SetSPChangeNumberText(amount);
    }

    public void Guard(bool beginGuarding)
    {
        isGuarding = beginGuarding;
        unitAnimator.SetBool(unitAnimator.animIDGuarding, isGuarding);

        UpdateHUD();
    }

    public void Knockdown()
    {
        if (!isKnockedDown && !isKOed && !currentDamageData.isKOHit)
        {
            //Raise knocked down event
            isKnockedDown = true;
            KnockdownEvent.UnitKnockdown(attacker);
        }
    }

    private void ShowHealthUI()
    {
        healthUI.DisplayUI(currentDamageData, GetHealthNormalized(), currentDamageData.affinityToAttack == Affinity.Absorb);
        UpdateHUD();
    }

    private void ClearDamageData()
    {
        currentDamageData = null;
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

    protected bool CanApplyForces(DamageData damageData)
    {
        bool immuneToForces = myUnit.stats.IsImmuneToForces();
        Affinity affinity = damageData.affinityToAttack;

        if(immuneToForces || affinity == Affinity.Evade || affinity == Affinity.Reflect)
        {
            return false;
        }

        return damageData.hitByAttackData.forceData.forceType != SkillForceType.None;
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
