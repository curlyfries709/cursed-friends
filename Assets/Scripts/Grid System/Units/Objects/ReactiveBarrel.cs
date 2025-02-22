using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReactiveBarrel : GridUnit //, IDamageable
{
    /*[Header("Values")]
    [SerializeField] int maxHealth = 10;
    [Header("UI")]
    [SerializeField] GameObject healthCanvas;
    [Header("Barrel Data")]
    [SerializeField] Element reactiveElement;
    [Header("DAMAGE FEEDBACKS")]
    [SerializeField] AffinityFeedback damageFeedbacks;

    public int currentHealth { get; set; }
    public int currentSP { get; set; }
    public int currentFP { get; set; }

    //Caches
    UnitHealthUI healthUI;
    CharacterGridUnit attacker;

    bool isCritical = false;
    bool reactionTriggered = false;


    protected override void Awake()
    {
        base.Awake();
        healthUI = healthCanvas.GetComponent<UnitHealthUI>();

        Setup();
    }

    private void Setup()
    {
        currentHealth = maxHealth;
        healthUI.Setup(this, GetHealthNormalized());
    }

    public void ActivateHealthVisual(bool show)
    {
        healthUI.Fade(show);
    }

    public DamageData TakeDamage(AttackData attackData)
    {       
        IDamageable.unitAttackComplete += DisplayDamageData;

        reactionTriggered = attackData.attackElement == reactiveElement && reactiveElement != Element.None;

        if (reactionTriggered)
        {
            PrepareExplosion();
        }

        isCritical = attackData.isCritical;
        attacker = attackData.attacker;

        currentHealth = currentHealth - attackData.rawDamage;


        //Prepare Data to be shown.
        healthUI.SetHPChangeNumberText(attackData.rawDamage);

        currentHealth = Mathf.Max(currentHealth, 0);

        //Knockback
        if (attackData.forceData.forceType!= SkillForceType.None)
        {
            SkillForce.Instance.PrepareToApplyForceToUnit(attacker, this, attackData.forceData, attackData.rawDamage);
        }

        IDamageable.unitHit(GetDamageData(attackData.rawDamage, false));

        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(Affinity.None, currentHealth == 0, false);

        return new DamageData(this, attacker, attackData);
    }

    public void TakeBumpDamage(int damage)
    {
        //Subscribe to event
        IDamageable.unitAttackComplete += DisplayDamageData;

        currentHealth = currentHealth - damage;

        //Prepare Data to be shown.
        healthUI.SetHPChangeNumberText(damage);

        currentHealth = Mathf.Max(currentHealth, 0);

        IDamageable.unitHit(GetDamageData(damage, true));

        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(Affinity.None, currentHealth == 0, false);
        //KO Logic must occur once damage data has been displayed.
    }

    private void PrepareExplosion()
    {
        //Add Explosion To Turn End Event. High Priority.
        //Also Cancel Below Turn End Events On Units hit.
        //No Chain Attack; No CounterAttack.
    }

    private void Explode()
    {

    }

    public void DeactivateHealthVisualImmediate()
    {
        healthUI.gameObject.SetActive(false);
    }

    private void DisplayDamageData(bool beginHealthCountdown)
    {
        //UnSubscribe to event
        IDamageable.unitAttackComplete -= DisplayDamageData;

        //ShowCritical();
        SetHealthBar(GetHealthNormalized());

        if (currentHealth <= 0)
        {
            Invoke("Destroyed", FantasyCombatManager.Instance.GetSkillFeedbackDisplayTime());
        }

        damageable.BeginHealthUICountdown(beginHealthCountdown);
    }

    private DamageData GetDamageData(int damageTaken, bool isKnockbackDamage)
    {
        DamageData damageData = new DamageData(this, attacker, Affinity.None, damageTaken);

        damageData.isBackstab = false;
        damageData.isCritical = isCritical;
        damageData.isKOHit = currentHealth == 0;
        damageData.isTargetGuarding = false;
        //damageData.isKnockbackDamage = isKnockbackDamage;

        return damageData;
    }


    private void Destroyed()
    {
        LevelGrid.Instance.RemoveUnitFromGrid(this);
        gameObject.SetActive(false);
    }

    public void ResetStateToBattleStart(int healthAtStart, int spAtStart, int fpAtStart)
    {
        gameObject.SetActive(true);
        currentHealth = healthAtStart;
    }


    public float GetHealthNormalized()
    {
        return (float)currentHealth / maxHealth;
    }

    private void SetHealthBar(float value)
    {
        healthUI.ShowDamage(value);
    }

    public AffinityFeedback GetDamageFeedbacks(Transform transformToPlayVFX, GameObject VFXToPlay)
    {
        damageable.SetVFXToPlay(this, damageFeedbacks, transformToPlayVFX, VFXToPlay);
        return damageFeedbacks;
    }*/
}
