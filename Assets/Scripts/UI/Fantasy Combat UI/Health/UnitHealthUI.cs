using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

public class UnitHealthUI : FadeUI
{
    [Header("Main Text")]
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI damageNumberText;
    [SerializeField] TextMeshProUGUI healNumberText;
    [Space(10)]
    [SerializeField] TextMeshProUGUI spLossNumberText;
    [Space(10)]
    [SerializeField] HealthUIBuffText buffText;
    [Header("Health")]
    [SerializeField] GameObject heartHeader;
    [SerializeField] Image outerHeart;
    [SerializeField] Image innerHeart;
    [Header("Colors")]
    [SerializeField] Color outerHeartHealColour;
    [Header("Timers")]
    [SerializeField] float heartAnimationTime;
    [SerializeField] float damageDealtDisplayTime;
    [Space(10)]
    [SerializeField] float statusEffectOnlyDisplayTime;
    [Header("Status Effects")]
    [SerializeField] Transform statusEffectHeader;
    [Header("Affinity Event Text")]
    [SerializeField] GameObject absorbText;
    [SerializeField] GameObject immuneText;
    [SerializeField] GameObject weakText;
    [SerializeField] GameObject reflectText;
    [SerializeField] GameObject resistText;
    [Header("Other Event Text")]
    [SerializeField] GameObject evadeText;
    [SerializeField] GameObject guardText;
    [Space(10)]
    [SerializeField] GameObject critText;
    [SerializeField] GameObject backStabText;
    [Space(10)]
    [SerializeField] GameObject knockdownText;
    [SerializeField] GameObject bumpText;

    //Variables
    public float displayTime { get; private set; } = 1;

    bool hasSetName = false;
    public bool showingSkillFeedback { get; private set; } = false;

    //STATUS EFFECT DATA
    bool showingBuffsOnly = false;

    List<ChanceOfInflictingStatusEffect> buffsToDisplay = new List<ChanceOfInflictingStatusEffect>();
    List<bool> buffsExtensionData = new List<bool>();

    //CACHE
    private Color outerHeartDefaultColor;

    CharacterGridUnit myCharacter;

    //Events
    public Action<bool> HealthUIComplete; 

    protected override void Awake()
    {
        base.Awake();
        outerHeartDefaultColor = outerHeart.color;
    }

    private void OnEnable()
    {
        if (myCharacter && !hasSetName)
        {
            nameText.text = EnemyDatabase.Instance.GetEnemyDisplayName(myCharacter, myCharacter.stats.data);
            hasSetName = true;
        }
    }
    //Setup

    public void Setup(GridUnit unit, float healthNormalized)
    {
        myCharacter = unit as CharacterGridUnit;

        if (!myCharacter)
        {
            nameText.text = unit.unitName;
            hasSetName = true;
        }

        outerHeart.fillAmount = healthNormalized;
        innerHeart.fillAmount = healthNormalized;

        damageNumberText.gameObject.SetActive(false);

        ClearStatusEffects();
    }

    //Evade passes null healthChangeData so affinity argument is necessay
    public void DisplayHealthChangeUI(Affinity affinity, HealthChangeData healthChangeData, float newNormalizedHealth)
    {
        Debug.Log("Displaying Health Change UI for " + nameText.text);
        showingSkillFeedback = true;

        //Set Display time
        displayTime = FantasyCombatManager.Instance.GetSkillFeedbackDisplayTime();

        //Bools
        bool showStatusEffectOnly = affinity == Affinity.None && !healthChangeData.IsVitalsChanged();
        bool showHealthChange = !showStatusEffectOnly && 
            !(affinity == Affinity.Immune || affinity == Affinity.Reflect || affinity == Affinity.Evade);

        ActivateHealthChangeMode(showHealthChange); //Must be called before affinity functions.

        if (showStatusEffectOnly)
        {
            ShowBuffsOnly();
            StartCoroutine(StatusEffectOnlyRoutine(displayTime));
            return;
        }

        switch (affinity)
        {
            case Affinity.Absorb:
                Absorb();
                break;
            case Affinity.Resist:
                Resist();
                break;
            case Affinity.Immune:
                Immune();
                break;
            case Affinity.Reflect:
                Reflect();
                break;
            case Affinity.Weak:
                Weak();
                break;
            case Affinity.Evade:
                Evade();
                break;
        }

        if (affinity == Affinity.Evade) { return; } //Exit early if evade. Beyond this point, health change data should be valid.

        DamageData damageData = healthChangeData as DamageData;

        if (damageData != null && damageData.isBackstab)
        {
            BackStab();
        }

        if (healthChangeData != null && healthChangeData.isCritical)
        {
            CritHit();
        }

        if(damageData != null && damageData.isTargetGuarding && damageData.damageType != DamageType.StatusEffect)
        {
            Guard();
        }

        if(damageData != null && damageData.damageType == DamageType.KnockbackBump)
        {
            Bump();
        }

        if (!showHealthChange) { return; }

        bool isHealing = healthChangeData == null ? false :
            !(healthChangeData is DamageData) || (healthChangeData as DamageData).affinityToAttack == Affinity.Absorb;

        if (isHealing)
        {
            ShowHealing(newNormalizedHealth, healthChangeData);
        }
        else
        {
            ShowDamage(newNormalizedHealth, healthChangeData);
        }
    }

    //Display Data
    private void ShowHealing(float newNormalizedHealth, HealthChangeData healthChangeData)
    {
        int healthChange = healthChangeData.HPChange;
        int staminaChange = healthChangeData.SPChange;

        if(healthChange > 0)
        {
            SetHPChangeNumberText(healthChange);
            healNumberText.gameObject.SetActive(true);
        }

        if (staminaChange > 0)
        {
            SetSPChangeNumberText(staminaChange);
            spLossNumberText.gameObject.SetActive(true);
        }

        ShowBuffsOnly();

        outerHeart.color = outerHeartHealColour;
        StartCoroutine(HealingRoutine(newNormalizedHealth));
    }

    private void ShowDamage(float newNormalizedHealth, HealthChangeData healthChangeData)
    {
        int healthChange = healthChangeData.HPChange;
        int staminaChange = healthChangeData.SPChange;

        if(healthChange > 0)
        {
            SetHPChangeNumberText(healthChange);
            damageNumberText.gameObject.SetActive(true);
        }

        if (staminaChange > 0)
        {
            SetSPChangeNumberText(staminaChange);
            spLossNumberText.gameObject.SetActive(true);
        }

        ShowBuffsOnly();

        outerHeart.color = outerHeartDefaultColor;
        StartCoroutine(DamageDealtRoutine(newNormalizedHealth));
    }

    public void ShowBuffsOnly()
    {
        //NEEDS TO BE UPDATED.

        /*if (buffsToDisplay.Count > 0)
        {
            SetBuffText();
        }
        else if (showingBuffsOnly)
        {
            Fade(false);
        }*/
    }

    //Visual setups
    private void ActivateHealthChangeMode(bool showHeart)
    {
        nameText.gameObject.SetActive(false);
        gameObject.SetActive(true);
        heartHeader.SetActive(showHeart);
        canvasGroup.alpha = 1;
    }

    private void DefaultMode()
    {
        heartHeader.SetActive(true);
        nameText.gameObject.SetActive(true);
        //numberText.gameObject.SetActive(false);
    }

    public void NameOnlyMode(bool activate)
    {
        heartHeader.SetActive(false);
        nameText.gameObject.SetActive(true);
        Fade(activate);
    }

    //Data setters
    private void SetHPChangeNumberText(int num)
    {
        damageNumberText.text = num.ToString();
        healNumberText.text = num.ToString();
    }

    private void SetSPChangeNumberText(int num)
    {
        spLossNumberText.text = num.ToString();
    }

    /*private void SetBuffsToDisplay(List<ChanceOfInflictingStatusEffect> buffs)
    {
        foreach (ChanceOfInflictingStatusEffect data in buffs)
        {
            if (data.statusEffect.isStatBuffOrDebuff)
            {
                buffsToDisplay.Add(data);
                buffsExtensionData.Add(StatusEffectManager.Instance.WillMaxBuffBeExtended(myCharacter, data.statusEffect, data.buffChange));
            }
        }
    }

    private void SetBuffText()
    {
        ChanceOfInflictingStatusEffect data = buffsToDisplay[0];
        string textToAppend = "Up";

        if (data.buffChange < 0)
        {
            textToAppend = "Down";
        }

        string newText = data.statusEffect.buffNickname + " " + textToAppend;

        if (buffsExtensionData[0])
        {
            newText = newText + " " + "extended";
        }

        buffText.Setup(newText, StatusEffectManager.Instance.GetBuffColor(data.buffChange));

        buffsToDisplay.RemoveAt(0);
        buffsExtensionData.RemoveAt(0);
    }*/

    //Data Routines

    IEnumerator HealingRoutine(float newNormalizedHealth)
    {
        outerHeart.DOFillAmount(newNormalizedHealth, heartAnimationTime);
        yield return new WaitForSeconds(damageDealtDisplayTime);
        innerHeart.DOFillAmount(newNormalizedHealth, heartAnimationTime);
    }

    IEnumerator DamageDealtRoutine(float newNormalizedHealth)
    {
        innerHeart.DOFillAmount(newNormalizedHealth, heartAnimationTime);
        yield return new WaitForSeconds(damageDealtDisplayTime);
        outerHeart.DOFillAmount(newNormalizedHealth, heartAnimationTime);
    }

    IEnumerator StatusEffectOnlyRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        Fade(false);
    }

    //Event Texts

    private void Weak()
    {
        weakText.SetActive(true);
    }

    private void Immune()
    {
        immuneText.SetActive(true);
    }

    private void Resist()
    {
        resistText.SetActive(true);
    }

    private void Reflect()
    {
        reflectText.SetActive(true);
    }

    private void Evade()
    {
        evadeText.SetActive(true);
    }

    private void Absorb()
    {
        absorbText.SetActive(true);
    }

    private void CritHit()
    {
        critText.SetActive(true);
    }

    private void BackStab()
    {
        backStabText.SetActive(true);
    }

    private void Guard()
    {
        guardText.SetActive(true);
    }

    private void Bump()
    {
        bumpText.SetActive(true);
    }

    public void KnockDown()
    {
        if (weakText.activeInHierarchy) { return; }

        //ActivateHealthChangeMode(true);
        //knockdownText.SetActive(true);
    }

    private void OnDisable()
    {
        ResetData();  
    }

    private void ResetData()
    {
        Debug.Log("Resetting health ui for: " + GetUnitDisplayName());
        canvasGroup.alpha = 0;

        fadingIn = false;
        showingBuffsOnly = false;
        showingSkillFeedback = false;

        buffsToDisplay.Clear();
        buffsExtensionData.Clear();

        DefaultMode();
    }

    public override void FadeOutComplete()
    {
        if(!gameObject.activeInHierarchy || fadingIn) { return; }

        bool raiseEvents = showingSkillFeedback; //Make copy of variable before it is reset. 

        currentTween = null;
        fadeCompleteCallback?.Invoke();

        gameObject.SetActive(false); //Showing Skill Feedback gets reset here.
        HideAllEventText();

        if (raiseEvents)
        {
            if (HealthUIComplete == null)
            {
                Debug.Log("Health UI Complete for: " + nameText.text);
                FantasyCombatManager.Instance.currentCombatAction?.DisplayUnitHealthUIComplete();
            }
            else
            {
                Debug.Log("Health UI Event Raised for: " + nameText.text);
                HealthUIComplete(true);
            }
        }
    }

    public void DeactivateImmediate()
    {
        gameObject.SetActive(false);
        HideAllEventText();
        currentTween?.Kill();
        currentTween = null;
    }

    private void HideAllEventText()
    {
        reflectText.SetActive(false);
        immuneText.SetActive(false);
        absorbText.SetActive(false);
        weakText.SetActive(false);
        reflectText.SetActive(false);

        evadeText.SetActive(false);
        critText.SetActive(false);
        backStabText.SetActive(false);
        bumpText.SetActive(false);
        guardText.SetActive(false);
    }


    public string GetUnitDisplayName()
    {
        return nameText.text;
    }

    public Transform StatusEffectHeader()
    {
        return statusEffectHeader;
    }

    private void ClearStatusEffects()
    {
        foreach(Transform child in statusEffectHeader)
        {
            Destroy(child.gameObject);
        }
    }
}
