using AnotherRealm;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class Health : MonoBehaviour, ISaveable
{
    [Header("UI")]
    [SerializeField] protected GameObject healthCanvas;
    [Header("Transform Headers")]
    [SerializeField] protected Transform statusEffectVisualHeader;
    [SerializeField] protected Transform hitVFXHeader;
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

    public GridUnit mainAttacker { get; protected set; }
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
        hitVFXHeader.gameObject.SetActive(false);
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

    public virtual void TakeBumpDamage(int damage)
    {
        if (isKOed) { return; }

        FinalizeDamageData(null, DamageType.KnockbackBump, damage);
    }

    protected DamageData FinalizeDamageData(AttackData attackData, DamageType damageType, int optionalNumValue = 0)
    {
        DamageData newDamageData;
        mainAttacker = attackData != null ? attackData.mainInstigator : null;

        //Setup Damage
        if (damageType == DamageType.Default)
        {
            attackData.canEvade = attackData.canEvade && !StatusEffectManager.Instance.IsUnitDisabled(myCharacter);
        }

        //Calculate Damage
        switch (damageType)
        {
            case DamageType.StatusEffect:
                newDamageData = TheCalculator.Instance.CalculateStatusEffectDamage(myCharacter, attackData);
                break;
            case DamageType.KnockbackBump:
                newDamageData = new DamageData(myUnit, null, null);
                newDamageData.HPChange = optionalNumValue;
                break;
            default:
                CharacterHealth characterHealth = this as CharacterHealth;
                newDamageData = TheCalculator.Instance.CalculateDamageReceived(attackData, myUnit, damageType, characterHealth ? characterHealth.isGuarding : false);
                break;
        }

        Affinity currentAffinity = newDamageData.affinityToAttack;

        //Subscribe or prep events
        if (currentAffinity == Affinity.Evade)
        {
            Evade.Instance.PrepUnitToEvade(mainAttacker, myCharacter);
            currentHealthChangeDatas.Add(null); //Pad the list with null data. 
            Debug.Log("Adding Null Evade Data for : " + healthUI.GetUnitDisplayName());

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
            SkillForce.Instance.PrepareToApplyForceToUnit(mainAttacker, myUnit, newDamageData.hitByAttackData.forceData.Value, attackData.HPChange, damageType == DamageType.Reflect);
        }

        //Add to list
        currentHealthChangeDatas.Add(newDamageData);

        return newDamageData;
    }

    protected void ReflectDamage(DamageData damageData)
    {
        //Update attack data
        AttackData attackData = new AttackData(damageData.hitByAttackData);

        attackData.HPChange = damageData.HPChange;
        attackData.mainInstigator = damageData.mainInstigator;
        attackData.SetSupportInstigators(damageData.supportInstigators);

        attackData.numOfTargets = 1;
        attackData.canEvade = false;
        attackData.inflictedStatusEffects = damageData.inflictedStatusEffects;

        attackData.appliedModifiers.Clear();

        foreach(GridUnit instigator in damageData.GetInstigatorList())
        {
            instigator.Health().TakeDamage(attackData, DamageType.Reflect);
        }
    }

    //Heal
    public void Heal(HealData healData)
    {
        HealData newHealData = TheCalculator.Instance.CalculateHealReceived(healData);

        //Subscribe to damage event instead
        if (newHealData.convertToDamage)
        {
            //Create damage data
            DamageData newDamageData = new DamageData(myUnit, null, Affinity.None, newHealData.HPChange);
            currentHealthChangeDatas.Add(newDamageData);
            return;
        }

        //Suction only
        if (newHealData.forceData?.forceType == SkillForceType.KnockbackAll)
            Debug.LogError("HEALING SKILLS SHOULD NOT HAVE FORCE TYPE OF KNOCKBACK ALL. PLEASE FIX");

        if (CanApplyForces(newHealData))
        {
            SkillForce.Instance.PrepareToApplyForceToUnit(mainAttacker, myUnit, newHealData.forceData.Value, 0, false);
        }
        else if (newHealData.HPChange <= 0 && !myCharacter) //Do not bother if this is object and only restore SP or FP
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

        Debug.Log("Triggering health change Event for: " + healthUI.GetUnitDisplayName());
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
            Debug.Log("Evade Detected for: " + healthUI.GetUnitDisplayName());
        }
    }

    protected void TriggerDamageEvent()
    {
        //Update Damage Display Time
        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(currentDamageData.affinityToAttack, currentDamageData.isKOHit, currentDamageData.isKnockdownHit, currentHealthChangeDatas.Count > 0);

        Affinity currentAffinity = currentDamageData.affinityToAttack;

        //Update Health
        UpdateHealthFromAffinity(currentAffinity);

        //Update SP if character
        if(this is CharacterHealth characterHealth)
        {
            characterHealth.LoseSP(currentDamageData.SPChange);
        }

        //Update Enemy Database if enemy.
        if (!isPlayer && !IsObject())
        {
            EnemyDatabase.Instance.UpdateEnemyData(myCharacter.stats.data, currentDamageData.hitByAttackData);
        }

        //Raise Hit Event
        UnitHit?.Invoke(currentDamageData);

        SetVFXToPlay(currentDamageData.hitByAttackData);

        //Trigger Feedback
        CombatFunctions.PlayAffinityFeedback(currentAffinity, damageFeedbacks);

        switch (currentAffinity)
        {
            case Affinity.Evade: //Affinity shouldn't be evade if this function is being called.
                Debug.LogError("Affinity of evade found in trigger damage event. That's a problem!");
                return;
            case Affinity.Immune:
            case Affinity.Reflect:
            case Affinity.Absorb:
                ShowHealthChangeUI(currentAffinity, currentDamageData);
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
            currentHealth = currentHealth + currentDamageData.HPChange;
            currentHealth = Mathf.Min(currentHealth, MaxHealth());
        }
        else
        {
            //Else Deduct Damage.
            currentHealth = currentHealth - currentDamageData.HPChange;
        }

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

    public virtual void ShowHealthChangeUI(Affinity affinity, HealthChangeData healthChangeData)
    {
        healthUI.DisplayHealthChangeUI(affinity, healthChangeData, GetHealthNormalized());
    }

    protected void ClearAllData()
    {
        //Clear list
        currentHealthChangeDatas.Clear();

        //Unsubscribe from events
        ListenToHealthChangeEvent(false);
        ListenToHealthUICompleteEvent(false);

        //Reset data.
        mainAttacker = null;
        currentDamageData = null;
        currentHealData = null;
    }

    public void TryGiveAttackerFP(bool isEnhancedAction, int numOfSEApplied = 0)
    {
        foreach(GridUnit unit  in currentDamageData.GetInstigatorList())
        {
            if(unit is CharacterGridUnit attacker)
            {
                attacker.CharacterHealth().GainFP(TheCalculator.Instance.CalculateFPGain(isEnhancedAction, numOfSEApplied));
            }
        }
    }

    //GETTERS
    protected bool CanApplyForces(HealthChangeData healthChangeData)
    {
        bool immuneToForces = myUnit.stats.IsImmuneToForces();

        if (immuneToForces || !healthChangeData.forceData.HasValue)
        {
            return false;
        }

        SkillForceType forceType = healthChangeData.forceData.Value.forceType;

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
                    predictedHealth = predictedHealth + damageData.HPChange;
                }
                else
                {
                    predictedHealth = predictedHealth - damageData.HPChange;
                }
            }
            else if (healthChangeData is HealData healData)
            {
                predictedHealth = predictedHealth + healData.HPChange;
            }

            if (predictedHealth <= 0) //If it ever drops to 0 or below, immediately retun 0 cos at that point they are KOed. 
            {
                return 0;
            }
        }

        return predictedHealth;
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

    private void SetVFXToPlay(AttackData attackData)
    {
        GameObject VFXToPlay = attackData.hitVFX;

        if (!VFXToPlay) { return; }

        Vector3 position = attackData.hitVFXPos;

        VFXToPlay.transform.parent = hitVFXHeader; 
        VFXToPlay.transform.position = position;
        VFXToPlay.transform.forward = (attackData.mainInstigator.transform.position - myUnit.transform.position).normalized;
    }

    //SETTERS
    public virtual void ActivateHealthVisual(bool show)
    {
        healthUI.Fade(show);
    }

    public void ActivateNameOnlyUI(bool show)
    {
        healthUI.NameOnlyMode(show);
    }

    public void DeactivateHealthVisualImmediate()
    {
        healthUI.DeactivateImmediate();
    }

    public void SetBuffsToApplyVisual(List<ChanceOfInflictingStatusEffect> buffs)
    {
        //healthUI.SetBuffsToDisplay(buffs);
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

    protected HealthChangeData GetCurrentHealthChangeData()
    {
        return currentDamageData != null ? currentDamageData : currentHealData;
    }
}
