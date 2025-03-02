
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.Serialization;
using AnotherRealm;

public class CharacterHealth : Health
{
    [Header("Character Health")]
    [Range(25, 100)]
    [SerializeField] int maxFP;
    [Space(10)]
    [SerializeField] Transform unitStatusEffectHeader;

    //Vitals
    public int currentSP { get; protected set; }
    public int currentFP { get; protected set; }

    //State Bools
    public bool isGuarding { get; private set; }
    public bool isKnockedDown { get; private set; }

    public bool isFiredUp { get; private set; }

    //Events
    public static Action<CharacterGridUnit> CharacterUnitRevived;


    //EVENTS
    public void TriggerEvadeEvent() //Called via Evade class
    {
        Debug.Log("Triggering Evade Event for: " + healthUI.GetUnitDisplayName());
        currentHealthChangeDatas.RemoveAt(0); //Remove the null data from the list. 

        //Update Damage Display Time
        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(Affinity.Evade, false, false, currentHealthChangeDatas.Count > 0);

        //Play Feedback
        CombatFunctions.PlayAffinityFeedback(Affinity.Evade, damageFeedbacks);

        //Gain FP
        GainFP(TheCalculator.Instance.CalculateFPGain(true));

        ShowHealthChangeUI(Affinity.Evade, null);
    }

    protected override void TriggerHealEvent()
    {
        //Update display time
        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(Affinity.None, false, false, currentHealthChangeDatas.Count > 0);

        int SPRestore = currentHealData.SPChange;
        int HPRestore = currentHealData.HPChange;
        int FPRestore = currentHealData.FPChange;

        //Update Vitals
        currentSP = Mathf.Min(currentSP + SPRestore, MaxSP());
        currentHealth = Mathf.Min(currentHealth + HPRestore, MaxHealth());

        if (FPRestore > 0)
        {
            GainFP(FPRestore, false);
        }

        //Revive Functionality
        if (isKOed && currentHealData.canRevive)
        {
            isKOed = false;

            if (isPlayer)
                (myCharacter as PlayerGridUnit).ActivateGridCollider(true);

            myCharacter.unitAnimator.SetTrigger(myCharacter.unitAnimator.animIDRevive);

            CharacterUnitRevived(myCharacter);
        }

        //Apply Status Effects
        ApplyAndActivateStatusEffects(currentHealData.inflictedStatusEffects);

        //Show Health
        ShowHealthChangeUI(Affinity.None, currentHealData);

        //Clear current heal data
        currentHealData = null;
    }

    protected override void Hit()
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
            myCharacter.unitAnimator.Hit();
        }

        //Give Attacker FP
        if (currentDamageData.affinityToAttack != Affinity.Resist)
        {
            TryGiveAttackerFP(currentDamageData.isCritical || currentDamageData.isBackstab || currentDamageData.isKnockdownHit || currentDamageData.isKOHit, numberOfSEApplied);
        }

        ShowHealthChangeUI(currentDamageData.affinityToAttack, currentDamageData);
    }

    protected override void KO()
    {
        ShowHealthChangeUI(currentDamageData.affinityToAttack, currentDamageData);
        isKOed = true;

        myCharacter.unitAnimator.KO();

        //Give FP, then Deplete FP
        TryGiveAttackerFP(true);

        PlayerGridUnit player = myCharacter as PlayerGridUnit;

        if (player)
            player.ActivateGridCollider(false);
            
        LoseFP(true);
        isGuarding = false;

        UnitKOed?.Invoke(myUnit);
    }

    private int ApplyAndActivateStatusEffects(List<InflictedStatusEffectData> statusEffectsToApply)
    {
        if (currentHealth > 0)
        {
            GridUnit inflictor = currentDamageData != null ? currentDamageData.attacker : currentHealData.healer;
            List<ChanceOfInflictingStatusEffect> statusEffects = new List<ChanceOfInflictingStatusEffect>();

            //Apply Status Effects if still alive
            foreach (InflictedStatusEffectData inflictedStatusEffect in statusEffectsToApply)
            {
                if (StatusEffectManager.Instance.ApplyAndActivateStatusEffect(inflictedStatusEffect.effectData, myCharacter, inflictor, inflictedStatusEffect.numOfTurns, inflictedStatusEffect.buffChange))
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

    public void LoseSP(int amount)
    {
        if(amount == 0) { return; }
        currentSP = Mathf.Max(0, currentSP - amount);
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
        myCharacter.unitAnimator.SetBool(myCharacter.unitAnimator.animIDGuarding, isGuarding);

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

    public override void ShowHealthChangeUI(Affinity affinity, HealthChangeData healthChangeData)
    {
        base.ShowHealthChangeUI(affinity, healthChangeData);
        UpdateHUD();
    }

    //FP
    public void GainFP(int gain, bool updateHUD = true)
    {
        if (StatusEffectManager.Instance.ProhibitUnitFPGain(myCharacter)) { return; }

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
            currentFP = TheCalculator.Instance.CalculateNewFPAfterLoss(currentFP);
        }

        UpdateHUD();
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
    protected override void NewGameSetup()
    {
        SetIntializationData();

        myCharacter.stats.UpdateSubAndMainAttributes();

        currentSP = MaxSP();
        currentFP = 0;

        isGuarding = false;

        base.NewGameSetup();
    }

    public void BattleComplete() //Called By Victory & Fled
    {
        isGuarding = false;
        if (!isKOed) { return; }

        isKOed = false;
        currentHealth = 1;

        PlayerGridUnit player = myCharacter as PlayerGridUnit;

        if (player)
            player.ActivateGridCollider(true);

        UpdateHUD();
    }

    public override void ResetStateToBattleStart(int healthAtStart, int spAtStart, int fpAtStart)
    {
        isGuarding = false;
        isFiredUp = false;
        isKnockedDown = false;

        currentSP = spAtStart;
        currentFP = fpAtStart;

        base.ResetStateToBattleStart(healthAtStart, spAtStart, fpAtStart);
    }

    public override void ResetVitals()
    {
        isKOed = false;
        isGuarding = false;
        isFiredUp = false;
        isKnockedDown = false;

        currentHealth = MaxHealth();
        currentSP = MaxSP();
        currentFP = 0;

        base.ResetVitals();
    }

    //Getters

    public bool CanTriggerFiredUp()
    {
        return currentFP >= maxFP && !StatusEffectManager.Instance.IsUnitDisabled(myCharacter);
    }

    public GameObject GetStatusEffectHeader()
    {
        return unitStatusEffectHeader.gameObject;
    }

    public Transform GetStatusEffectUIHeader()
    {
        return healthUI.StatusEffectHeader();
    }



    public float GetStaminaNormalized()
    {
        return (float)currentSP / MaxSP();
    }

    public float GetFPNormalized()
    {
        return (float)currentFP / maxFP;
    }

    public override int MaxHealth()
    {
        return myCharacter.stats.Vitality;
    }

    public int MaxSP()
    {
        return myCharacter.stats.Stamina;
    }

    public bool CanTriggerAssistAttack() //Such as a bump attack or bond ability
    {
        return !isGuarding && !StatusEffectManager.Instance.IsUnitDisabled(myCharacter) && !isKOed;
    }

    //Setters
    private void UpdateHUD()
    {
        PlayerGridUnit player = myCharacter as PlayerGridUnit;

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
        PlayerGridUnit player = myCharacter as PlayerGridUnit;

        if (player)
        {
            HUDManager.Instance.ShowFiredUpSun(player, value);
        }
    }

    public void ShowKnockdownText()
    {
        healthUI.KnockDown();
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

    //SAVING
    public override object CaptureState()
    {
        healthState.currentHealth = currentHealth;
        healthState.currentSP = currentSP;
        healthState.currentFP = currentFP;

        return SerializationUtility.SerializeValue(healthState, DataFormat.Binary);
    }

    public override void RestoreState(object state)
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
            myCharacter.ActivateUnit(false);
        }
        else if(isPlayer)
        {
            myCharacter.ActivateUnit(true);
        }
        else if (TryGetComponent(out EnemyStateMachine enemyStateMachine) && !enemyStateMachine.enemyGroup.disableMembersOnStart)
        {
            myCharacter.ActivateUnit(true);
            enemyStateMachine.SwitchState(enemyStateMachine.patrollingState);
        }

        //UpdateHUD(); Health UI updated when Combat Manager Intializes Unit for battle.
    }

}
