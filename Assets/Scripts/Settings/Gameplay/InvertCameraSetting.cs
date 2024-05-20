using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvertCameraSetting : GameSetting
{
    [SerializeField] GameObject checkmark;
    [SerializeField] bool invertX;

    int currentIndex = -1;
    bool isInverted = false;


    protected override void DisplayCurrentData()
    {
        if(currentIndex < 0)
        {
            isInverted = invertX ? GameManager.Instance.InvertXCam : GameManager.Instance.InvertYCam;
            checkmark.SetActive(isInverted);
        }

        currentIndex = 0;
    }

    public override bool OnSelect()
    {
        changesMade = true;
        isInverted = !isInverted;
        checkmark.SetActive(isInverted);
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

        if (invertX)
        {
            GameManager.Instance.InvertXCam = isInverted;
        }
        else
        {
            GameManager.Instance.InvertYCam = isInverted;
        }

        currentIndex = -1;
    }

    public override bool OnCycle(int indexChange) { return false; }
}
