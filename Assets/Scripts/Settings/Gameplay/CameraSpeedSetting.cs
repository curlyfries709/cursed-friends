using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnotherRealm;

public class CameraSpeedSetting : GameSetting
{
    [Header("Controls")]
    [SerializeField] bool controlX;
    [SerializeField] int defaultUIIndex = 7;
    [Space(10)]
    [SerializeField] float defaultMultiplierValue = 1;
    [SerializeField] float increment = 0.1f;
    [Header("UI")]
    [SerializeField] RectTransform icon;
    [SerializeField] RectTransform barsHeader;

    int currentIndex = -1;

    protected override void DisplayCurrentData()
    {
        if (currentIndex < 0)
        {
            float currentMultiplier = controlX ? GameManager.Instance.XCamSpeedMultiplier : GameManager.Instance.YCamSpeedMultiplier;
            currentIndex = ConvertMultiplierToIndex(currentMultiplier,increment, defaultMultiplierValue, defaultUIIndex);
        }

        OnCycle(0);
    }

    public override bool OnCycle(int indexChange)
    {
        CycleSlider(indexChange, ref currentIndex, barsHeader, icon);
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

        if (controlX)
        {
            GameManager.Instance.XCamSpeedMultiplier = ConvertIndexToMultipler(currentIndex, increment, defaultMultiplierValue, defaultUIIndex);
        }
        else
        {
            GameManager.Instance.YCamSpeedMultiplier = ConvertIndexToMultipler(currentIndex, increment, defaultMultiplierValue, defaultUIIndex);
        }
        
        currentIndex = -1;
    }

    public override bool OnSelect() { return false; }
}
