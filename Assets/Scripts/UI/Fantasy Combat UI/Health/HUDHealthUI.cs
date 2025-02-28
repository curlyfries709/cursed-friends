using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class HUDHealthUI : MonoBehaviour
{
    [Header("Potrait")]
    public Image potrait;
    public Image potraitMask;
    [Space(10)]
    [SerializeField] bool useTransparentBGPotrait = false;
    [Header("Health")]
    public TextMeshProUGUI HPTitle;
    public Image HPBar;
    public TextMeshProUGUI HPValue;
    [Header("Stamina")]
    public TextMeshProUGUI SPTitle;
    public Image SPBar;
    public TextMeshProUGUI SPValue;
    [Header("Status Effect")]
    public Transform statusEffectsHeader;
    [Header("Fired Up!")]
    public Image firedUpMeter;
    public GameObject firedUpFilledCircle;
    public GameObject firedUpSun;
    [Header("Selected Components")]
    [SerializeField] GameObject selectedArrow;
    [Space(5)]
    [SerializeField] Image bannerBG;
    [SerializeField] GameObject innerBanner;
    [Space(5)]
    [SerializeField] Color selectedColor;
    [SerializeField] Color defaultColor;

    //Cache
    Color HPDefaultColor;
    Color SPDefaultColor;

    PlayerGridUnit myPlayer;

    public void UpdateHealth(PlayerGridUnit unit)
    {
        int newHealth = unit.CharacterHealth().currentHealth;
        int currentHealth = Mathf.RoundToInt(HPBar.fillAmount * unit.stats.Vitality);


        if (HPValue)
            currentHealth = Int32.Parse(HPValue.text);

        Color newColor = newHealth > currentHealth ? HUDManager.Instance.healColour : HUDManager.Instance.damageColour;
        newColor = newHealth == currentHealth ? HPDefaultColor : newColor;

        if(HPTitle)
            HPTitle.color = newColor;
        HPBar.color = newColor;

        if (HPValue)
        {
            HPValue.color = newColor;
            DOTween.To(() => currentHealth, x => currentHealth = x, newHealth, HUDManager.Instance.healthBarAnimationTime).OnUpdate(() => HPValue.text = currentHealth.ToString());
        }
            
        HPBar.DOFillAmount(unit.CharacterHealth().GetHealthNormalized(), HUDManager.Instance.healthBarAnimationTime).OnComplete(() => ResetHealthToDefaultColor());
    }

    public void UpdateSP(PlayerGridUnit unit)
    {
        int newSP = unit.CharacterHealth().currentSP;
        int currentSP = Mathf.RoundToInt(SPBar.fillAmount * unit.stats.Stamina);

        Color newColor = newSP > currentSP ? SPDefaultColor : HUDManager.Instance.SPLossColor;
        newColor = newSP == currentSP ? SPDefaultColor : newColor;

        if (SPTitle)
            SPTitle.color = newColor;

        SPBar.color = newColor;


        if (SPValue)
        {
            SPValue.color = newColor;
            currentSP = Int32.Parse(SPValue.text);
            DOTween.To(() => currentSP, x => currentSP = x, newSP, HUDManager.Instance.healthBarAnimationTime).OnUpdate(() => SPValue.text = currentSP.ToString());
        }

        SPBar.DOFillAmount(unit.CharacterHealth().GetStaminaNormalized(), HUDManager.Instance.healthBarAnimationTime).OnComplete(() => ResetSPToDefaultColor());
    }

    public void UpdateFP(PlayerGridUnit unit)
    {
        float currentFP = unit.CharacterHealth().GetFPNormalized();

        if (currentFP > 1 && firedUpFilledCircle)
        {
            firedUpFilledCircle.SetActive(false);
        }

        firedUpMeter.DOFillAmount(unit.CharacterHealth().GetFPNormalized(), HUDManager.Instance.healthBarAnimationTime).OnComplete(() => firedUpFilledCircle?.SetActive(currentFP >= 1));
    }

    public void ShowFiredUpSun(bool show)
    {
        if (!firedUpSun) { return; }

        firedUpSun.SetActive(show);
    }

    public Transform GetStatusEffectHeader()
    {
        return statusEffectsHeader.GetComponent<ScrollRect>().content;
    }

    private void ResetHealthToDefaultColor()
    {
        if(HPTitle)
            HPTitle.color = HPDefaultColor;
        HPBar.color = HPDefaultColor;

        if(HPValue)
            HPValue.color = HPDefaultColor;
    }

    private void ResetSPToDefaultColor()
    {
        if (SPTitle)
            SPTitle.color = SPDefaultColor;
        SPBar.color = SPDefaultColor;

        if (SPValue)
            SPValue.color = SPDefaultColor;
    }

    //INVENTORY UI
    public void IsSelected(bool selected)
    {
        innerBanner.SetActive(selected);
        selectedArrow.SetActive(selected);
        bannerBG.color = selected ? selectedColor : defaultColor;
    }

    public void SetData(PlayerGridUnit unit)
    {
        myPlayer = unit;

        HPDefaultColor = HUDManager.Instance.HPColor;
        SPDefaultColor = HUDManager.Instance.SPColor;

        //Set Potrait
        potrait.sprite = useTransparentBGPotrait ? unit.transparentBackgroundPotrait : unit.portrait;

        if (potraitMask)
            potraitMask.sprite = unit.transparentBackgroundPotrait;

        //Set Default Colors
        if(HPTitle)
            HPTitle.color = HPDefaultColor;

        if(SPTitle)
            SPTitle.color = SPDefaultColor;

        HPBar.color = HPDefaultColor;
        SPBar.color = SPDefaultColor;

        if(HPValue)
            HPValue.color = HPDefaultColor;
        if(SPValue)
            SPValue.color = SPDefaultColor;

        //Set Health
        HPBar.fillAmount = unit.CharacterHealth().GetHealthNormalized();

        if(HPValue)
            HPValue.text = unit.CharacterHealth().currentHealth.ToString();

        //Set SP
        SPBar.fillAmount = unit.CharacterHealth().GetStaminaNormalized();

        if(SPValue)
            SPValue.text = unit.CharacterHealth().currentSP.ToString();

        //Clear Status Effect Header
        foreach (Transform child in statusEffectsHeader.GetComponent<ScrollRect>().content)
        {
            Destroy(child.gameObject);
        }

        //Set Fired Up
        if (firedUpFilledCircle)
        {
            firedUpSun.SetActive(false);
            firedUpFilledCircle.SetActive(false);
        }
            
        firedUpMeter.fillAmount = unit.CharacterHealth().GetFPNormalized();
    }

    //Getters
    public PlayerGridUnit GetUnit()
    {
        return myPlayer;
    }


}
