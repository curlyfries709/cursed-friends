
public interface ISaveable
{
    object CaptureState();
    void RestoreState(object state);

    bool IsDataRestored();

    public void AutoRestoreStateOnAwake(SaveableEntity saveableEntity)
    {
        if (!SavingLoadingManager.Instance.AllowSelfDataLoad) { return; }
        SavingLoadingManager.Instance.LoadSaveableDataOnAwake(saveableEntity);
    }

    
}
