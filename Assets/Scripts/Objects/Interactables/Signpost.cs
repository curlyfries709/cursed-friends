using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Signpost : Interact, IControls
{
    [Header("Signpost")]
    [SerializeField] GameObject signpostCam;
    [SerializeField] FadeUI signpostCanvas;
    [Space(10)]
    [SerializeField] Transform controlsHeader;

    private void Awake()
    {
        ControlsManager.Instance.AddControlHeader(controlsHeader);
    }

    private void OnEnable()
    {
        ControlsManager.Instance.SubscribeToPlayerInput("Read", this);
    }


    public override void HandleInteraction(bool inCombat)
    {
        if (inCombat) { return; }

        AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);
        ControlsManager.Instance.SwitchCurrentActionMap(this);
        ActivateCanvas(true);
    }

    private void ActivateCanvas(bool activate)
    {
        if(activate)
            HUDManager.Instance.HideHUDs();

        signpostCam.SetActive(activate);
        signpostCanvas.Fade(activate);
    }

    private void StopReading()
    {
        AudioManager.Instance.PlaySFX(SFXType.TabBack);
        ActivateCanvas(false);
        ControlsManager.Instance.RevertToPreviousControls();
    }


    //INput 

    private void OnExit(InputAction.CallbackContext context)
    {
        if (context.action.name != "Exit") { return; }

        if (context.performed)
        {
            StopReading();
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnExit;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnExit;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }
}
