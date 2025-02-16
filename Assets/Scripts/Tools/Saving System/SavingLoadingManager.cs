using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Sirenix.Serialization;
using AnotherRealm;
using TMPro;
using UnityEngine.SceneManagement;

public class SavingLoadingManager : MonoBehaviour, IControls
{
    public static SavingLoadingManager Instance { get; private set; }
    [Header("New Game")]
    [SerializeField] int newGameStartSceneIndex = 3;
    [Header("Components")]
    [SerializeField] FadeUI loadingScreen;
    [Space(10)]
    [SerializeField] GameObject saveUI;
    [SerializeField] Transform slotHeader;
    [Header("Header")]
    [SerializeField] GameObject saveTitleArea;
    [SerializeField] GameObject loadTitleArea;
    [Header("Colors")]
    [SerializeField] Color selectedColor;
    [SerializeField] Color defaultColor;
    [Header("Modal")]
    [SerializeField] GameObject confirmModal;
    [SerializeField] TextMeshProUGUI confirmText;
    [Space(10)]
    [TextArea(2,4)]
    [SerializeField] string confirmSaveMessage;
    [TextArea(2, 4)]
    [SerializeField] string confirmLoadMessage;
    [Header("Controls")]
    [SerializeField] List<Transform> controlHeaders = new List<Transform>();

    [SerializeField, HideInInspector]
    private SaveManagerState saveState = new SaveManagerState();

    bool shouldSave = false;
    int currentSlotIndex = 0;
    float currentLoadedPlaytime = 0;

    const string saveFilePrepend = "Save";
    const string persistentDataFile = "PersistentSave";
    const string savingManagerKey = "SavingManager";

    const string myActionMap = "Menu";
    const string playerActionMap = "Player";

    bool enablePlayerControlsOnLoadComplete = true;
    public bool AllowSelfDataLoad { get; private set; } = false;

    //Test Bool 
    public bool LoadingEnabled { get; private set; } = false;

    //Cache
    SavingSystem savingSystem;
    RealmType prevRealmType = RealmType.NotSet;

    //Events
    public Action BeginNewGameCinematic;
    public Action EnteringNewTerritory;
    public Action ReturnToDefaultPosition;

    public Action<SceneData> LoadGameDataComplete;
    public Action<SceneData> NewSceneLoadComplete;
    public Action<RealmType> NewRealmEntered; 

    private void Awake()
    {
        Instance = this;
        savingSystem = GetComponent<SavingSystem>();
        ControlsManager.Instance.SubscribeToPlayerInput(myActionMap, this);

        foreach (Transform controlHeader in controlHeaders)
        {
            ControlsManager.Instance.AddControlHeader(controlHeader);
        }

        RestoreState(LoadPersistentData(savingManagerKey));
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (!LoadingEnabled)
        {
            savingSystem.NewTerritoryRestore(true);
            InvokeSceneLoadedEvents(true);
            EnablePlayerControls();
        }
#endif
    }

    private void InvokeSceneLoadedEvents(bool loadedSaveFile)
    {
        SceneData currentSceneData = FindObjectOfType<SceneData>();

        if (!currentSceneData)
        {
            Debug.Log("COULDN'T FIND SCENE DATA!");
        }

        GameSystemsManager.Instance.SetupNewSceneLoadedData(currentSceneData);//Players Spawned. Party Data Set. Level Grid Setup
        RealmType newRealm = currentSceneData.GetRealmType();

        if (loadedSaveFile)
        {
            //Called when loading of an old save file is complete.
            LoadGameDataComplete?.Invoke(currentSceneData);
        }
        else if (prevRealmType != newRealm)
        {
            //Called when going from Modern to Fantasy realm and vice versa. 
            NewRealmEntered?.Invoke(newRealm);
        }
        
        //Called when a new scene/territory is loaded.
        NewSceneLoadComplete?.Invoke(currentSceneData);
    }

    //Saving Functionality
    private void RestoreNewScene()
    {

    }

    public void StoreCurrentSceneData()
    {
        savingSystem.StoreCurrentSceneData();
    }

    public void SaveGame(int saveSlotIndex, Action saveComplete)
    {
        UpdateSaveData(saveSlotIndex);

        string selectedSaveFile = saveFilePrepend + saveSlotIndex;
        savingSystem.Save(selectedSaveFile);

        saveComplete?.Invoke();
    }

    public void DeleteSaveFile(int saveSlotIndex)
    {
        //Called When Starting New Game or from Win Screen.
        string selectedSaveFile = saveFilePrepend + saveSlotIndex;
        savingSystem.DeleteFile(selectedSaveFile);
    }

    //Loading Functionalty
    public void LoadGame(int saveSlotIndex)
    {
        SaveSlotState slotState = saveState.saveSlotStates[saveSlotIndex];
        currentLoadedPlaytime = slotState.playTime;

        StartCoroutine(BeginLoadRoutine(saveSlotIndex));
    }

    public void LoadSaveableDataOnAwake(SaveableEntity saveableEntity)
    {
        savingSystem.RestoreIndividualState(saveableEntity);
    }

    //Persistent Data. E.G Game Settings & Save Slots.
    public void SavePersistentData(string key, object data)
    {
        savingSystem.SavePersistentData(persistentDataFile, key, data);
    }
    public object LoadPersistentData(string key)
    {
        Dictionary<string, object> loadedData = savingSystem.LoadPersistentData(persistentDataFile);

        if (loadedData.ContainsKey(key))
        {
            return loadedData[key];
        }

        return null;
    }

    //New Game
    public void LoadNewGame()
    {
        currentLoadedPlaytime = 0;
        StartCoroutine(LoadNewGameRoutine());
    }

    //Scene Loading
    public void ReturnToTitleScreen()
    {
        StartCoroutine(LoadTitleScreen());
    }

    public void EnterNewTerritory(string sceneName)
    {
        StartCoroutine(EnterNewTerritoryRoutine(sceneName));
    }

    IEnumerator BeginLoadRoutine(int saveSlotIndex)
    {
        ControlsManager.Instance.DisableControls();
        AudioManager.Instance.StopMusic();
        LoadingEnabled = true;
        AllowSelfDataLoad = false;
        loadingScreen.Fade(true);
        yield return new WaitForSecondsRealtime(loadingScreen.fadeInTime);
        confirmModal.SetActive(false);
        saveUI.SetActive(false);
        string selectedSaveFile = saveFilePrepend + saveSlotIndex;
        int lastSceneIndex = savingSystem.GetLastSavedSceneIndex(selectedSaveFile);

        if (lastSceneIndex >= 0 && lastSceneIndex != SceneManager.GetActiveScene().buildIndex)
        {
            //Load Scene & Wait
            yield return LoadScene(lastSceneIndex, LoadSceneMode.Single);
            Debug.Log("Scene Loaded");
        }
        else if (lastSceneIndex == SceneManager.GetActiveScene().buildIndex)
        {
            ReturnToDefaultPosition?.Invoke();
        }

        savingSystem.LoadFromFile(selectedSaveFile);
        InvokeSceneLoadedEvents(true);

        yield return new WaitForSecondsRealtime(0.5f); //Short Delay to Hide Camera Transition when Warping Player.
        loadingScreen.Fade(false);

        GameManager.Instance.ResumeGame();
        AudioManager.Instance.PlayMusic(MusicType.Roam);

        AllowSelfDataLoad = true;
    }

    IEnumerator EnterNewTerritoryRoutine(string sceneName)
    {
        //Pause Game Or Whatever

        //Disable
        ControlsManager.Instance.DisableControls();
        AudioManager.Instance.StopMusic();
        //Set Bools
        LoadingEnabled = true;
        enablePlayerControlsOnLoadComplete = true;

        //Loading Screen
        loadingScreen.Fade(true);
        yield return new WaitForSecondsRealtime(loadingScreen.fadeInTime);

        //Set Current Realm Type
        prevRealmType = GameSystemsManager.Instance.GetCurrentSceneData().GetRealmType();

        //Allow A chance for territory only objects to be discarded or restored before saving current scene data.
        EnteringNewTerritory?.Invoke();

        //Store Current Data
        StoreCurrentSceneData();

        //Begin Loading Next Scene
        yield return LoadScene(sceneName, LoadSceneMode.Single);

        //Restore New Scene Data.
        savingSystem.NewTerritoryRestore(false);

        InvokeSceneLoadedEvents(false);
        yield return new WaitForSecondsRealtime(0.5f); //Short Delay to Hide Camera Transition when Warping Player.

        loadingScreen.Fade(false);
        //GameManager.Instance.ResumeGame();
        AudioManager.Instance.PlayMusic(MusicType.Roam);

        //Re-Enable Controls
        EnablePlayerControls();
    }

    IEnumerator LoadNewGameRoutine()
    {
        ControlsManager.Instance.DisableControls();
        AudioManager.Instance.StopMusic();
        LoadingEnabled = true;
        AllowSelfDataLoad = false;
        loadingScreen.Fade(true);
        yield return new WaitForSecondsRealtime(loadingScreen.fadeInTime);
        //Deactivate any UI
        yield return LoadScene(newGameStartSceneIndex, LoadSceneMode.Single);

        //Send Everyone Null Data so they can reset themselves
        savingSystem.NewTerritoryRestore(true);
        InvokeSceneLoadedEvents(true);

        yield return new WaitForSecondsRealtime(0.5f); //Short Delay to Hide Camera Transition when Warping Player.

        Debug.Log("BEgin New Game Cinematic called");
        BeginNewGameCinematic?.Invoke(); //For The Story Manager To Begin New Game Cinematic.
        loadingScreen.Fade(false);
        AllowSelfDataLoad = true;
    }

    IEnumerator LoadTitleScreen()
    {
        ControlsManager.Instance.DisableControls();
        AudioManager.Instance.StopMusic();
        LoadingEnabled = true;
        AllowSelfDataLoad = false;
        loadingScreen.Fade(true);
        yield return new WaitForSecondsRealtime(loadingScreen.fadeInTime);
        GameManager.Instance.ActivateActiveMenu(false);
        GameManager.Instance.ResetTimeScale();
        //Load Scene & Wait
        yield return LoadScene(0, LoadSceneMode.Single);
        loadingScreen.Fade(false); 
    }

    IEnumerator LoadScene(int buildIndex, LoadSceneMode loadSceneMode)
    {
        string sceneName = SceneManager.GetSceneByBuildIndex(buildIndex).name;
        yield return LoadScene(sceneName, loadSceneMode);
    }

    IEnumerator LoadScene(string sceneName, LoadSceneMode loadSceneMode)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);

        yield return asyncOperation;

        if (loadSceneMode == LoadSceneMode.Additive)
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        }
    }

    public bool IsLoading()
    {
        return loadingScreen.gameObject.activeInHierarchy;
    }

    private void EnablePlayerControls()
    {
        if (!enablePlayerControlsOnLoadComplete) { return; }
        ControlsManager.Instance.SwitchCurrentActionMap(playerActionMap);
    }

    public void SetEnableControls(bool enable)
    {
        enablePlayerControlsOnLoadComplete = enable;
    }

    //UI Functionality
    private void SelectOption()
    {
        bool currentSlotHasData = saveState.saveSlotStates[currentSlotIndex].hasData;

        if (shouldSave)
        {
            AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

            if (currentSlotHasData && !confirmModal.activeInHierarchy)
            {
                confirmModal.SetActive(true);
            }
            else
            {
                confirmModal.SetActive(false);
                ControlsManager.Instance.DisableControls();
                SaveGame(currentSlotIndex, NewSavedData);
            }
        }
        else if (!shouldSave && currentSlotHasData)
        {
            AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

            if (confirmModal.activeInHierarchy)
            {
                confirmModal.SetActive(false);
                ControlsManager.Instance.DisableControls();
                //Activate Loading Screen
                LoadGame(currentSlotIndex);
            }
            else
            {
                confirmModal.SetActive(true);
            }
        }
        else if(!shouldSave && !currentSlotHasData)
        {
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
        }
    }

    private void UpdateSaveData(int index)
    {
        saveState.indexOfLastSavedSlot = index;
        SaveSlotState slotState = saveState.saveSlotStates[index];

        slotState.hasData = true;

        slotState.weekday = CalendarManager.Instance.currentDate.DayOfWeek.ToString();
        slotState.date = CalendarManager.Instance.GetCurrentDayMonthInGameFormat();
        slotState.period = CalendarManager.Instance.currentPeriod.ToString();

        slotState.activeSceneName = GameSystemsManager.Instance.GetCurrentSceneData().GetSceneName();

        slotState.leaderLevel = PartyManager.Instance.GetLeaderLevel();
        slotState.difficulty = GameManager.Instance.GetGameDifficulty().ToString();

        slotState.playTime = currentLoadedPlaytime + Time.unscaledTime; //Current Loaded Playtime.

        SavePersistentData(savingManagerKey, CaptureState());
    }

    private void NewSavedData()
    {
        EnableControls();
        slotHeader.GetChild(currentSlotIndex).GetComponent<SaveSlot>().SetData(saveState.saveSlotStates[currentSlotIndex]);
    }

    private void EnableControls()
    {
        ControlsManager.Instance.SwitchCurrentActionMap(this);
    }

    //UI

    public void ActivateUI(bool isSave)
    {
        shouldSave = isSave;
        EnableControls();

        if (isSave)
        {
            currentSlotIndex = 0;
            confirmText.text = confirmSaveMessage;
        }
        else
        {
            currentSlotIndex = saveState.indexOfLastSavedSlot;
            confirmText.text = confirmLoadMessage;
        }

        saveTitleArea.SetActive(isSave);
        loadTitleArea.SetActive(!isSave);

        CreateUI();

        saveUI.SetActive(true);
    }

    public void CreateUI()
    {
        foreach (Transform slot in slotHeader)
        {
            int index = slot.GetSiblingIndex();
            slot.GetComponent<SaveSlot>().SetData(saveState.saveSlotStates[index]);
        }

        UpdateSelectedUI(0);
    }

    private void UpdateSelectedUI(int indexChange)
    {
        if(indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        CombatFunctions.UpdateListIndex(indexChange, currentSlotIndex, out currentSlotIndex, slotHeader.childCount);

        foreach(Transform slot in slotHeader)
        {
            bool isSelected = slot.GetSiblingIndex() == currentSlotIndex;
            slot.GetComponent<SaveSlot>().SetIsSelected(isSelected, isSelected ? selectedColor : defaultColor);
        }
    }

    //Input
    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
            {
                int indexChange = context.action.name == "ScrollD" ? 1 : -1;

                UpdateSelectedUI(indexChange);
            }
        }
    }

    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            SelectOption();
        }
    }


    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed)
        {
            AudioManager.Instance.PlaySFX(SFXType.TabBack);

            if (confirmModal.activeInHierarchy)
            {
                confirmModal.SetActive(false);
            }
            else
            {
                saveUI.SetActive(false);
                GameManager.Instance.ReturnToActiveMenu();
            }
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;
        }
    }

    //Save States
    [System.Serializable]
    public class SaveManagerState
    {
        public int indexOfLastSavedSlot = 0;
        public SaveSlotState[] saveSlotStates = new SaveSlotState[5];
    }

    [System.Serializable]
    public class SaveSlotState
    {
        public bool hasData = false;
        public string weekday;
        public string date;
        public string period;
        public string activeSceneName;
        public int leaderLevel;
        public string difficulty;
        public float playTime = 0;
    }

    public object CaptureState()
    {
        return SerializationUtility.SerializeValue(saveState, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        if(state == null) { return; }

        byte[] bytes = state as byte[];
        saveState = SerializationUtility.DeserializeValue<SaveManagerState>(bytes, DataFormat.Binary);
    }
}
