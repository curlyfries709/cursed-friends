using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using AnotherRealm;

public class MasteryUIComponent : MonoBehaviour
{
    [Header("Areas")]
    [SerializeField] GameObject selectedArea;
    [Space(5)]
    [SerializeField] GameObject lockedMastery;
    [SerializeField] GameObject unlockedMastery;
    [Header("Image")]
    [SerializeField] Image icon;
    [Header("Components")]
    [SerializeField] Image progressionBar;
    [Space(5)]
    [SerializeField] GameObject checkMark;
    [SerializeField] Outline masteryOutline;
    [Header("Text")]
    [SerializeField] TextMeshProUGUI progressionTitle;
    [SerializeField] TextMeshProUGUI progressionCount;
    [Space(5)]
    [SerializeField] TextMeshProUGUI progressionReward;
    [SerializeField] TextMeshProUGUI progressionUnearnedReward;
    [Header("Colors")]
    [SerializeField] Color defaultOutlineColor;
    [SerializeField] Color selectedOutlineColor;


    bool isLocked = false;
    float selectedOutlineSize = 1.5f;
    float defaultOutlineSize = 1;

    public void SetData(MasteryProgression progression, int currentProgressionCount)
    {
        isLocked = false;

        lockedMastery.SetActive(false);
        unlockedMastery.SetActive(true);

        progressionBar.fillAmount = (float)currentProgressionCount / progression.requiredCountToComplete;

        if (progression.progressionIcon)
        {
            icon.sprite = progression.progressionIcon;
            icon.color = progression.iconColor;
        }

        bool isComplete = progression.requiredCountToComplete == currentProgressionCount;

        progressionTitle.text = progression.description;

        progressionReward.gameObject.SetActive(isComplete);
        progressionUnearnedReward.gameObject.SetActive(!isComplete);
        
        checkMark.SetActive(isComplete);
        progressionCount.text = currentProgressionCount.ToString();

        if (isComplete)
        {
            progressionReward.text = "+" + progression.rewardPoints.ToString() + " " + HandyFunctions.GetAttributeAbbreviation(progression.rewardAttribute);
        }
        else
        {
            progressionUnearnedReward.text = "+" + progression.rewardPoints.ToString() + " " + HandyFunctions.GetAttributeAbbreviation(progression.rewardAttribute);
            progressionCount.text = progressionCount.text + "/" + progression.requiredCountToComplete;
        }

        IsSelected(false);
    }

    public void IsSelected(bool isSelected)
    {
        if (isLocked) { return;}

        selectedArea.SetActive(isSelected);

        masteryOutline.effectColor = isSelected ? selectedOutlineColor : defaultOutlineColor;
        masteryOutline.effectDistance = isSelected ? new Vector2 (selectedOutlineSize, selectedOutlineSize) : new Vector2(defaultOutlineSize, defaultOutlineSize);
    }


    public void Lock()
    {
        isLocked = true;

        progressionBar.fillAmount = 0;

        lockedMastery.SetActive(true);
        unlockedMastery.SetActive(false);

        selectedArea.SetActive(false);
    }
}
