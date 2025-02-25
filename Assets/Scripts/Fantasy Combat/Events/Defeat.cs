using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using AnotherRealm;
using Cinemachine;
using DG.Tweening;
using UnityEngine.UI;
using MoreMountains.Feedbacks;

public class Defeat : MonoBehaviour, IControls
{
    [Header("Timers")]
    [SerializeField] float canvasDelay = 0.3f;
    [Space(5)]
    [SerializeField] float defeatCanvasDisplayTime;
    [SerializeField] float defeatCanvasFadeOutTime = 0.5f;
    [Header("Canvases")]
    [SerializeField] CanvasGroup defeatCanvas;
    [SerializeField] FadeUI gameOverCanvas;
    [SerializeField] FadeUI retryFader;
    [Header("Cameras")]
    [SerializeField] GameObject partyKOCam;
    [SerializeField] CinemachineTargetGroup partyKOTargetGroup;
    [Space(5)]
    [SerializeField] float targetGroupWeight = 1;
    [SerializeField] float targetGroupRadius = 1.8f;
    [Header("Menu Components")]
    [SerializeField] GameObject gameOverSection;
    [SerializeField] GameObject quitConfirmationSection;
    [Space(5)]
    [SerializeField] Transform gameOverHeader;
    [SerializeField] Transform quitConfirmationHeader;
    //[Header("Cameras")]

    //Cache
    CharacterGridUnit characterWhoDealtFinalBlow;
    CharacterGridUnit KOEDUnit;
    GameObject KOCam;

    int gameOverIndex = 0;
    int confirmationIndex = 0;

    bool confirmationRequired = false;

    private void OnEnable()
    {
        ControlsManager.Instance.SubscribeToPlayerInput("Menu", this);
    }

    public void OnDefeat(PlayerGridUnit KOEDUnit)
    {
        ControlsManager.Instance.DisableControls();
        FantasyCombatManager.Instance.CombatEnded?.Invoke(BattleResult.Defeat, FantasyCombatManager.Instance.battleTrigger);

        //Play Defeat Music
        AudioManager.Instance.PlayMusic(MusicType.Defeat);

        ResetData();

        this.KOEDUnit = KOEDUnit;
        KOCam = KOEDUnit.koCam;

        characterWhoDealtFinalBlow = KOEDUnit.Health().attacker as CharacterGridUnit;

        FantasyCombatManager.Instance.ShowHUD(false);

        if (characterWhoDealtFinalBlow)
        {
            characterWhoDealtFinalBlow.ReturnToPosAfterAttack(true);
            characterWhoDealtFinalBlow.unitAnimator.ShowModel(false);
        }

        List<Transform> partyTransforms = new List<Transform>();

        foreach (PlayerGridUnit player in FantasyCombatManager.Instance.GetPlayerCombatParticipants(true, true))
        {
            partyTransforms.Add(player.camFollowTarget);
        }

        CombatFunctions.OverrideCMTargetGroup(partyKOTargetGroup, partyTransforms, targetGroupWeight, targetGroupRadius);
        ActivateFinalKOCam(true);

        StartCoroutine(DefeatRoutine());
    }



    IEnumerator DefeatRoutine()
    {
        yield return new WaitForSeconds(FantasyCombatManager.Instance.GetSkillFeedbackDisplayTime() - 0.15f);
        KOEDUnit.Health().ActivateHealthVisual(false);
        KOEDUnit.unitAnimator.ActivateSlowmo();
        yield return new WaitForSeconds(canvasDelay);
        defeatCanvas.alpha = 1;
        defeatCanvas.gameObject.SetActive(true);
        yield return new WaitForSeconds(defeatCanvasDisplayTime);
        KOEDUnit.unitAnimator.ReturnToNormalSpeed();
        if(characterWhoDealtFinalBlow)
            characterWhoDealtFinalBlow.unitAnimator.ShowModel(true);

        IBattleTrigger battleTrigger = FantasyCombatManager.Instance.battleTrigger;
        defeatCanvas.DOFade(0, defeatCanvasFadeOutTime);

        if (battleTrigger.CanPlayDefeatScene())
        {
            ActivateFinalKOCam(false);
            partyKOCam.SetActive(true);
            gameOverSection.SetActive(true);
            quitConfirmationSection.SetActive(false);
            gameOverCanvas.Fade(true, SwitchToMenuControls);
        }
        else
        {
            retryFader.Fade(true, TriggerDefeatEvent);
        }
    }



    private void ActivateFinalKOCam(bool show)
    {
        KOCam.SetActive(show);
    }

    //Logic

    public void Retry()
    {
        ControlsManager.Instance.DisableControls();
        GameManager.Instance.UnPauseGame();
        FantasyCombatManager.Instance.BattleInterrupted();
        retryFader.Fade(true, RetryFadeInComplete);
    }

    private void RetryFadeInComplete()
    {
        partyKOCam.SetActive(false);
        gameOverCanvas.Fade(false);
        FantasyCombatManager.Instance.RestartBattle();
        retryFader.Fade(false);
    }

    private void TriggerDefeatEvent()
    {
        IBattleTrigger battleTrigger = FantasyCombatManager.Instance.battleTrigger;
        ActivateFinalKOCam(false);
        battleTrigger.TriggerDefeatEvent(FantasyCombatManager.Instance.GetEnemyCombatParticipants(false, true), retryFader.fadeOutTime);
        retryFader.Fade(false);
    }

    public void Load()
    {
        Debug.Log("LOAD SELECTED!");
        GameManager.Instance.SetActiveMenu(gameOverCanvas.gameObject, this);
        GameManager.Instance.Load();
    }

    public void FirstQuit()
    {
        confirmationRequired = true;

        confirmationIndex = 0;
        UpdateUI(0);

        gameOverSection.SetActive(false);
        quitConfirmationSection.SetActive(true);
    }

    public void ConfirmQuit()
    {
        GameManager.Instance.ConfirmQuit();
    }

    public void CancelQuit()
    {
        if (!confirmationRequired) { return; }

        confirmationRequired = false;

        gameOverIndex = 0;
        UpdateUI(0);

        gameOverSection.SetActive(true);
        quitConfirmationSection.SetActive(false);
    }

    //UI
    private void UpdateUI(int indexChange)
    {
        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        if (confirmationRequired)
        {
            CombatFunctions.UpdateListIndex(indexChange, confirmationIndex, out confirmationIndex, quitConfirmationHeader.childCount);
        }
        else
        {
            CombatFunctions.UpdateListIndex(indexChange, gameOverIndex, out gameOverIndex, gameOverHeader.childCount);
        }

        Transform header = confirmationRequired ? quitConfirmationHeader : gameOverHeader;

        foreach (Transform option in header)
        {
            bool isSelected = confirmationRequired ? option.GetSiblingIndex() == confirmationIndex : option.GetSiblingIndex() == gameOverIndex;

            option.GetChild(0).gameObject.SetActive(isSelected);
            option.GetChild(1).gameObject.SetActive(!isSelected);
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

                UpdateUI(indexChange);
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
            if (confirmationRequired)
                AudioManager.Instance.PlaySFX(SFXType.TabBack);

            CancelQuit();
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

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
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
            gameOverHeader.GetChild(gameOverIndex).GetComponent<Button>().onClick.Invoke();
        }
    }

    private void SwitchToMenuControls()
    {
        defeatCanvas.gameObject.SetActive(false);
        ControlsManager.Instance.SwitchCurrentActionMap(this);
    }

    private void ResetData()
    {
        confirmationIndex = 0;
        gameOverIndex = 0;
        confirmationRequired = false;
    }
}
