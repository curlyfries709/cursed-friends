using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tutorial : FadeUI, ISaveable
{
    [Header("Tutorial")]
    public Transform pageHeader;
    public string newActionMapName = "";

    [HideInInspector] public bool played = false;
    public bool AutoRestoreOnNewTerritoryEntry { get; set; } = false;

    //Saving

    public object CaptureState()
    {
        return played;
    }

    public void RestoreState(object state)
    {
        if (state == null) { return; }

        played = (bool)state;
    }
}
