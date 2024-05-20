using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;

public class VolumeSetting : GameSetting
{
    [Header("Controls")]
    [SerializeField] MMSoundManager.MMSoundManagerTracks audioTrack;
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
            float currentMultiplier = AudioManager.Instance.GetVolume(audioTrack);
            currentIndex = ConvertMultiplierToIndex(currentMultiplier, increment, defaultMultiplierValue, defaultUIIndex);
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
        AudioManager.Instance.SetVolume(audioTrack, ConvertIndexToMultipler(currentIndex, increment, defaultMultiplierValue, defaultUIIndex));
        currentIndex = -1;
    }

    public override bool OnSelect() { return false;  }
}
