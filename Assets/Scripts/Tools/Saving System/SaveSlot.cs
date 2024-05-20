using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class SaveSlot : MonoBehaviour
{
    [Header("Areas")]
    [SerializeField] GameObject emptyArea;
    [SerializeField] GameObject contentArea;
    [Header("Components")]
    [SerializeField] Image bgColor;
    [SerializeField] GameObject selectedIcon;
    [Header("Date")]
    [SerializeField] TextMeshProUGUI weekday;
    [SerializeField] TextMeshProUGUI date;
    [SerializeField] TextMeshProUGUI period;
    [Header("Environment")]
    [SerializeField] TextMeshProUGUI sceneName;
    [Header("Other")]
    [SerializeField] TextMeshProUGUI leaderLevel;
    [SerializeField] TextMeshProUGUI difficulty;
    [SerializeField] TextMeshProUGUI playtime;



    public void SetIsSelected(bool isSelected, Color bgColor)
    {
        selectedIcon.SetActive(isSelected);
        this.bgColor.color = bgColor;
    }

    public void SetData(SavingLoadingManager.SaveSlotState data)
    {
        contentArea.SetActive(data.hasData);
        emptyArea.SetActive(!data.hasData);

        if (!data.hasData) { return; }

        weekday.text = data.weekday;
        date.text = data.date;
        period.text = data.period;
        sceneName.text = data.activeSceneName;

        leaderLevel.text = "Keenan Lv " + data.leaderLevel.ToString();
        difficulty.text = data.difficulty;

        TimeSpan t = TimeSpan.FromSeconds(data.playTime);
        string formattedPlayetime = string.Format("{0:D2}:{1:D2}", t.Hours, t.Minutes);

        playtime.text = formattedPlayetime;
    }
}
