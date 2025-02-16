using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tutorial : FadeUI, ISaveable
{
    [Header("Tutorial")]
    public Transform pageHeader;
    public string newActionMapName = "";

    [HideInInspector] public bool played = false;
    bool isDataRestored = false;

    //Saving

    public object CaptureState()
    {
        return played;
    }

    public void RestoreState(object state)
    {
        isDataRestored = true;
        if (state == null) { return; }

        played = (bool)state;
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
