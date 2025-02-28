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

    bool dataShown = true;
    bool hasSetName = false;
    public bool showingSkillFeedback { get; private set; } = false;
    bool showSPChange = false;
    bool showingBuffsOnly = false;

    List<ChanceOfInflictingStatusEffect> buffsToDisplay = new List<ChanceOfInflictingStatusEffect>();
    List<bool> buffsExtensionData = new List<bool>();

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

    public void DisplayHealthChangeUI(Affinity affinity, HealthChangeData healthChangeData, float newNormalizedHealth, bool isHealing, bool shortenedDisplayTime)
    {
        SetDisplayTime(shortenedDisplayTime);

        bool showHealthChange = true;
        showingSkillFeedback = true;

        Debug.Log("Displaying Health Change UI for " + nameText.text);

        switch (affinity)
        {
            case Affinity.Absorb:
                Absorb();
                break;
            case Affinity.Resist:
                Resist();
                break;
            case Affinity.Immune:
                showHealthChange = false;
                Immune();
                break;
            case Affinity.Reflect:
                showHealthChange = false;
                Reflect();
                break;
            case Affinity.Weak:
                Weak();
                break;
            case Affinity.Evade:
                showHealthChange = false;
                Evade();
                break;
        }

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

        if (isHealing)
        {
            ShowHealing(newNormalizedHealth);
        }
        else
        {
            ShowDamage(newNormalizedHealth);
        }
    }

    private void SetDisplayTime(bool shortenedDisplayTime)
    {
        displayTime = FantasyCombatManager.Instance.GetSkillFeedbackDisplayTime();

        if (shortenedDisplayTime)
        {
            //displayTime = displayTime * 0.5f;
        }
    }

    public void SetHPChangeNumberText(int num)
    {
        dataShown = false;
        damageNumberText.text = num.ToString();
        healNumberText.text = num.ToString();
    }

    public void SetSPChangeNumberText(int num)
    {
        dataShown = false;
        showSPChange = true;
        spLossNumberText.text = num.ToString();
    }

    public void SetBuffsToDisplay(List<ChanceOfInflictingStatusEffect> buffs)
    {
        dataShown = false;

        foreach (ChanceOfInflictingStatusEffect data in buffs)
        {
            if (data.statusEffect.isStatBuffOrDebuff)
            {
                buffsToDisplay.Add(data);
                buffsExtensionData.Add(StatusEffectManager.Instance.WillMaxBuffBeExtended(myCharacter, data.statusEffect, data.buffChange));
            }
        }
    }

    //Display Data
    private void ShowHealing(float newNormalizedHealth)
    {
        if (!gameObject.activeInHierarchy)
        {
            ActivateHealthChangeMode(true);
        }

        if (showSPChange)
        {
            spLossNumberText.gameObject.SetActive(true);
        }

        DisplayBuff();

        healNumberText.gameObject.SetActive(true);
        outerHeart.color = outerHeartHealColour;
        StartCoroutine(HealingRoutine(newNormalizedHealth));

        dataShown = true;
    }

    private void ShowDamage(float newNormalizedHealth)
    {
        if (!gameObject.activeInHierarchy)
        {
            ActivateHealthChangeMode(true);
        }

        if (showSPChange)
        {
            spLossNumberText.gameObject.SetActive(true);
        }

        DisplayBuff();

        damageNumberText.gameObject.SetActive(true);
        outerHeart.color = outerHeartDefaultColor;
        StartCoroutine(DamageDealtRoutine(newNormalizedHealth));

        dataShown = true;
    }

    public void ShowBuffsOnly()
    {
        if (!gameObject.activeInHierarchy)
        {
            ActivateHealthChangeMode(false);
        }

        showingBuffsOnly = true;
        DisplayBuff();

        dataShown = true;
    }

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

    //Event Texts

    private void Weak()
    {
        ActivateHealthChangeMode(true);
        weakText.SetActive(true);
    }

    private void Immune()
    {
        ActivateHealthChangeMode(false);
        immuneText.SetActive(true);
    }

    private void Resist()
    {
        ActivateHealthChangeMode(true);
        resistText.SetActive(true);
    }

    private void Reflect()
    {
        ActivateHealthChangeMode(false);
        reflectText.SetActive(true);
    }

    private void Evade()
    {
        ActivateHealthChangeMode(false);
        evadeText.SetActive(true);
    }

    private void Absorb()
    {
        ActivateHealthChangeMode(true);
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

    private void ActivateHealthChangeMode(bool showHeart)
    {
        nameText.gameObject.SetActive(false);
        gameObject.SetActive(true);
        heartHeader.SetActive(showHeart);
        canvasGroup.alpha = 1;
    }

    private void OnDisable()
    {
        //Reset Data
        canvasGroup.alpha = 0;
        ResetData();

        DefaultMode();
    }

    private void ResetData()
    {
        if (!dataShown) { return; }

        dataShown = false;
        showSPChange = false;
        fadingIn = false;
        showingBuffsOnly = false;
        showingSkillFeedback = false;

        buffsToDisplay.Clear();
        buffsExtensionData.Clear();
    }

    public void DisplayBuff()
    {
        if (buffsToDisplay.Count > 0)
        {
            SetBuffText();
        }
        else if (showingBuffsOnly)
        {
            Fade(false);
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
    }

    private void DefaultMode()
    {
        heartHeader.SetActive(true);
        nameText.gameObject.SetActive(true);
        //numberText.gameObject.SetActive(false);
    }

    public void NameOnlyMode()
    {
        heartHeader.SetActive(false);
        nameText.gameObject.SetActive(true);
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
