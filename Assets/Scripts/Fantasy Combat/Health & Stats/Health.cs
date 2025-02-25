using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Health : MonoBehaviour, ISaveable
{
    [Header("UI")]
    [SerializeField] protected GameObject healthCanvas;
    [SerializeField] protected Transform statusEffectVisualHeader;
    [Header("DAMAGE FEEDBACKS")]
    [SerializeField] protected AffinityFeedback damageFeedbacks;

    //Caches
    protected GridUnit myUnit;
    protected CharacterGridUnit myCharacter;

    protected UnitHealthUI healthUI;

    protected bool isPlayer = false;

    //Events
    public static Action<DamageData> UnitHit;
    public static Action<GridUnit> UnitKOed;

    public static Action<bool> TriggerHealthChangeEvent; //True if health change was successful, false if health change was cancelled. 

    //Event Bools
    protected bool subscribedToHealthUIEvent = false;
    protected bool subscribedToHealthChangeEvent = false;

    public GridUnit attacker { get; protected set; }
    public int currentHealth { get; protected set; }

    //Data
    protected List<HealthChangeData> currentHealthChangeDatas = new List<HealthChangeData>();

    protected DamageData currentDamageData = null;
    protected HealData currentHealData = null;

    //State bools
    public bool isKOed { get; protected set; }

    //SAVING
    //Saving Data
    [SerializeField, HideInInspector]
    protected HealthState healthState = new HealthState();
    protected bool isDataRestored = false;

    //Saving
    [System.Serializable]
    public class HealthState
    {
        //vitals
        public int currentHealth;
        public int currentSP;
        public int currentFP;
    }

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

    //ABSTRACT

    protected abstract void TriggerHealEvent();
    protected abstract void Hit();

    protected abstract void KO();

    public abstract int MaxHealth();

    public abstract object CaptureState();
    public abstract void RestoreState(object state);

    //END ABSTRACT

    //DAMAGE
    public virtual DamageData TakeDamage(AttackData attackData, DamageType damageType)
    {
        if (isKOed) { return null; }

        return FinalizeDamageData(attackData, damageType);
    }

    public void TakeStatusEffectDamage(AttackData attackData, int healthPercentToRemove)
    {
        if (isKOed) { return; }

        FinalizeDamageData(attackData, DamageType.StatusEffect, healthPercentToRemove);
    }

    public virtual void TakeBumpDamage(int damage)
    {
        if (isKOed) { return; }

        FinalizeDamageData(null, DamageType.KnockbackBump, damage);
    }

    protected DamageData FinalizeDamageData(AttackData attackData, DamageType damageType, int optionalNumValue = 0)
    {
        DamageData newDamageData;
        attacker = attackData != null ? attackData.attacker : null;

        //Setup Damage
        if (damageType == DamageType.Default)
        {
            attackData.canEvade = attackData.canEvade && !StatusEffectManager.Instance.IsUnitDisabled(myCharacter);
        }

        //Calculate Damage
        switch (damageType)
        {
            case DamageType.StatusEffect:
                newDamageData = TheCalculator.Instance.CalculateStatusEffectDamage(myCharacter, attackData, optionalNumValue);
                break;
            case DamageType.KnockbackBump:
                newDamageData = new DamageData(myUnit, null, null);
                newDamageData.damageReceived = optionalNumValue;
                break;
            default:
                CharacterHealth characterHealth = this as CharacterHealth;
                newDamageData = TheCalculator.Instance.CalculateDamageReceived(attackData, myCharacter, damageType, characterHealth ? characterHealth.isGuarding : false);
                break;
        }

        Affinity currentAffinity = newDamageData.affinityToAttack;

        //Subscribe or prep events
        if (currentAffinity == Affinity.Evade)
        {
            Evade.Instance.PrepUnitToEvade(attacker, myCharacter);
            currentHealthChangeDatas.Add(null); //Pad the list with null data. 
            Debug.Log("Adding Null Evade Data for : " + myCharacter.unitName);

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

    protected void ReflectDamage(DamageData damageData)
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
        HealData newHealData = TheCalculator.Instance.CalculateHealReceived(healData);

        //Subscribe to damage event instead
        if (newHealData.convertToDamage)
        {
            //Create damage data
            DamageData newDamageData = new DamageData(myUnit, null, Affinity.None, newHealData.HPRestore);
            currentHealthChangeDatas.Add(newDamageData);
            return;
        }

        //Suction only
        if (newHealData.forceData.forceType == SkillForceType.KnockbackAll)
            Debug.LogError("HEALING SKILLS SHOULD NOT HAVE FORCE TYPE OF KNOCKBACK ALL. PLEASE FIX");

        if (CanApplyForces(newHealData))
        {
            SkillForce.Instance.PrepareToApplyForceToUnit(attacker, myUnit, newHealData.forceData, 0, false);
        }
        else if (newHealData.HPRestore <= 0 && !myCharacter) //Do not bother if this is object and only restore SP or FP
        {
            return;
        }
        else if (newHealData.IsOnlyFPRestore() && StatusEffectManager.Instance.ProhibitUnitFPGain(myCharacter))
        {
            return;
        }

        //Subscribe to event if not already
        ListenToHealthChangeEvent(true);

        currentHealthChangeDatas.Add(newHealData);
    }

    //EVENT RESPONDERS
    protected void OnHealthChangeEvent(bool triggerEvent)
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
        else if (currentHealthChangeDatas[0] is HealData healData)
        {
            currentHealData = healData;
        }

        //Remove from list
        currentHealthChangeDatas.RemoveAt(0);

        ListenToHealthUICompleteEvent(currentHealthChangeDatas.Count > 0);

        //Trigger relevant event
        if (currentDamageData != null)
        {
            TriggerDamageEvent();
        }
        else if (currentHealData != null)
        {
            TriggerHealEvent();
        }
        else //Else means it's null, so it's an evade
        {
            Debug.Log("Evade Detected for: " + myUnit.unitName);
        }
    }

    protected void TriggerDamageEvent()
    {
        //Update Damage Display Time
        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(currentDamageData.affinityToAttack, currentDamageData.isKOHit, currentDamageData.isKnockdownHit);

        Affinity currentAffinity = currentDamageData.affinityToAttack;

        //Update Health
        UpdateHealthFromAffinity(currentAffinity);

        //Update Enemy Database if enemy.
        if (!isPlayer && !IsObject())
        {
            EnemyDatabase.Instance.UpdateEnemyData(myCharacter.stats.data, currentDamageData.hitByAttackData);
        }

        //Raise Hit Event
        UnitHit?.Invoke(currentDamageData);

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

    //EVENT TRIGGERS & LISTENERS
    public static void RaiseHealthChangeEvent(bool canTrigger)
    {
        TriggerHealthChangeEvent?.Invoke(canTrigger);

        /*if(canTrigger)
            FantasyCombatManager.Instance.BeginHealthUICountdown();*/
    }

    protected void ListenToHealthChangeEvent(bool listen)
    {
        if (listen)
        {
            if (!subscribedToHealthChangeEvent)
            {
                TriggerHealthChangeEvent += OnHealthChangeEvent;
                subscribedToHealthChangeEvent = true;
            }
        }
        else
        {
            if (subscribedToHealthChangeEvent)
            {
                TriggerHealthChangeEvent -= OnHealthChangeEvent;
                subscribedToHealthChangeEvent = false;
            }
        }
    }

    protected void ListenToHealthUICompleteEvent(bool listen)
    {
        if (listen)
        {
            if (!subscribedToHealthUIEvent)
            {
                healthUI.HealthUIComplete += OnHealthChangeEvent;
                subscribedToHealthUIEvent = true;
            }
        }
        else
        {
            if (subscribedToHealthUIEvent)
            {
                healthUI.HealthUIComplete -= OnHealthChangeEvent;
                subscribedToHealthUIEvent = false;
            }
        }
    }

    //HELPERS

    protected virtual void ShowHealthUI(Affinity affinity, HealthChangeData healthChangeData)
    {
        bool isHealing = healthChangeData == null ? false :
            !(healthChangeData is DamageData) || (healthChangeData as DamageData).affinityToAttack == Affinity.Absorb;

        healthUI.DisplayHealthChangeUI(affinity, healthChangeData, GetHealthNormalized(), isHealing, currentHealthChangeDatas.Count > 0);
    }

    protected void ClearAllData()
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

    public void TryGiveAttackerFP(bool isEnhancedAction, int numOfSEApplied = 0)
    {
        AttackerAsCharacter()?.Health().GainFP(TheCalculator.Instance.CalculateFPGain(isEnhancedAction, numOfSEApplied));
    }

    //GETTERS
    protected bool CanApplyForces(HealthChangeData healthChangeData)
    {
        bool immuneToForces = myUnit.stats.IsImmuneToForces();

        if (immuneToForces)
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

    public int GetPredictedCurrentHealth()
    {
        int predictedHealth = currentHealth;

        foreach (HealthChangeData healthChangeData in currentHealthChangeDatas)
        {
            if (healthChangeData is DamageData damageData)
            {
                if (damageData.affinityToAttack == Affinity.Absorb)
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

            if (predictedHealth <= 0) //If it ever drops to 0 or below, immediately retun 0 cos at that point they are KOed. 
            {
                return 0;
            }
        }

        return predictedHealth;
    }

    public AffinityFeedback GetDamageFeedbacks(Transform transformToPlayVFX, GameObject VFXToPlay)
    {
        SetVFXToPlay(myUnit, damageFeedbacks, transformToPlayVFX, VFXToPlay);
        return damageFeedbacks;
    }

    //SETUPS
    protected virtual void NewGameSetup()
    {
        SetIntializationData();

        currentHealth = MaxHealth();

        isKOed = false;

        SetupHealthUI();
    }

    public virtual void ResetStateToBattleStart(int healthAtStart, int spAtStart, int fpAtStart)
    {
        PlayerGridUnit player = myUnit as PlayerGridUnit;

        if (player)
            player.ActivateGridCollider(true);

        isKOed = false;

        currentHealth = healthAtStart;
        myCharacter?.ActivateUnit(true);

        myCharacter?.unitAnimator.ResetAnimatorToCombatState();
        SetupHealthUI();
    }

    public virtual void ResetVitals()
    {
        isKOed = false;

        currentHealth = MaxHealth();
        myCharacter?.unitAnimator.ResetAnimatorToRoamState();

        SetupHealthUI();
    }

    public void SetVFXToPlay(GridUnit myUnit, AffinityFeedback feedbacks, Transform transformToPlayVFX, GameObject VFXToPlay)
    {
        //Deactive all children so VFX Doesn't trigger when detached
        foreach (Transform child in feedbacks.spawnVFXHeader)
        {
            child.gameObject.SetActive(false);
        }

        //Unparent Children
        feedbacks.spawnVFXHeader.DetachChildren();

        if (!VFXToPlay) { return; }

        VFXToPlay.transform.parent = null;

        Vector3 spawnHitDestination = myUnit.GetClosestPointOnColliderToPosition(transformToPlayVFX.position) + (transformToPlayVFX.forward.normalized * 0.25f);

        VFXToPlay.transform.position = spawnHitDestination;
        VFXToPlay.transform.rotation = transformToPlayVFX.rotation;

        VFXToPlay.transform.parent = feedbacks.spawnVFXHeader;
    }

    //SETTERS
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

    protected void SetIntializationData()
    {
        if (myUnit) { return; } //Means Data grabbed Already

        myUnit = GetComponent<GridUnit>();
        myCharacter = myUnit as CharacterGridUnit;
        healthUI = healthCanvas.GetComponent<UnitHealthUI>();

        isPlayer = myCharacter as PlayerGridUnit;
    }


    public UnitHealthUI GetHealthUI()
    {
        return healthUI;
    }

    public void SetupHealthUI()
    {
        healthUI.Setup(myUnit, GetHealthNormalized());
    }

    //GETTERS
    public float GetHealthNormalized()
    {
        return (float)currentHealth / MaxHealth();
    }

    public bool IsObject()
    {
        return this is ObjectHealth;
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }

    protected CharacterGridUnit AttackerAsCharacter()
    {
        return attacker as CharacterGridUnit;
    }
}
