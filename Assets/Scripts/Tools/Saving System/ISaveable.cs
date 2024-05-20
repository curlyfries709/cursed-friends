using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISaveable
{
    public bool AutoRestoreOnNewTerritoryEntry { get; set; } //When New Territory entered or new area at home loaded.

    object CaptureState();
    void RestoreState(object state);

    public void AutoRestoreStateOnAwake(SaveableEntity saveableEntity)
    {
        if (!SavingLoadingManager.Instance.AllowSelfDataLoad) { return; }
        SavingLoadingManager.Instance.LoadSaveableDataOnAwake(saveableEntity);
    }
}
