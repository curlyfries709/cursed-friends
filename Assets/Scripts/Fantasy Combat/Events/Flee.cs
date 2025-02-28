using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using AnotherRealm;
using UnityEngine.InputSystem;
using TMPro;

public class Flee : MonoBehaviour, IControls
{
    [Header("Camera")]
    [SerializeField] GameObject mainFleeCam;
    [Header("Value")]
    [SerializeField] float invalidMessageDisplayTime = 0.5f;
    [Space(5)]
    [SerializeField] int minPathDistanceFromEnemiesToFlee = 3;
    [Space(5)]
    [SerializeField] Vector3 jumpOffset = new Vector3(6, 0, 8);
    [SerializeField] float jumpHeight = 2;
    [SerializeField] float jumpTime = 0.5f;
    [Header("UI")]
    [SerializeField] FadeUI fader;
    [Space(10)]
    [SerializeField] GameObject confirmFleeUI;
    [SerializeField] FadeUI invalidFleeUI;
    [SerializeField] TextMeshProUGUI invalidFleeText;
    [Space(10)]
    [SerializeField] List<FleeInvalidMessage> fleeInvalidMessages;

    //Event 
    public static Action<CharacterGridUnit> UnitFled;


    PlayerGridUnit activeUnit;
    PlayerInput playerInput;

    bool fleeing = false;
    bool messageRoutinePlaying = false;

    public enum FleeResult
    {
        TooClose,
        LeaderCannotFlee,
        InMultipleCells,
        NonFleeBattle,
        CanFlee
    }

    [System.Serializable]
    public class FleeInvalidMessage
    {
        public FleeResult reason;
        [TextArea(2, 5)]
        public string message;
    }

    private void Awake()
    {
        playerInput = ControlsManager.Instance.GetPlayerInput();
        ControlsManager.Instance.SubscribeToPlayerInput("Menu", this);
    }

    public void TryFlee(PlayerGridUnit player)
    {
        FleeResult result = CanFlee(player);
        bool canFlee = result == FleeResult.CanFlee;

        if (canFlee)
        {
            //Play SFX
            AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

            ConfirmFlee(player);
        }
        else
        {
            //Update UI & Show Reason.
            string message = fleeInvalidMessages.Find((item) => item.reason == result).message;
            invalidFleeText.text = message;

            if(!messageRoutinePlaying)
                StartCoroutine(InvalidMessageRoutine());
        }
    }

    private void OnFlee()
    {
        if (fleeing) { return; }

        fleeing = true;
        ControlsManager.Instance.DisableControls();

        confirmFleeUI.SetActive(false);
        activeUnit.fleeCam.SetActive(false);
        mainFleeCam.SetActive(true);

        Vector3 destination = activeUnit.transform.position + (-activeUnit.transform.forward * jumpOffset.z) + (activeUnit.transform.right * jumpOffset.x);

        //Move Unit Out Camera //On Move Complete Switch To Hidden State
        activeUnit.transform.DOJump(destination, jumpHeight, 1, jumpTime).OnComplete(FleeComplete);
    }

    private void FleeComplete()
    {
        fleeing = false;

        ActivateConfirmFleeUI(false, true);
        activeUnit.ActivateUnit(false);
        mainFleeCam.SetActive(false);

        UnitFled.Invoke(activeUnit);
    }

    public void OnLastPlayerFled()
    {
        Debug.Log("LAST PLAYER FLED CALLED");
        ControlsManager.Instance.DisableControls();
        StartCoroutine(FledRoutine());
    }

    IEnumerator InvalidMessageRoutine()
    {
        //Play SFX
        AudioManager.Instance.PlaySFX(SFXType.ActionDenied);

        messageRoutinePlaying = true;
        invalidFleeUI.Fade(true);
        yield return new WaitForSeconds(invalidMessageDisplayTime);
        invalidFleeUI.Fade(false);
        messageRoutinePlaying = false;
    }

    IEnumerator FledRoutine()
    {
        ControlsManager.Instance.DisableControls();
        fader.Fade(true);
        yield return new WaitForSeconds(fader.fadeInTime);

        ActivateConfirmFleeUI(false, false);
        GridSystemVisual.Instance.HideAllGridVisuals();

        //Activate All Players
        foreach (PlayerGridUnit player in PartyManager.Instance.GetActivePlayerParty())
        {
            player.CharacterHealth().BattleComplete(); //Restore Any KOED Units to 1 Health.
            player.ActivateUnit(true);
            player.unitAnimator.ResetAnimatorToRoamState();
        }

        //Restore Surviving Units
        foreach (GridUnit unit in FantasyCombatManager.Instance.GetAllCombatUnits(false))
        {
            if(unit is PlayerGridUnit) { continue; }

            unit.Health().ResetVitals();

            if(unit.TryGetComponent(out EnemyStateMachine stateMachine))
            {
                stateMachine.WarpBackToPatrol();
            }  
        }

        //End Combat
        FantasyCombatManager.Instance.CombatEnded?.Invoke(BattleResult.Fled, FantasyCombatManager.Instance.battleTrigger);

        //Switch Music
        AudioManager.Instance.PlayMusic(MusicType.Roam);

        //Warp Player & Allies. 
        PlayerStateMachine playerStateMachine = PlayerSpawnerManager.Instance.GetPlayerStateMachine();
        FantasyCombatManager.Instance.WarpPlayerToPostCombatPos(playerStateMachine);

        fader.Fade(false);

        yield return new WaitForSeconds(fader.fadeOutTime);
        ControlsManager.Instance.SwitchCurrentActionMap("Player");
    }


    private FleeResult CanFlee(PlayerGridUnit player)
    {
        //Is Battle Fleeable
        IBattleTrigger battleTrigger = FantasyCombatManager.Instance.battleTrigger;

        if (battleTrigger.battleType == BattleType.Story || battleTrigger.battleType == BattleType.MonsterChest)
        {
            return FleeResult.NonFleeBattle;
        }

        //Check if Leader
        if (PartyManager.Instance.GetLeader() == player && FantasyCombatManager.Instance.GetPlayerCombatParticipants(false, true).Count > 1)
        {
            return FleeResult.LeaderCannotFlee;
        }

        //Check if in good position
        if (CombatFunctions.IsUnitStandingInMoreCellsThanNeccesary(player))
        {
            return FleeResult.InMultipleCells;
        }

        GridUnit closestEnemy = CombatFunctions.GetClosestUnitOfTypeOrDefault(player, FantasyCombatTarget.Enemy);

        //Check Distance
        if(PathFinding.Instance.GetPathLengthInGridUnits(player.GetCurrentGridPositions()[0], closestEnemy.GetGridPositionsOnTurnStart()[0], player) < minPathDistanceFromEnemiesToFlee)
        {
            return FleeResult.TooClose;
        }

        return FleeResult.CanFlee;
    }

    private void ConfirmFlee(PlayerGridUnit currentPlayer)
    {
        ControlsManager.Instance.SwitchCurrentActionMap(this);
        activeUnit = currentPlayer;

        ActivateConfirmFleeUI(true, false);
    }

    private void CancelFlee()
    {
        //Play SFX
        AudioManager.Instance.PlaySFX(SFXType.TabBack);

        ActivateConfirmFleeUI(false, true);
        ControlsManager.Instance.SwitchCurrentActionMap("FantasyCombat");
    }

    private void ActivateConfirmFleeUI(bool activate, bool showHUD)
    {
        activeUnit.fleeCam.SetActive(activate);
        confirmFleeUI.SetActive(activate);

        FantasyCombatManager.Instance.ShowActionMenu(showHUD);
        FantasyCombatManager.Instance.ShowHUD(showHUD, showHUD);
    }

    //Input

    private void OnConfirmFlee(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            OnFlee();
        }
    }


    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed)
        {
            CancelFlee();
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            playerInput.onActionTriggered += OnConfirmFlee;
            playerInput.onActionTriggered += OnCancel;
        }
        else
        {
            playerInput.onActionTriggered -= OnConfirmFlee;
            playerInput.onActionTriggered -= OnCancel;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }
}
