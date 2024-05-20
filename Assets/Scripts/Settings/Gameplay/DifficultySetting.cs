using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using AnotherRealm;
using System;

public class DifficultySetting : GameSetting
{
    [SerializeField] TextMeshProUGUI currentDifficulty;
    [SerializeField] TextMeshProUGUI difficultyDescription;

    int currentIndex = -1;

    protected override void DisplayCurrentData()
    {
        if(currentIndex < 0)
        {
            GameManager.Difficulty currentDifficulty = GameManager.Instance.GetGameDifficulty();
            currentIndex = (int)currentDifficulty;
        }

        OnCycle(0);
    }

    public override bool OnCycle(int indexChange)
    {
        if (indexChange != 0)
            changesMade = true;

        CombatFunctions.UpdateListIndex(indexChange, currentIndex, out currentIndex, Enum.GetNames(typeof(GameManager.Difficulty)).Length);

        currentDifficulty.text = ((GameManager.Difficulty)currentIndex).ToString();
        difficultyDescription.text = GameManager.Instance.GetDifficultyDescription((GameManager.Difficulty)currentIndex);

        return true;
    }

    protected override void OnSaveChanges()
    {
        if (!changesMade) 
        {
            currentIndex = -1;
            return; 
        }

        changesMade = false;

        GameManager.Instance.UpdateDifficulty((GameManager.Difficulty)currentIndex);
        currentIndex = -1;
    }

    public override bool OnSelect() { return false;  }
}
