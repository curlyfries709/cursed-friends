using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDeckBuilderUI : MonoBehaviour, IControls
{
    [Header("Deck Restrictions")]
    [SerializeField] int minCardsInDeck = 3;
    [SerializeField] int maxCardsIndeck = 20;
    [Space(10)]
    [SerializeField] int maxBronzeCopies = 3;
    [SerializeField] int maxGoldCopies = 1;
    [Header("Componets")]
    [SerializeField] GameObject opponentReadyingArea;
    [SerializeField] GameObject confirmText;
    [Header("TEST")]
    [SerializeField] List<Card> playerTestDeck = new List<Card>();


    Card.Faction currentFaction = Card.Faction.Werewolves;
    private void Awake()
    {
        ControlsManager.Instance.SubscribeToPlayerInput("Menu", this);
    }

    private void PlayerReady()
    {
        CardDataManager.Instance.OnPlayerReady(currentFaction);
    }

    public void ActivateUI(bool activate)
    {
        ActivateOpponentReadyingUI(false);
        confirmText.SetActive(true);

        if (activate)
        {
            //Grab Data from Card Data Manager

            //TEMP
            SetTestData();
        }

        //PhoneMenu.Instance.OpenApp(activate);
        gameObject.SetActive(activate);

        if (activate)
            ControlsManager.Instance.SwitchCurrentActionMap(this);
    }


    public void ActivateOpponentReadyingUI(bool activate)
    {
        confirmText.SetActive(!activate);
        opponentReadyingArea.SetActive(activate);
    }

    private void SetTestData()
    {
        foreach(Card card in playerTestDeck)
        {
            CardDataManager.Instance.AddCardToDeck(currentFaction, card);
        }
    }

    //UI

    public void UpdateUI()
    {

    }

    private void RotateFaction()
    {
        //Update Current Faction. 
    }


    //Controls
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

    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed)
        {

        }
    }

    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            PlayerReady();
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }
}
