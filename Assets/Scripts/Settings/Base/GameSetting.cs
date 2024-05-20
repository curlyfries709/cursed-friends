using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnotherRealm;
using UnityEngine.UI;

public abstract class GameSetting : MonoBehaviour
{
    protected bool changesMade = false;

    private void Start()
    {
        SettingsUI.SaveSettings += OnSaveChanges;
    }

    private void OnEnable()
    {
        DisplayCurrentData();
    }

    public abstract bool OnCycle(int indexChange);
    public abstract bool OnSelect();

    protected abstract void DisplayCurrentData();

    protected abstract void OnSaveChanges();

    protected void CycleSlider(int indexChange, ref int currentIndex, RectTransform barsHeader, RectTransform sliderIcon)
    {
        if (indexChange != 0)
            changesMade = true;

        LayoutRebuilder.ForceRebuildLayoutImmediate(barsHeader);

        CombatFunctions.UpdateListIndex(indexChange, currentIndex, out currentIndex, barsHeader.childCount);
        RectTransform currentBar = barsHeader.GetChild(currentIndex) as RectTransform;

        sliderIcon.anchoredPosition = new Vector2(currentBar.anchoredPosition.x, sliderIcon.anchoredPosition.y);
    }

    protected float ConvertIndexToMultipler(int index, float increment, float defaultMultiplierValue, int defaultUIIndex)
    {
        float difference = (index - defaultUIIndex) * increment;
        return defaultMultiplierValue + difference;
    }

    protected int ConvertMultiplierToIndex(float currentMultiplier,float increment, float defaultMultiplierValue, int defaultUIIndex)
    {
        float firstMultiplierValue = ConvertIndexToMultipler(0, increment, defaultMultiplierValue, defaultUIIndex);
        return Mathf.RoundToInt((currentMultiplier - firstMultiplierValue) / increment);
    }

    private void OnDestroy()
    {
        SettingsUI.SaveSettings -= OnSaveChanges;
    }

}
