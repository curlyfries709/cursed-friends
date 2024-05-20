using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CardDataManager : MonoBehaviour
{
    public static CardDataManager Instance { get; private set; }

    //Components
    [SerializeField] PlayerDeckBuilderUI deckBuilderUI;

    //Storage
    Dictionary<Card.Faction, List<Card>> factionDecks = new Dictionary<Card.Faction, List<Card>>();
    Card.Faction lastPlayedFaction;

    //Data to Send
    CardOpponentData currentOpponentData;
    Card.Faction playerSelectedFaction;
    List<Card> playerSelectedDeck;

    //Variables
    AsyncOperation currentAsyncOperation;
    bool isPlayerReady = false;
    bool isPlayerWaiting = false;

    const string cardSceneName = "Card";

    private void Awake()
    {
        Instance = this;

        InitializeDecks();
    }
    public void ChallengeOpponent(CardOpponentData opponentData)
    {
        currentOpponentData = opponentData;

        //Activate deck Builder UI
        deckBuilderUI.ActivateUI(true);

        //Begin Loading Async
        StartCoroutine(LoadCardSceneRoutine());
    }

    public void OnPlayerReady(Card.Faction playerFaction)
    {
        lastPlayedFaction = playerFaction;
        playerSelectedFaction = playerFaction;

        playerSelectedDeck = factionDecks[playerFaction];

        isPlayerReady = true;

        //inform player opponent getting ready
        deckBuilderUI.ActivateOpponentReadyingUI(true);

        //Allow Scene Activation
        currentAsyncOperation.allowSceneActivation = true;
    }
    private void BeginGame()
    {
        //SceneManager.SetActiveScene(SceneManager.GetSceneByName(cardSceneName));
        //Setup Data:
        CardGameManager.Instance.SetGameData(playerSelectedFaction, playerSelectedDeck, currentOpponentData);

        //Hide UI
        deckBuilderUI.ActivateUI(false);

        //Start Game
        CardGameManager.Instance.BeginGame();

        isPlayerReady = false;
        isPlayerWaiting = false;

        currentAsyncOperation = null;
    }

    //LOAD CARD SCENE
    IEnumerator LoadCardSceneRoutine()
    {
        currentAsyncOperation = SceneManager.LoadSceneAsync(cardSceneName, LoadSceneMode.Additive);

        //Don't let the Scene activate until you allow it to
        currentAsyncOperation.allowSceneActivation = false;

        yield return currentAsyncOperation;

        //Begin Game when complete
        BeginGame();
    }

    //EDIT METHODS
    public void AddCardToDeck(Card.Faction faction, Card card)
    {
        List<Card> deck = factionDecks[faction];
        deck.Add(card);

        //Update UI
        deckBuilderUI.UpdateUI();
    }

    public void RemoveCardFromDeck(Card.Faction faction, Card card)
    {
        List<Card> deck = factionDecks[faction];
        deck.Remove(card);

        //Update UI
        deckBuilderUI.UpdateUI();
    }

    public void SwapCardInDeck(Card.Faction faction, Card cardToReplace, Card newCard)
    {
        List<Card> deck = factionDecks[faction];

        int index = deck.IndexOf(cardToReplace);

        deck.Remove(cardToReplace);
        deck.Insert(index, newCard);

        //Update UI
        deckBuilderUI.UpdateUI();
    }

    private void InitializeDecks()
    {
        foreach(int faction in Enum.GetValues(typeof(Card.Faction)))
        {
            if((Card.Faction)faction == Card.Faction.Neutral) { continue; }

            factionDecks[(Card.Faction)faction] = new List<Card>();
        }
    }

}
