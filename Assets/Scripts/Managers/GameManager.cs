
using UnityEngine;
using AnotherRealm;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using MoreMountains.Feedbacks;
using Sirenix.Serialization;
using System.Collections;

public class GameManager : MonoBehaviour, IControls, ISaveable
{
    public static GameManager Instance { get; private set; }
    [Header("Configs")]
    public float uiScrollSpeed = 120;
    [Header("Difficulty")]
    [SerializeField] Difficulty gameDifficulty = Difficulty.Normal;
    [Space(5)]
    [SerializeField] DifficultyConfig easyModeConfig;
    [SerializeField] DifficultyConfig normalModeConfig;
    [SerializeField] DifficultyConfig hardModeConfig;
    [Header("Pause Menus")]
    [SerializeField] GameObject freeRoamPauseMenu;
    [SerializeField] GameObject combatPauseMenu;
    [Space(10)]
    [SerializeField] FadeUI saveDisabledWarning;
    [Header("Inner Pause Menus")]
    [SerializeField] SettingsUI settingsUI;
    [Header("Menu Components")]
    [SerializeField] GameObject freeRoamMenuSection;
    [SerializeField] GameObject quitConfirmationSection;
    [Header("Support Us")]
    [SerializeField] GameObject supportUsSection;
    [SerializeField] int supportIndex;
    [Header("Menu Headers")]
    [SerializeField] Transform combatMenuHeader;
    [Space(5)]
    [SerializeField] Transform freeRoamMenuHeader;
    [SerializeField] Transform quitConfirmationHeader;

    const string persistentDataKey = "GameManager";
    const string myActionMap = "Menu";

    bool warningPlaying = false;

    [HideInInspector] public float XCamSpeedMultiplier = 1;
    [HideInInspector] public float YCamSpeedMultiplier = 1;

    [HideInInspector] public bool InvertXCam = false;
    [HideInInspector] public bool InvertYCam = false;

    //Saving Data
    [SerializeField, HideInInspector]
    private GameSettingsState settingsState = new GameSettingsState();
    private bool isDataRestored = false;

    SavingLoadingManager savingManager;

    [System.Serializable]
    public class DifficultyConfig
    {
        public float playerDamageMultiplier = 1;
        public float enemyDamageMultiplier = 1;
        public float sussometerRateMultiplier = 1;
        public float enemyChaseSpeedMultiplier = 1;
        [Space(10)]
        [TextArea(3,5)]
        public string difficultyDescription;
    }

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
        //Nightmare
    }

    int menuIndex = 0;
    int confirmationIndex = 0;

    bool inCombat = false;
    bool confirmationRequired = false;

    bool playerInDanger = false;
    bool subscibedToPlayerEvent = false;

    //Cache
    GameObject activeMenu;
    IControls activeMenuControls;
    

    private void Awake()
    {
        if (!Instance)
            Instance = this;

        savingManager = SavingLoadingManager.Instance;

        LoadGameSettings();
    }

    private void OnEnable()
    {
        ControlsManager.Instance.SubscribeToPlayerInput(myActionMap, this);
        SavingLoadingManager.Instance.LoadGameDataComplete += SubscribeToPlayerEvent;
    }
    private void Start()
    {
        SettingsUI.GameSettingsUpdated += SaveNewSettings;
        SubscribeToPlayerEvent(null);
    }


    private void SubscribeToPlayerEvent(SceneData newSceneData)
    {
        if (PlayerStateMachine.PlayerInDanger != null && !subscibedToPlayerEvent)
        {
            subscibedToPlayerEvent = true;
            PlayerStateMachine.PlayerInDanger += SetPlayerInDanger;
        }
    }

    private void SetPlayerInDanger(bool inDanger)
    {
        playerInDanger = inDanger;
    }


    private void OnDisable()
    {
        //SettingsUI.GameSettingsUpdated -= SaveNewSettings;
        SavingLoadingManager.Instance.LoadGameDataComplete -= SubscribeToPlayerEvent;

        if (subscibedToPlayerEvent)
            PlayerStateMachine.PlayerInDanger -= SetPlayerInDanger;
    }


    //Difficulty
    public float GetPlayerDifficultyDamageMultiplier()
    {
        switch (gameDifficulty)
        {
            case Difficulty.Easy:
                return easyModeConfig.playerDamageMultiplier;
            case Difficulty.Normal:
                return normalModeConfig.playerDamageMultiplier;
            default:
                return hardModeConfig.playerDamageMultiplier;
        }
    }

    public float GetEnemyDifficultyDamageMultiplier()
    {
        switch (gameDifficulty)
        {
            case Difficulty.Easy:
                return easyModeConfig.enemyDamageMultiplier;
            case Difficulty.Normal:
                return normalModeConfig.enemyDamageMultiplier;
            default:
                return hardModeConfig.enemyDamageMultiplier;
        }
    }

    public float GetDifficultySussmometerMultiplier()
    {
        switch (gameDifficulty)
        {
            case Difficulty.Easy:
                return easyModeConfig.sussometerRateMultiplier;
            case Difficulty.Normal:
                return normalModeConfig.sussometerRateMultiplier;
            default:
                return hardModeConfig.sussometerRateMultiplier;
        }
    }

    public float GetDifficultyChaseSpeedMultiplier()
    {
        switch (gameDifficulty)
        {
            case Difficulty.Easy:
                return easyModeConfig.enemyChaseSpeedMultiplier;
            case Difficulty.Normal:
                return normalModeConfig.enemyChaseSpeedMultiplier;
            default:
                return hardModeConfig.enemyChaseSpeedMultiplier;
        }
    }

    public string GetDifficultyDescription(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                return easyModeConfig.difficultyDescription;
            case Difficulty.Normal:
                return normalModeConfig.difficultyDescription;
            default:
                return hardModeConfig.difficultyDescription;
        }
    }

    //MENUS
    public void SetActiveMenu(GameObject menu, IControls controls)
    {
        activeMenu = menu;
        activeMenuControls = controls;
    }

    public void PauseGame(bool inCombat)
    {
        AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

        this.inCombat = inCombat;

        ResetData();

        FreezeGame();
        HUDManager.Instance.HideHUDs();

        SetActiveMenu(inCombat ? combatPauseMenu : freeRoamPauseMenu, this);
        ActivateActiveMenu(true);

        ControlsManager.Instance.SwitchCurrentActionMap(this);
    }

    public void UnPauseGame()
    {
        if(MMTimeManager.Instance.CurrentTimeScale == 1) { return; }

        MMTimeManager.Instance.SetTimeScaleTo(1);
        ActivateActiveMenu(false);
    }

    public void FreezeGame()
    {
        MMTimeManager.Instance.SetTimeScaleTo(0);
    }

    //Logic
    public void ResumeGame()
    {
        ActivateActiveMenu(false);
        ResetTimeScale();

        if (FantasyCombatManager.Instance && FantasyCombatManager.Instance.InCombat())
        {
            FantasyCombatManager.Instance.ShowHUD(true);
            ControlsManager.Instance.SwitchCurrentActionMap("FantasyCombat");
        }
        else
        {
            ControlsManager.Instance.SwitchCurrentActionMap("Player");
        }    
    }

    public void ResetTimeScale()
    {
        MMTimeManager.Instance.SetTimeScaleTo(1);
    }

    public void Save()
    {
        if (playerInDanger) 
        {
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            if (!warningPlaying)
                StartCoroutine(SaveDisableWarning());
            return; 
        }

        ActivateActiveMenu(false);
        savingManager.ActivateUI(true);
    }

    IEnumerator SaveDisableWarning()
    {
        warningPlaying = true;

        saveDisabledWarning.Fade(true);

        yield return new WaitForSeconds(0.75f);

        saveDisabledWarning.Fade(false);

        warningPlaying = false;
    }

    public void Load()
    {
        ActivateActiveMenu(false);
        savingManager.ActivateUI(false);
    }

    public void ActivateSettings()
    {
        ActivateActiveMenu(false);
        settingsUI.gameObject.SetActive(true);
    }

    public void ReturnToActiveMenu()
    {
        ControlsManager.Instance.SwitchCurrentActionMap(activeMenuControls);
        ActivateActiveMenu(true);
    }

    public void FirstQuit()
    {
        confirmationRequired = true;

        confirmationIndex = 0;
        UpdateUI(0);

        freeRoamMenuSection.SetActive(false);
        quitConfirmationSection.SetActive(true);
    }

    public void ConfirmQuit()
    {
        SavingLoadingManager.Instance.ReturnToTitleScreen();
        CancelQuit();
    }

    public void CancelQuit()
    {
        if (!confirmationRequired) { return; }

        confirmationRequired = false;

        menuIndex = 0;
        UpdateUI(0);

        freeRoamMenuSection.SetActive(true);
        quitConfirmationSection.SetActive(false);
    }

    public void Retry()
    {
        FantasyCombatManager.Instance.RetryBattle();
    }

    public void OpenSocialLink(string link)
    {
        Application.OpenURL(link);
    }


    //Settings
    public void UpdateDifficulty(Difficulty newDifficulty)
    {
        gameDifficulty = newDifficulty;
    }

    //UI
    public void ActivateActiveMenu(bool activate)
    {
        if(!activeMenu) { return; }

        if(activeMenu.TryGetComponent(out FadeUI fadeUI))
        {
            fadeUI.Fade(activate);
        }
        else
        {
            activeMenu.SetActive(activate);
        }
    }

    private void UpdateUI(int indexChange)
    {
        Transform header;

        if(indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        if (confirmationRequired)
        {
            header = quitConfirmationHeader;
            CombatFunctions.UpdateListIndex(indexChange, confirmationIndex, out confirmationIndex, quitConfirmationHeader.childCount);
        }
        else
        {   
            header = inCombat ? combatMenuHeader : freeRoamMenuHeader;
            CombatFunctions.UpdateListIndex(indexChange, menuIndex, out menuIndex, header.childCount);
        }

        foreach (Transform option in header)
        {
            bool isSelected = confirmationRequired ? option.GetSiblingIndex() == confirmationIndex : option.GetSiblingIndex() == menuIndex;

            option.GetChild(0).gameObject.SetActive(isSelected);
            option.GetChild(1).gameObject.SetActive(!isSelected);
        }

        if(header == freeRoamMenuHeader)
        {
            supportUsSection.SetActive(menuIndex == supportIndex && !confirmationRequired);
        }
    }


    //GETTERS
    public Difficulty GetGameDifficulty()
    {
        return gameDifficulty;
    }

    [System.Serializable]
    public class GameSettingsState
    {
        //Difficulty is not persistent so shouldn't get saved.

        public float musicVolume;
        public float sfxVolume;
        public float uiVolume;

        public float XCamSpeedMultiplier;
        public float YCamSpeedMultiplier;

        public bool InvertXCam;
        public bool InvertYCam;
    }


    //Save Settings
    private void SaveNewSettings()
    {
        settingsState.XCamSpeedMultiplier = XCamSpeedMultiplier;
        settingsState.YCamSpeedMultiplier = YCamSpeedMultiplier;

        settingsState.InvertXCam = InvertXCam;
        settingsState.InvertYCam = InvertYCam;

        savingManager.SavePersistentData(persistentDataKey, SerializationUtility.SerializeValue(settingsState, DataFormat.Binary));
    }

    private void LoadGameSettings()
    {
        object loadedData = savingManager.LoadPersistentData(persistentDataKey);

        if(loadedData == null) { return; }

        byte[] bytes = loadedData as byte[];
        settingsState = SerializationUtility.DeserializeValue<GameSettingsState>(bytes, DataFormat.Binary);

        XCamSpeedMultiplier = settingsState.XCamSpeedMultiplier;
        YCamSpeedMultiplier = settingsState.YCamSpeedMultiplier;

        InvertXCam = settingsState.InvertXCam;
        InvertYCam = settingsState.InvertYCam;
    }

    //Save Difficulty
    public object CaptureState()
    {
        return SerializationUtility.SerializeValue(gameDifficulty, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        isDataRestored = true;
        if (state == null) { return; } //Means It's a New Game, Difficulty would be set via title screen.

        byte[] bytes = state as byte[];
        gameDifficulty = SerializationUtility.DeserializeValue<Difficulty>(bytes, DataFormat.Binary);
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }


    //INPUT

    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
            {
                int indexChange = context.action.name == "ScrollD" ? 1 : -1;

                UpdateUI(indexChange);

            }
        }
    }

    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "CycleR")
            {
                
            }
            else if (context.action.name == "CycleL")
            {
                
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

            if (confirmationRequired)
            {
                CancelQuit();
            }
            else
            {
                ResumeGame();
            }
        }
    }
    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;
        }
    }

    private void SelectOption()
    {
        AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

        if (confirmationRequired)
        {
            quitConfirmationHeader.GetChild(confirmationIndex).GetComponent<Button>().onClick.Invoke();
        }
        else
        {
            Transform activeHeader = inCombat ? combatMenuHeader : freeRoamMenuHeader;

            activeHeader.GetChild(menuIndex).GetComponent<Button>().onClick.Invoke();
        }
    }


    private void ResetData()
    {
        confirmationIndex = 0;
        menuIndex = 0;

        confirmationRequired = false;

        UpdateUI(0);
    }
}
