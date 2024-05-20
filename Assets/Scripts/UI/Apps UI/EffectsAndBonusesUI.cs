using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using AnotherRealm;

public class EffectsAndBonusesUI : MonoBehaviour, IControls
{
    [Header("Areas")]
    [SerializeField] GameObject effectsArea;
    [SerializeField] GameObject bonusArea;
    [Header("Scroll Rect")]
    [SerializeField] ScrollRect effectsScrollRect;
    [SerializeField] ScrollRect bonusesScrollRect;
    [Header("Controls")]
    [SerializeField] List<Transform> controlHeaders;

    bool scrolling = false;
    bool scrollUp = false;

    ScrollRect activeScrollRect;

    private void Awake()
    {
        foreach (Transform header in controlHeaders)
        {
            ControlsManager.Instance.AddControlHeader(header);
        }

        ControlsManager.Instance.SubscribeToPlayerInput("Menu", this);
    }

    public void ActivateUI(bool activate)
    {
        if (activate)
        {
            effectsArea.SetActive(true);
            bonusArea.SetActive(false);

            activeScrollRect = effectsScrollRect;
        }

        PhoneMenu.Instance.OpenApp(activate);
        gameObject.SetActive(activate);

        if (activate)
            ControlsManager.Instance.SwitchCurrentActionMap(this);
    }

    private void Update()
    {
        if (scrolling)
        {
            HandyFunctions.Scroll(activeScrollRect, scrollUp);
        }
    }


    private void UpdateActivateTab()
    {
        AudioManager.Instance.PlaySFX(SFXType.TabForward);

        effectsArea.SetActive(!effectsArea.activeInHierarchy);
        bonusArea.SetActive(!bonusArea.activeInHierarchy);

        activeScrollRect = bonusArea.activeInHierarchy ? bonusesScrollRect : effectsScrollRect;
    }


    //Input
    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "CycleR" || context.action.name == "CycleL")
            {
                UpdateActivateTab();
            }
        }
    }

    private void OnScroll(InputAction.CallbackContext context)
    {

        if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
        {
            scrollUp = context.action.name == "ScrollD" ? false : true;

            if (context.performed)
            {
                scrolling = true;
            }
            else if (context.canceled)
            {
                scrolling = false;
            }
        }  
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed)
        {
            ActivateUI(false);
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }
}
