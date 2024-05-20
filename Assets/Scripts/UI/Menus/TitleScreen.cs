using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using AnotherRealm;
using TMPro;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour, IControls
{
    [Header("Music")]
    [SerializeField] AudioClip titleScreenMusic;
    [Header("Headers")]
    [SerializeField] Transform menuOptionsHeader;
    [SerializeField] Transform socialHeader;
    [Space(10)]
    [SerializeField] FadeUI titleScreenMenuOptions;
    [SerializeField] FadeUI supportArea;
    [Header("UI")]
    [SerializeField] Color defaultTextColor = Color.white;
    [SerializeField] Color selectedTexColor;
    [Space(5)]
    [SerializeField] int supportUsIndex = 4;
    [SerializeField] int separatorIndex = 3;
    [Header("New Game")]
    [SerializeField] GameObject newGameArea;
    [Space(5)]
    [SerializeField] FadeUI selectDifficultyArea;
    [Space(5)]
    [SerializeField] Transform difficultyOptionsHeader;
    [SerializeField] TextMeshProUGUI difficultyDescription;
    [Space(5)]
    [SerializeField] FadeUI storyArea;
    [SerializeField] TextMeshProUGUI storyText;
    [SerializeField] GameObject storyNextIcon;

    int menuIndex = 0;
    int socialIndex = 0;
    int difficultyIndex = 1;

    GameManager.Difficulty currentDifficulty = GameManager.Difficulty.Normal;

    bool newGameBegun = false;
    bool selectDifficulty = false;
    const string myActionKey = "TitleScreen";

    Coroutine currentRoutine = null;


    private void Awake()
    {
        ControlsManager.Instance.SubscribeToPlayerInput(myActionKey, this);
    }

    private void Start()
    {
        //Play Music
        if(titleScreenMusic)
            AudioManager.Instance.PlayCustomMusic(titleScreenMusic);

        GameManager.Instance.SetActiveMenu(menuOptionsHeader.gameObject, this);
        ControlsManager.Instance.SwitchCurrentActionMap(myActionKey);
    }

    //New Game Logic
    public void BeginNewGame()
    {
        ActivateNewGameArea(true);
        SelectDifficulty();
    }

    private void ActivateNewGameArea(bool activate)
    {
        titleScreenMenuOptions.Fade(!activate);

        newGameBegun = activate;
        newGameArea.SetActive(activate);
        selectDifficultyArea.Fade(activate);
    }

    private void SelectDifficulty()
    {
        difficultyIndex = 1;
        selectDifficulty = true;

        UpdateUI(0);
    }

    private void Next()
    {
        AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

        if (selectDifficulty)
        {
            //Set Difficulty
            GameManager.Instance.UpdateDifficulty(currentDifficulty);
            //Begin Story
            ActivateStoryArea(true);
        }
        else
        {
            //Means Viewing story, so begin new Game
            ControlsManager.Instance.DisableControls();
            SavingLoadingManager.Instance.LoadNewGame();
        }
    }

    private void ActivateStoryArea(bool activate)
    {
        selectDifficultyArea.Fade(!activate);
        storyArea.Fade(activate);

        if (activate)
        {
            selectDifficulty = false;
            currentRoutine = StartCoroutine(TypeStory());
        }
        else if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }    
    }

    private void GoBack()
    {
        AudioManager.Instance.PlaySFX(SFXType.TabBack);

        if (selectDifficulty)
        {
            ActivateNewGameArea(false);
        }
        else
        {
            //GO Back to select Difficulty
            ActivateStoryArea(false);
            SelectDifficulty();
        }
    }

    IEnumerator TypeStory()
    {
        storyNextIcon.SetActive(false);
        string story = storyText.text;
        storyText.text = "";

        foreach (char letter in story.ToCharArray())
        {
            storyText.text = storyText.text + letter;
            yield return null;
        }

        //Show Caret Icon.
        storyNextIcon.SetActive(true);
        currentRoutine = null;
    }

    //Other Logic
    public void SocialLink(string link)
    {
        Application.OpenURL(link);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    //UI
    private void UpdateUI(int indexChange)
    {
        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        if(newGameBegun && selectDifficulty)
        {
            UpdateDifficultyUI(indexChange);
            return;
        }

        bool updateSocials = IsSupportAreaActive();

        CombatFunctions.UpdateListIndex(indexChange, menuIndex, out menuIndex, menuOptionsHeader.childCount);

        if (menuIndex == separatorIndex) //Skip Separator.
            menuIndex = menuIndex + indexChange;

        foreach (Transform option in menuOptionsHeader)
        {
            bool isSelected = option.GetSiblingIndex() == menuIndex;

            option.GetChild(0).gameObject.SetActive(isSelected);

            if(option.GetChild(1).TryGetComponent(out TextMeshProUGUI textMesh))
                textMesh.color = isSelected ? selectedTexColor : defaultTextColor;
        }

        if (updateSocials || IsSupportAreaActive())
            UpdateSocialUI(0);

        supportArea.Fade(IsSupportAreaActive());
    }

    private void UpdateDifficultyUI(int indexChange)
    {
        CombatFunctions.UpdateListIndex(indexChange, difficultyIndex, out difficultyIndex, difficultyOptionsHeader.childCount);

        foreach (Transform option in difficultyOptionsHeader)
        {
            bool isSelected = option.GetSiblingIndex() == difficultyIndex;
            option.GetChild(0).gameObject.SetActive(isSelected);
        }

        difficultyOptionsHeader.GetChild(difficultyIndex).GetComponent<Button>().onClick.Invoke();
    }

    public void SetDifficultyDescription(int difficulty)
    {
        currentDifficulty = (GameManager.Difficulty)difficulty;
        difficultyDescription.text = GameManager.Instance.GetDifficultyDescription(currentDifficulty);
    }

    private void UpdateSocialUI(int indexChange)
    {
        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        CombatFunctions.UpdateListIndex(indexChange, socialIndex, out socialIndex, socialHeader.childCount);

        foreach (Transform option in socialHeader)
        {
            bool isSelected = option.GetSiblingIndex() == socialIndex;

            option.GetChild(0).gameObject.SetActive(isSelected && IsSupportAreaActive());
        }
    }

    private void SelectOption()
    {
        if(IsSupportAreaActive())
        {
            //Activate Social Link
            AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);
            socialHeader.GetChild(socialIndex).GetComponent<Button>().onClick.Invoke();
        }
        else
        {
            AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);
            menuOptionsHeader.GetChild(menuIndex).GetComponent<Button>().onClick.Invoke();
        } 
    }

    private bool IsSupportAreaActive()
    {
        return menuIndex == supportUsIndex;
    }

    //Button Events
    public void Load()
    {
        GameManager.Instance.Load();
    }

    public void Settings()
    {
        GameManager.Instance.ActivateSettings();
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
        if (context.performed && IsSupportAreaActive())
        {
            if (context.action.name == "CycleR")
            {
                UpdateSocialUI(1);
            }
            else if (context.action.name == "CycleL")
            {
                UpdateSocialUI(-1);
            }
        }
    }


    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            if (newGameBegun)
            {
                Next();
            }
            else
            {
                SelectOption();
            }
        }
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed && newGameBegun)
        {
            //Only Allowed when Started Begin New Game Process
            GoBack();
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }


}
