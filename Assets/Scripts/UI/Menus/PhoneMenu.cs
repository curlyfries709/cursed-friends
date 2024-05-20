using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using AnotherRealm;
using UnityEngine.InputSystem;

public class PhoneMenu : MonoBehaviour, IControls
{
    public static PhoneMenu Instance { get; private set; }

    [Header("Game Menu")]
    [SerializeField] GameObject gameMenuCam;
    [SerializeField] FadeUI gameMenuFader;
    [Header("Menu Components")]
    [SerializeField] GameObject fantasyGameMenu;
    [SerializeField] GameObject modernGameMenu;
    [Space(10)]
    [SerializeField] FadeUI appUnavailableArea;
    [SerializeField] float unavailableMessageDisplayTime = 0.5f;
    [Header("Menu Headers")]
    [SerializeField] Transform fantasyAppHeader;
    [SerializeField] Transform modernAppHeader;
    [Header("Core Apps")]
    [SerializeField] HelpUI helpUI;
    [Header("Values")]
    [SerializeField] int phoneColumnCount = 3;

    const string myActionMap = "GameMenu";
    int menuIndex = 0;

    bool playerInDanger = false;
    bool subscibedToPlayerEvent = false;

    bool unavailableMessageDisplayed = false;

    private void Awake()
    {
        Instance = this;

        gameMenuFader.gameObject.SetActive(false);
        gameMenuFader.transform.parent = transform;
    }


    private void OnEnable()
    {
        ControlsManager.Instance.SubscribeToPlayerInput(myActionMap, this);
        SavingLoadingManager.Instance.DataAndSceneLoadComplete += SubscribeToPlayerEvent;
    }

    private void Start()
    {
        SubscribeToPlayerEvent();
    }

    private void SubscribeToPlayerEvent()
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
        SavingLoadingManager.Instance.DataAndSceneLoadComplete -= SubscribeToPlayerEvent;

        if (subscibedToPlayerEvent)
            PlayerStateMachine.PlayerInDanger -= SetPlayerInDanger;
    }



    //MENUS
    public void ActivateGameMenu(bool activate)
    {
        if (playerInDanger && activate)
        {
            Debug.Log("CANNOT OPEN PHONE WHILE BEEING CHASED");
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            return;
        }
        else if (activate)
        {
            AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);
        }

        ResetData();

        ControlsManager.Instance.DisableControls();
        HUDManager.Instance.HideHUDs();

        modernGameMenu.SetActive(false);
        fantasyGameMenu.SetActive(true);

        gameMenuCam.SetActive(activate);
        gameMenuFader.Fade(activate, EnableControls);

        PlayerGridUnit leader = PartyData.Instance.GetLeader();
        leader.unitAnimator.SetBool(leader.unitAnimator.animIDTexting, activate);
    }

    public void OpenApp(bool open)
    {
        gameMenuFader.gameObject.SetActive(!open);
        if (!open)
        {
            EnableControls();
        }
    }


    private void OnChangeView()
    {
        if (!gameMenuCam.activeInHierarchy) { return; }

        AudioManager.Instance.PlaySFX(SFXType.TabForward);

        menuIndex = 0;

        modernGameMenu.SetActive(!modernGameMenu.activeInHierarchy);
        fantasyGameMenu.SetActive(!fantasyGameMenu.activeInHierarchy);

        UpdateUI(Vector2.zero);
    }

    private void EnableControls()
    {
        if (gameMenuCam.activeInHierarchy)
        {
            ControlsManager.Instance.SwitchCurrentActionMap(myActionMap);
        }
        else
        {
            ControlsManager.Instance.SwitchCurrentActionMap("Player");
        }
    }


    //Logic
    public void ResumeGame()
    {
        if (gameMenuCam.activeInHierarchy)
        {
            AudioManager.Instance.PlaySFX(SFXType.TabBack);
            ActivateGameMenu(false);
            return;
        }
    }


    public void DisplayAppUnavailableMessage()
    {
        AudioManager.Instance.PlaySFX(SFXType.ActionDenied);

        if (!unavailableMessageDisplayed)
            StartCoroutine(UnavailableMessageRoutine());
    }

    IEnumerator UnavailableMessageRoutine()
    {
        unavailableMessageDisplayed = true;
        appUnavailableArea.Fade(true);
        yield return new WaitForSeconds(unavailableMessageDisplayTime);
        appUnavailableArea.Fade(false);
        unavailableMessageDisplayed = false;
    }

    //UI


    private void UpdateUI(Vector2 indexChange)
    {
        if(indexChange != Vector2.zero)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        Transform header = modernGameMenu.activeInHierarchy ? modernAppHeader : fantasyAppHeader;
        CombatFunctions.UpdateGridIndex(indexChange, ref menuIndex, phoneColumnCount, header.childCount);

        foreach (Transform option in header)
        {
            bool isSelected = option.GetSiblingIndex() == menuIndex;
            option.GetChild(2).gameObject.SetActive(isSelected);
        }
    }
 




    //INPUT

    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
            {
                int indexChange = context.action.name == "ScrollD" ? 1 : -1;

                UpdateUI(new Vector2(0, indexChange));
            }
        }
    }

    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "CycleR")
            {
                UpdateUI(new Vector2(1, 0));
            }
            else if (context.action.name == "CycleL")
            {
                UpdateUI(new Vector2(-1, 0));
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


    private void OnMap(InputAction.CallbackContext context)
    {
        if (context.action.name != "Map") { return; }

        if (context.performed)
        {
            DisplayAppUnavailableMessage();
        }
    }

    private void OnQuest(InputAction.CallbackContext context)
    {
        if (context.action.name != "Quest") { return; }

        if (context.performed)
        {
            DisplayAppUnavailableMessage();
        }
    }

    private void OnHelp(InputAction.CallbackContext context)
    {
        if (context.action.name != "Help") { return; }

        if (context.performed)
        {
            //Open Help App
            helpUI.ActivateUI(true);
        }
    }


    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed)
        {
            ResumeGame();
        }
    }

    private void OnChangeTab(InputAction.CallbackContext context)
    {
        if (context.action.name != "Tab") { return; }

        if (context.performed)
        {
            OnChangeView();
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
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnChangeTab;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnMap;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnQuest;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnHelp;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnChangeTab;

            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnMap;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnQuest;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnHelp;
        }
    }

    private void SelectOption()
    {
        Transform activeHeader = modernGameMenu.activeInHierarchy ? modernAppHeader : fantasyAppHeader;
        activeHeader.GetChild(menuIndex).GetComponent<Button>().onClick.Invoke();
    }


    private void ResetData()
    {
        menuIndex = 0;
        UpdateUI(Vector2.zero);
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }
}
