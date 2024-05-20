using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System;

public class XPGainUI : MonoBehaviour
{
    [Header("XP")]
    [SerializeField] TextMeshProUGUI characterName;
    [SerializeField] TextMeshProUGUI xpGained;
    [Header("Level Up")]
    [SerializeField] Image levelBar;
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] GameObject levelUp;
    [Header("Animation")]
    [SerializeField] float barAnimationTime = 0.5f;


    bool didlevelUp = false;
    int levelsGained = 0;
    float barEndValue;

    public void Setup(string player, int XP, float levelBarStartValue, float levelBarEndValue, int startLevel, bool levelUp, int levelsGained)
    {
        didlevelUp = levelUp;
        barEndValue = levelBarEndValue;
        this.levelsGained = levelsGained;

        characterName.text = player;
        xpGained.text = "+" + XP.ToString();
        levelText.text = startLevel.ToString();
        levelBar.fillAmount = levelBarStartValue;
    }


    public void PlayAnimation()
    {
        if (didlevelUp)
        {
            StartCoroutine(GainedMultipleLevelsRoutine());
        }
        else
        {
            levelBar.DOFillAmount(barEndValue, barAnimationTime).SetEase(Ease.OutQuad);
        }
    }

    IEnumerator GainedMultipleLevelsRoutine()
    {
        int startLevel = Int32.Parse(levelText.text);

        for (int i = 0; i < levelsGained; i++)
        {
            levelBar.DOFillAmount(1, barAnimationTime).SetEase(Ease.Linear);
            yield return new WaitForSeconds(barAnimationTime);
            levelBar.fillAmount = 0;
            levelUp.SetActive(true);
            levelText.text = (startLevel + i + 1).ToString();
        }

        levelBar.DOFillAmount(barEndValue, barAnimationTime).SetEase(Ease.OutQuad);
    }


}
