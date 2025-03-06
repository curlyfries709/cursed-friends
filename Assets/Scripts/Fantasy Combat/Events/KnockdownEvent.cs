using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Linq;
using Cinemachine;
using AnotherRealm;
using DG.Tweening;
using MoreMountains.Feedbacks;

public class KnockdownEvent : CombatAction, ITurnEndEvent
{
    [Header("Scripts")]
    [SerializeField] Beatdown beatdown;
    [SerializeField] PowerOfFriendship powerOfFriendship;
    [Header("Components")]
    [SerializeField] Animator knockdownTextAnimator;
    [Header("Cameras")]
    [SerializeField] CinemachineVirtualCamera chainAttackCam;
    [Header("Canvases")]
    [SerializeField] FadeUI flasher;
    [Space(5)]
    [SerializeField] FadeUI knockdownText;
    [SerializeField] FadeUI knockdownSpeedlines;
    [Space(5)]
    [SerializeField] CombatEventCanvas chainTriggeredCanvas;
    [Header("SP Recovery")]
    [SerializeField] int firstSPRecovery = 3;
    [SerializeField] int SPGainPerChain = 2;
    [Header("Timers")]
    [SerializeField] float flashDuration = 0.1f;
    [Space(5)]
    [SerializeField] float defaultKnockdownUIDuration = 0.5f;
    [SerializeField] float canChainKnockdownUIDuration = 0.3f;
    [Space(5)]
    [Range(2, 10)]
    [SerializeField] float chainSelectionTimeInSecs = 5;
    [Space(5)]
    [Range(0.1f, 1f)]
    [SerializeField] float chainAttackSelectionTimeScale = 0.5f;
    [SerializeField] float chainTriggeredCanvasDuration = 0.5f;
    [Space(5)]
    [SerializeField] float selectedPotraitAnimTime = 0.25f;
    [Header("Knockdown Buttons")]
    [SerializeField] GameObject pofButton;
    [SerializeField] GameObject chainButton;
    [SerializeField] GameObject beatdownButton;
    [Header("UI")]
    [SerializeField] Image chainSPBar;
    [SerializeField] TextMeshProUGUI chainSPValue;
    [Space(5)]
    [SerializeField] GameObject chainAttackSetup;
    [Space(5)]
    [SerializeField] ScrollRect chainAttackScrollRect;
    [SerializeField] Transform potraitHeader;
    [Space(5)]
    [SerializeField] Transform skillAOEDiagramHeader;
    [SerializeField] TextMeshProUGUI skillDescription;
    [SerializeField] TextMeshProUGUI skillQuickData;
    [Space(5)]
    [SerializeField] GameObject skillPrefab;

     //Caches
    int selectedChainAttackIndex = 0;
    int selectedChainReceiverIndex = 0;

    CharacterGridUnit chainStarter;
    CharacterGridUnit currentAttacker;

    PlayerGridUnit selectedChainReceiver;
    PlayerBaseChainAttack selectedChainAttack;

    Coroutine currentRoutine = null;

    List<CharacterGridUnit> unitsAlreadyTriggeredChainAttack = new List<CharacterGridUnit>();

    List<PlayerGridUnit> playerParty = new List<PlayerGridUnit>();
    List<PlayerGridUnit> validChainPartners = new List<PlayerGridUnit>();
    List<PlayerBaseChainAttack> chainReceiverChainAttacks = new List<PlayerBaseChainAttack>();

    List<Type> otherEventTypesThatCancelThis = new List<Type>();

    bool alreadySubscribedToEvent = false;
    bool canTriggerChainAttack = false;
    bool canTriggerEvent = false;
    bool eventStarted = false;

    //Event
    public static Action<GridUnit> UnitKnockdown; //Variable of who triggered the knockdown.

     private void Start()
     {
        chainTriggeredCanvas.SetDuration(chainTriggeredCanvasDuration);

        //Clean Chain Attack Header.
        foreach(Transform child in chainAttackScrollRect.content)
        {
            Destroy(child.gameObject);
        }
     }

     private void OnEnable()
     {
         UnitKnockdown += OnUnitKnockedDown;
     }

     private void OnUnitKnockedDown(GridUnit attacker)
     {
         if (eventStarted) { return; } //To stop this method being called multiple times when multiple units are knockdowned from one attack.
            
        CharacterGridUnit attackerCharacter = attacker as CharacterGridUnit;

        if (!attackerCharacter) { return; }

        eventStarted = true;
        canTriggerEvent = false;
        currentAttacker = attackerCharacter;

        if (!unitsAlreadyTriggeredChainAttack.Contains(attackerCharacter))
        {
            unitsAlreadyTriggeredChainAttack.Add(attackerCharacter);
        }

        //Means this is the first Chain, so subscribe to current acting unit turn end event.
        if(!alreadySubscribedToEvent)
        {
            alreadySubscribedToEvent = true;
            chainStarter = attackerCharacter;
        }

        //Add Turn End Event if player beatdown available or attacker is enemy
        if (CanTriggerEvent())
        {
            canTriggerEvent = FantasyCombatManager.Instance.AddTurnEndEventToQueue(this);
        }

        StartCoroutine(KnockdownUIRoutine(FantasyCombatManager.Instance.GetSkillFeedbackDisplayTime()));
     }

    IEnumerator KnockdownUIRoutine(float skillFeedbackDuration)
    {
        bool canPlayerTriggerEvent = currentAttacker is PlayerGridUnit && canTriggerEvent;

        if (canPlayerTriggerEvent)
        {
            yield return new WaitForSeconds(skillFeedbackDuration - flashDuration - canChainKnockdownUIDuration);
        }
        else
        {
            yield return new WaitForSeconds(skillFeedbackDuration - flashDuration - defaultKnockdownUIDuration);
        }

        Flash();
        yield return new WaitForSeconds(flashDuration);

        UpdateEventButtons(false);
        ActivateKnockdownUI(true, false);

        if (canPlayerTriggerEvent)
        {
            yield return new WaitForSeconds(canChainKnockdownUIDuration);
        }
        else
        {
            yield return new WaitForSeconds(defaultKnockdownUIDuration);
        }
        
        if (!canTriggerEvent)
        {
            ActivateKnockdownUI(false, true);
        }

        eventStarted = false;
    }

     public void PlayTurnEndEvent()
     {
        bool isChainStarterPlayer = chainStarter is PlayerGridUnit;

        BeginAction();

         if (isChainStarterPlayer)
         {
             TriggerPlayerKnockdownEvent();
         }
         else
         {
             TriggerEnemyKnockdownEvent();
         }
     }

     private void TriggerPlayerKnockdownEvent()
     {
         //Show Buttons
         UpdateEventButtons(true);

         //Begin Timer.
         currentRoutine = StartCoroutine(EventSelectionCountdown());

        //Trigger Tutorial
        if (!CanActivatePOF())
        {
            if(!StoryManager.Instance.PlayTutorial(7))
                ControlsManager.Instance.SwitchCurrentActionMap("ChainSelection");
        }
        else
        {
            //Switch Controls.
            ControlsManager.Instance.SwitchCurrentActionMap("ChainSelection");
        }

        //Listen For Input.
        ListenToChainSelectionInput(true);

    }

     private void TriggerEnemyKnockdownEvent()
     {
         knockdownText.Fade(false);
         knockdownSpeedlines.Fade(false);

         if (CanEnemyTriggerBeatdown())
         {
             //Trigger Beatdown.
             beatdown.TriggerBeatdown(currentAttacker);
             return;
         }

         Again.Instance.SetUnitToGoAgain(currentAttacker);
         EndAction();
     }

    private void ActivateKnockdownUI(bool show, bool showHud, bool showWorldSpaceUI = true)
    {
        FantasyCombatManager.Instance.ShowHUD(showHud, showWorldSpaceUI);

        knockdownText.Fade(show);
        knockdownSpeedlines.Fade(show);
    }

    private void UpdateEventButtons(bool activate)
    {
        bool canActivatePOF = false;
        canTriggerChainAttack = false;

        if (activate)
        {
            canActivatePOF = CanActivatePOF();
            canTriggerChainAttack = CanTriggerChainEvent();
        }

        pofButton.SetActive(canActivatePOF && activate);
        chainButton.SetActive(canTriggerChainAttack && !canActivatePOF && activate);
        beatdownButton.SetActive(!canActivatePOF && HasKnockdownEnemies() && activate);

        if(activate)
            knockdownTextAnimator.SetTrigger("Rise");
    }

    private void PlayerBeatdownSelected()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

        ActivateKnockdownUI(false, false, false);
        ListenToChainSelectionInput(false);

        beatdown.TriggerBeatdown(currentAttacker);
    }

    private void ActivatePOF()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

        ActivateKnockdownUI(false, false, false);
        ListenToChainSelectionInput(false);

        powerOfFriendship.TriggerPOF((currentAttacker as PlayerGridUnit));
    }
    private void ChainSelected()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

        ActivateKnockdownUI(false, false, false);
        ListenToChainSelectionInput(false);

        StartCoroutine(ChainSelectedRoutine());
    }

    IEnumerator ChainSelectedRoutine()
    {
        ControlsManager.Instance.DisableControls();

        Flash();
        yield return new WaitForSeconds(flashDuration);
        currentAttacker.GetPhotoShootSet().PlayCounterUI();
        chainTriggeredCanvas.Show(true);
        RecoverSP();
        yield return new WaitForSeconds(chainTriggeredCanvasDuration);
        currentAttacker.GetPhotoShootSet().DeactivateSet();

        SelectRandomChainReceiver();
        EnableChainAttackSelection(true);
    }

    private void RecoverSP()
    {
        int spGain = firstSPRecovery + (SPGainPerChain * (unitsAlreadyTriggeredChainAttack.Count - 1));
        int currentSP = currentAttacker.CharacterHealth().currentSP;

        float animTime = chainTriggeredCanvasDuration - 0.1f;

        chainSPBar.fillAmount = currentAttacker.CharacterHealth().GetStaminaNormalized();
        chainSPValue.text = currentSP.ToString();

        //Gain SP
        currentAttacker.CharacterHealth().GainSPInstant(spGain);

        //Animate UI.
        chainSPBar.DOFillAmount(currentAttacker.CharacterHealth().GetStaminaNormalized(), animTime);
        DOTween.To(() => currentSP, x => currentSP = x, currentAttacker.CharacterHealth().currentSP, animTime).OnUpdate(() => chainSPValue.text = currentSP.ToString());
    }

    private void SelectRandomChainReceiver()
    {
        int randIndex = UnityEngine.Random.Range(0, validChainPartners.Count);

        selectedChainReceiverIndex = randIndex;
        selectedChainReceiver = validChainPartners[selectedChainReceiverIndex];

        SetPotraits();
    }

    private void SelectChainAttack()
    {
        if (selectedChainAttack.TrySelectSkill())
        {
            MMTimeManager.Instance.SetTimeScaleTo(1);

            AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

            selectedChainAttack.SetKnockdownEvent(this);
            //Add Unit to list of Units Chain With
            unitsAlreadyTriggeredChainAttack.Add(selectedChainAttack.GetSkillOwner());

            EndAttackSelection(false);
        }
        else
        {
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            Debug.Log("No Valid Targets");
            //Means This Chain Attack has no valid target so cannot be activated. Notify Player!
        }
    }

     private bool CanTriggerChainEvent()
     {
         bool isChainStarterPlayer = chainStarter is PlayerGridUnit;

         if (isChainStarterPlayer)
         {
             List<PlayerGridUnit> availableChainPartners = FantasyCombatManager.Instance.GetPlayerCombatParticipants(false, false).Where((player) => !unitsAlreadyTriggeredChainAttack.Contains(player)).ToList();
             validChainPartners = new List<PlayerGridUnit>(availableChainPartners);

             foreach (PlayerGridUnit player in availableChainPartners)
             {
                bool hasSkillWithTargets = false;

                foreach(PlayerBaseChainAttack chainAttack in player.Chain().GetChainAttacks())
                {
                    bool skillHasValidTargets = chainAttack.HasValidTargets(currentAttacker as PlayerGridUnit);
                    hasSkillWithTargets = hasSkillWithTargets || skillHasValidTargets;
                }

                 if (!hasSkillWithTargets)
                     validChainPartners.Remove(player);
             }

             if (validChainPartners.Count == 0)
             {
                 Debug.Log("Chain Attacks have no valid Targets");
             }

             return validChainPartners.Count > 0;
         }

         //return unitsAlreadyTriggeredChainAttack.Count < FantasyCombatManager.Instance.GetEnemyCombatParticipants(false, false).Count;
         return true;
     }

    private bool CanActivatePOF()
    {
        //return true;
        bool isChainStarterPlayer = chainStarter is PlayerGridUnit;

        if (!isChainStarterPlayer) { return false; }

        List<PlayerGridUnit> availablePlayers = FantasyCombatManager.Instance.GetPlayerCombatParticipants(false, false);
        int numOfAvailablePlayers = availablePlayers.Count;

        //Keenan must be available & At Least 2 players required.
        if (numOfAvailablePlayers <= 1 || !availablePlayers.Any((player) => player.unitName == PartyManager.Instance.GetLeaderName()))
        {
            return false;
        }

        if(PartyManager.Instance.GetActivePlayerParty().Count == unitsAlreadyTriggeredChainAttack.Count)
        {
            Debug.Log("All Players Chained With. Chained With Count: " + unitsAlreadyTriggeredChainAttack.Count);
            return true;
        }

        foreach (CharacterGridUnit enemy in FantasyCombatManager.Instance.GetEnemyCombatParticipants(false, false))
        {
            if (!enemy.CharacterHealth().isKnockedDown)
            {
                return false;
            }
        }

        return true;
    }

     private bool CanEnemyTriggerBeatdown()
     {
         List<PlayerGridUnit> nonKnockedDownPlayers = FantasyCombatManager.Instance.GetPlayerCombatParticipants(false, false).Where((player) => !player.CharacterHealth().isKnockedDown).ToList();

         //For Now Monsters cannot trigger beatdown.
         return nonKnockedDownPlayers.Count == 0 && currentAttacker.stats.data.race != Race.Monster;
     }

    private bool CanTriggerEvent()
    {
        return !(currentAttacker is PlayerGridUnit) || FantasyCombatManager.Instance.GetPlayerCombatParticipants(false, false).Count > 1;
    }

    public void OnSelectedChainAttackCancelled()
    {
        AudioManager.Instance.PlaySFX(SFXType.TabBack);

        selectedChainAttack = null;
        EnableChainAttackSelection(true);
    }

    private void EnableChainAttackSelection(bool updateTimeScale)
    {
        //Switch Controls.
        ControlsManager.Instance.SwitchCurrentActionMap("ChainSelection");

        ListenToChainAttackInput(true);

        if(updateTimeScale)
            MMTimeManager.Instance.SetTimeScaleTo(chainAttackSelectionTimeScale);

        DisplayChainAttackUI();
    }

    //UI
    private void DisplayChainAttackUI()
    {
        FantasyCombatManager.Instance.ShowHUD(false, false);

        selectedChainAttackIndex = 0;

        chainAttackCam.Follow = selectedChainReceiver.camFollowTarget;
        chainAttackCam.LookAt = selectedChainReceiver.camFollowTarget;

        chainReceiverChainAttacks = selectedChainReceiver.Chain().GetChainAttacks();

        foreach (PlayerBaseChainAttack chainAttack in chainReceiverChainAttacks)
        {
            int index = chainReceiverChainAttacks.IndexOf(chainAttack);
            GameObject skillObj;

            if (index < chainAttackScrollRect.content.childCount)
            {
                skillObj = chainAttackScrollRect.content.GetChild(index).gameObject;
            }
            else
            {
                skillObj = Instantiate(skillPrefab, chainAttackScrollRect.content);
            }

            foreach (Transform child in skillObj.transform.GetChild(1))
            {
                int siblingIndex = child.GetSiblingIndex();
                child.gameObject.SetActive(chainAttack.GetSkillIndex() == siblingIndex);
            }

            skillObj.transform.GetChild(0).gameObject.SetActive(true);
            skillObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = chainAttack.skillName;
            skillObj.transform.GetChild(3).gameObject.SetActive(false);
        }

        chainAttackSetup.SetActive(true);
        UpdateSelectedChainAttack(0);
    }

    private void UpdateSelectedChainReceiver(int indexChange)
    {
        if(indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.TabForward);

        CombatFunctions.UpdateListIndex(indexChange, selectedChainReceiverIndex, out selectedChainReceiverIndex, validChainPartners.Count);

        selectedChainReceiver = validChainPartners[selectedChainReceiverIndex];

        int childIndex = playerParty.IndexOf(selectedChainReceiver);

        foreach (Transform child in potraitHeader)
        {
            bool isSelected = child.GetSiblingIndex() == childIndex;
            float finalScale = isSelected ? 1.2f : 1f;

            child.DOScale(finalScale, selectedPotraitAnimTime);
        }

        DisplayChainAttackUI();
    }

    private void UpdateSelectedChainAttack(int indexChange)
    {
        if (indexChange != 0 && chainReceiverChainAttacks.Count == 1) { return; } //Don't bother updating with 1 item.

        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        CombatFunctions.UpdateListIndex(indexChange, selectedChainAttackIndex, out selectedChainAttackIndex, chainReceiverChainAttacks.Count);

        selectedChainAttack = chainReceiverChainAttacks[selectedChainAttackIndex];

        //Update Selected Attack.
        foreach (Transform child in chainAttackScrollRect.content)
        {
            int siblingIndex = child.GetSiblingIndex();
            child.GetChild(0).gameObject.SetActive(selectedChainAttackIndex == siblingIndex);
        }

        //UPDATE AOE DIAGRAM
        foreach(Transform child in skillAOEDiagramHeader)
        {
            Destroy(child.gameObject);
        }
        
        Instantiate(selectedChainAttack.aoeDiagram, skillAOEDiagramHeader);

        //Update Description
        skillDescription.text = selectedChainAttack.description;
        skillQuickData.text = selectedChainAttack.quickData;
    }

    private void SetPotraits()
    {
        playerParty = PartyManager.Instance.GetActivePlayerParty();

        foreach (PlayerGridUnit player in playerParty)
        {
            int index = playerParty.IndexOf(player);
            Transform potrait = potraitHeader.GetChild(index);

            potrait.GetComponent<Image>().sprite = player.portrait;

            Transform tintHeader = potrait.GetChild(0);
            bool activateUnavailableTint = false;


            foreach (Transform child in potrait.GetChild(0))
            {
                child.gameObject.SetActive(false);
            }

            if (player.CharacterHealth().isKOed)
            {
                activateUnavailableTint = true;
                tintHeader.GetChild(2).gameObject.SetActive(true);
            }
            else if (unitsAlreadyTriggeredChainAttack.Contains(player))
            {
                activateUnavailableTint = true;
                tintHeader.GetChild(0).gameObject.SetActive(true);
            }
            else if (!validChainPartners.Contains(player))
            {
                activateUnavailableTint = true;
                tintHeader.GetChild(1).gameObject.SetActive(true);
            }

            tintHeader.gameObject.SetActive(activateUnavailableTint);
        }

        UpdateSelectedChainReceiver(0);
    }


    //OTHER
    public void OnEventCancelled()
    {
        Debug.Log("Knockdown event cancelled");
        canTriggerChainAttack = false;
        ResetData();

        knockdownText.Fade(false);
        knockdownSpeedlines.Fade(false);
    }


    public List<Type> GetEventTypesThatCancelThis()
    {
        return otherEventTypesThatCancelThis;
    }


    protected override void ResetData()
    {
        base.ResetData();
        alreadySubscribedToEvent = false;
        currentAttacker = null;
        unitsAlreadyTriggeredChainAttack.Clear();
        selectedChainAttack = null;
        selectedChainReceiver = null;
    }

     private void EndAttackSelection(bool cancelledViaPlayerInput)
     {
        MMTimeManager.Instance.SetTimeScaleTo(1);

        ListenToChainAttackInput(false);
        chainAttackSetup.SetActive(false);

        if (cancelledViaPlayerInput)
            EndAction();
     }

     private void OnDisable()
     {
         UnitKnockdown -= OnUnitKnockedDown;
     }

     private IEnumerator EventSelectionCountdown()
     {
        yield return new WaitForSeconds(chainSelectionTimeInSecs);
        //If Timer Exhausted call Action Complete.
        EndEventSelection();
        EndAction();
     }

     private void EndEventSelection()
     {
         ActivateKnockdownUI(false, true);

         ListenToChainSelectionInput(false);
     }

    public void Flash()
    {
        flasher.Fade(true);
    }

    private bool HasKnockdownEnemies()
    {
        foreach (CharacterGridUnit enemy in FantasyCombatManager.Instance.GetEnemyCombatParticipants(false, true))
        {
            //Only Target knocked Down Units.
            if (StatusEffectManager.Instance.IsUnitKnockedDown(enemy))
            {
                return true;
            }
        }

        return false;
    }

    public float GetTurnEndEventOrder()
    {
        return transform.GetSiblingIndex();
    }

    //Input

    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU")
            {
                UpdateSelectedChainAttack(-1);
            }
            else if (context.action.name == "ScrollD")
            {
                UpdateSelectedChainAttack(1);
            }
        }
    }

    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "CycleR")
            {
                UpdateSelectedChainReceiver(1);
            }
            else if (context.action.name == "CycleL")
            {
                UpdateSelectedChainReceiver(-1);
            }
        }
    }


    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed && selectedChainAttack)
        {
            SelectChainAttack();
        }
    }

    private void OnBeatdown(InputAction.CallbackContext context)
    {
        if (context.action.name != "Beatdown") { return; }

        if (context.performed && !StoryManager.Instance.isTutorialPlaying)
        {
            if (!CanActivatePOF() && HasKnockdownEnemies())
            {
                PlayerBeatdownSelected();
            }
        }
    }

    private void OnActivateChainEvent(InputAction.CallbackContext context)
    {
        if (context.action.name != "Chain") { return; }

        if (context.performed && !StoryManager.Instance.isTutorialPlaying)
        {
            if(CanActivatePOF())
            {
                ActivatePOF();
            }
            else if(canTriggerChainAttack)
            {
                ChainSelected();
            }
        }
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "CancelCA") { return; }

        if (context.performed)
        {
            EndAttackSelection(true);
        }
    }



    private void ListenToChainSelectionInput(bool listen)
    {
        if (listen)
        {
            //Subscribe
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnActivateChainEvent;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnBeatdown;
        }
        else
        {
            //Unsubscribe
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnActivateChainEvent;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnBeatdown;
        }
    }

    private void ListenToChainAttackInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCycle;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
        }
    }

    protected override bool ListenForUnitHealthUIComplete()
    {
        return false;
    }

    /*private void OnMoveMouse(InputAction.CallbackContext context)
    {
        if (context.action.name != "Hover") { return; }

        Vector2 delta = context.ReadValue<Vector2>();

        if (context.performed && (Mathf.Abs(delta.x) >= mouseMinDeltaToDetectMovement || Mathf.Abs(delta.y) >= mouseMinDeltaToDetectMovement))
        {
            moveMouseDirection = delta.normalized;
            CalculateMousePos();
        } 
    }*/


    /*private void CalculateMousePos()
{
    //Don't Update when no mouse input.
    if(moveMouseDirection == Vector2.zero) { return; }

    float angleOfWheelSegment = 360 * wheelPieceFillAmount;
    float mouseAngle = Vector2.Angle(Vector2.up, moveMouseDirection); //Only returns a value from 0 to 180.

    //If Mouse moves to left of Vector2.Up subtract it from 360 to get a value greater than 180.
    if(moveMouseDirection.x < 0)
    {
        mouseAngle = 360 - mouseAngle;
    }

    int calculatedIndex = Mathf.FloorToInt(mouseAngle / angleOfWheelSegment);

    if(calculatedIndex >= chainAttackWheelHeader.childCount)
    {
        calculatedIndex = 0;
    }

    int index = chainAttackUnlockOrder.IndexOf(chainAttackWheelHeader.GetChild(calculatedIndex));

    if (index != selectedChainAttackIndex && index < currentChainAttacks.Count && currentChainAttacks[index].isUnlocked)
    {
        selectedChainAttackIndex = index;
        selectedChainAttack = currentChainAttacks[index];

        UpdateSelectedChainAttackUI();
    }
}*/

}
