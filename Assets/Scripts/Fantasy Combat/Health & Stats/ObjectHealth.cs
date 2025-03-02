
using UnityEngine;
using System;
using Sirenix.Serialization;
using Sirenix.OdinInspector;

public class ObjectHealth : Health
{
    [Header("Object Health")]
    [SerializeField] bool canTakeDamage = true;
    [Tooltip("Does hitting its weakness cause it to instantly be destoryed")]
    [SerializeField] bool isWeakHitInstaKo = true;
    [Space(10)]
    [ShowIf("canTakeDamage")]
    [SerializeField] int maxHealth = 100;

    public Action<DamageData> WeaknessHit;

    //DAMAGE
    public override DamageData TakeDamage(AttackData attackData, DamageType damageType)
    {
        if (!canTakeDamage)
        {
            return null;
        }

        return base.TakeDamage(attackData, damageType);
    }

    public override void TakeBumpDamage(int damage)
    {
        if (!canTakeDamage)
        {
            return;
        }

        base.TakeBumpDamage(damage);
    }

    protected override void Hit()
    {
        if(currentDamageData.affinityToAttack == Affinity.Weak)
        {
            WeaknessHit?.Invoke(currentDamageData);
        }

        ShowHealthChangeUI(currentDamageData.affinityToAttack, currentDamageData);
    }

    protected override void KO()
    {
        isKOed = true;

        if (currentDamageData.affinityToAttack == Affinity.Weak)
        {
            WeaknessHit?.Invoke(currentDamageData);
        }

        ShowHealthChangeUI(currentDamageData.affinityToAttack, currentDamageData);
        
        //Give FP, then Deplete FP
        TryGiveAttackerFP(true);

        UnitKOed?.Invoke(myUnit);
    }

    protected override void TriggerHealEvent()
    {
        //Update display time
        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(Affinity.None, false, false, currentHealthChangeDatas.Count > 0);

        int HPRestore = currentHealData.HPChange;

        //Update HP
        currentHealth = Mathf.Min(currentHealth + HPRestore, MaxHealth());

        //Objects cannot be Revived

        //Show Health
        ShowHealthChangeUI(Affinity.None, currentHealData);

        //Clear current heal data
        currentHealData = null;
    }

    //SETTERS
    public override void ActivateHealthVisual(bool show)
    {
        if (!canTakeDamage) { return; }
        base.ActivateHealthVisual(show);
    }

    //GETTERS
    public override int MaxHealth()
    {
        return maxHealth;
    }

    public bool IsWeakHitKO()
    {
        return isWeakHitInstaKo;
    }

    //SAVING
    public override object CaptureState()
    {
        healthState.currentHealth = currentHealth;
        healthState.currentSP = 0;
        healthState.currentFP = 0;

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

        isKOed = currentHealth <= 0;
    }



}
