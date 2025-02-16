using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;
using System;

public class CardGameManager : MonoBehaviour, IControls
{
    public static CardGameManager Instance { get; private set; }

    [Header("Stats")]
    [SerializeField] int powerScoreToWin = 32;
    [Space(10)]
    [SerializeField] int maxHandCount = 8;
    [SerializeField] int maxMulligans = 2;
    [Space(5)]
    [Tooltip("Maximum cards to draw at the start of a new phase")]
    [SerializeField] int maxDrawsPerPhase = 4;
    [Header("Timers")]
    [Tooltip("The Player should be able to take in the new card they've drawn before mulligan ends")]
    [SerializeField] float delayAfterLastMulligan = 1.5f;
    [Header("Components")]
    [SerializeField] CardUIManager UIManager;
    [SerializeField] CardOpponentAI opponentAI;
    [Space(10)]
    [SerializeField] Transform cursor;
    [Space(10)]
    [SerializeField] Transform playerAbilityHeader;
    [SerializeField] Transform opponentAbilityHeader;

    //Variables
    CardComponent currentHoveredComponent = null;
    BasePlayableCard selectedPlayableCard = null;
    BaseCardAbility activeAbilitySelection = null;

    //Factions
    Card.Faction playerFaction;
    Card.Faction opponentFaction;

    //Hands
    List<Card> playerHand = new List<Card>();
    List<Card> opponentHand = new List<Card>();

    //DECKS
    List<Card> playerStartingDeck; //SET BY CARD DATA MANAGER 
    List<Card> opponentStartingDeck; //SET BY CARD DATA MANAGER 


    List<Card> playerCurrentDeck =  new List<Card>();
    List<Card> opponentCurrentDeck = new List<Card>();

    List<Card> mulliganedCards = new List<Card>();

    //Graveyards
    List<Card> playerGraveyard = new List<Card>();
    List<Card> opponentGraveyard = new List<Card>();

    //Battlefied
    List<FieldCard> battlefieldCards = new List<FieldCard>();

    //Queue
    List<BaseCardAbility> turnEndAbilityQueue = new List<BaseCardAbility>();

    //Bools
    bool isPlayerTurn = false;

    bool canSelect = false;
    bool isMulliganPhase = false;

    bool isConfirmingRow = false;
    bool isConfiming = false;
    bool isSelectingTarget = false;
    
    //Count & Scores
    int mulliganCount = 0;

    int playerScore = 0;
    int opponentScore = 0;

    //Events
    public Action<bool> TurnStarted; //Bool IsPlayerTurn
    public Action<BasePlayableCard> CardPlayed;
    
    public Action<bool, BattleRow, int> ScoreChange; //Bool IsPlayerTurn. BattleRow row of change, Int Score Change.
    public Action<BasePlayableCard> ValidTargetSelected;


    public Action AbilityCompleted;

    const string myActionMap = "Cards";

    public enum BattleRow
    {
        OpponentRanged,
        OpponentMelee,
        PlayerMelee,
        PlayerRanged
    }



    /* 
     
     PROCESS

    Challenge Opponent from external Scene. 
    Edit & Choose Deck (Handled by Card Data Manager)
    Detemine who goes first and display UI to player. 
    Mulligan Phase. 
    Begin Game.
     
     */

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }     
    }

    private void OnEnable()
    {
        ScoreChange += UpdateScore;
        ControlsManager.Instance.SubscribeToPlayerInput(myActionMap, this);
    }

    public void SetGameData(Card.Faction playerFaction, List<Card> playerDeck, CardOpponentData opponentData)
    {
        //Player Data
        this.playerFaction = playerFaction;
        playerStartingDeck = new List<Card>(playerDeck);

        //Opponent Data
        opponentFaction = opponentData.GetFaction();
        opponentStartingDeck = new List<Card>(opponentData.GetDeck());
    }


    public void BeginGame()
    {
        OnGameStart();
    }

    private void Start()
    {
        //OnGameStart();
    }

    //TUrns & Events
    private void OnGameStart()
    {
        //Switch Controls
        ControlsManager.Instance.SwitchCurrentActionMap(myActionMap);

        //Determine who goes first.
        isPlayerTurn = UnityEngine.Random.Range(0, 2) == 0;
        //isPlayerTurn = true;
        Debug.Log("Is Player Starting: " + isPlayerTurn.ToString());

        //Shuffle Player Deck
        playerCurrentDeck = new List<Card>(playerStartingDeck);
        playerCurrentDeck = ShuffleDeck(playerCurrentDeck);

        //Shuffle Opponent Deck
        opponentCurrentDeck = new List<Card>(opponentStartingDeck);
        opponentCurrentDeck = ShuffleDeck(opponentCurrentDeck);

        //Draw a cards until hand full for both.
        DrawCardUntilCapacity(maxHandCount, true);
        DrawCardUntilCapacity(maxHandCount, false);

        SetUIOnGameStart();

        //Allow Mulligan for player if deck not empty
        if (playerCurrentDeck.Count > 0)
        {
            BeginMulliganPhase();
        }
        else
        {
            UIManager.UpdatePlayerHand(playerHand);
            OnTurnStart();
        }
    }

    private void UpdateScore(bool isPlayer, BattleRow rowOfChange, int scoreChange)
    {
        //Called When
            //Card Killed.
            //Card Power Set
            //When Unit Played
            //Card Transformed
        
        if(isPlayer)
        {
            playerScore = playerScore + scoreChange;
        }
        else
        {
            opponentScore = opponentScore + scoreChange;
        }

        UIManager.UpdateRowScore(rowOfChange, scoreChange);
        UIManager.UpdateTotalScore(isPlayer, isPlayer ? playerScore : opponentScore);
    }

    private void OnTurnStart()
    {
        TurnStarted?.Invoke(isPlayerTurn);
        canSelect = isPlayerTurn;

        if (IsCurrentHandEmpty())
        {
            //If the current turn owner has no cards in hand, End their turn.
            Debug.Log("Hand Empty Skipping Turn. IsPlayer: " + isPlayerTurn);
            DeployAbilitiesComplete();
            return;
        }

        if (isPlayerTurn)
        {
            ControlsManager.Instance.SwitchCurrentActionMap(myActionMap);
            Debug.Log("Player Turn");
        }
        else
        {
            OnOpponentTurn();
        }
    }

    public void DeployAbilitiesComplete()
    {
        //Clear Current Card
        UpdateSelectedPlayable(null);

        //Order Turn End Queue.
        OrderTurnEndQueue();

        OnTurnEndAbilityComplete();
    }

    public void OnTurnEndAbilityComplete()
    {
        if (turnEndAbilityQueue.Count > 0)
        {
            //Trigger First turn ability in queue.
            BaseCardAbility abilityToTrigger = turnEndAbilityQueue[0];
            turnEndAbilityQueue.RemoveAt(0);
            abilityToTrigger.OnTrigger();
        }
        else
        {
            //If No turn end abilities, end turn.
            OnTurnEnd();
        }
    }


    private void OnTurnEnd()
    {
        //Do A Victory Check to see if current turn owner has won.
        if (HasCurrentTurnOwnerWon())
        {
            TriggerVictory(isPlayerTurn);
        }
        else if (CanTriggerTiebreaker()) //Tie Breaker Check
        {
            TriggerTiebreaker();
        }
        else if (CanTriggerNextPhase()) //Next Phase Check
        {
            TriggerNextPhase();
        }
        else //Continue current phase.
        {
            SwitchTurns();
        }
    }

    private void SwitchTurns()
    {
        //Once Complete, Switch turns.
        isPlayerTurn = !isPlayerTurn;
        OnTurnStart();
    }

    //AI 
    private void OnOpponentTurn()
    {
        //Disable controls. 
        ControlsManager.Instance.DisableControls();

        //Contact AI to begin reasoning
        opponentAI.BeginTurn(opponentHand, opponentCurrentDeck, opponentGraveyard);
    }

    //Selections
    public void OnPlayableCardSelect(BasePlayableCard playableCard)
    {
        if(isSelectingTarget) { return; }

        //Set the current card
        UpdateSelectedPlayable(playableCard);

        //For Blessings a different UI is required. 

        //If Unit or weather, begin row selection.
        if(playableCard.cardData.cardType == Card.CardType.Unit || playableCard.cardData.cardType == Card.CardType.Weather)
        {
            ActivateRowSelection(true);
        }
        else
        {
            isConfiming = true;
        }
    }

    public void BeginFieldSelection(BaseCardAbility ability)
    {
        Debug.Log("Beginning Field Selection");

        activeAbilitySelection = ability;

        if (isPlayerTurn)
        {
            EnableFieldCardSelection(true);
        }
        else
        {
            
            opponentAI.SelectAbilityTarget(ability);
        }
    }

    public void EnableFieldCardSelection(bool enable)
    {
        canSelect = enable;
        isSelectingTarget = enable;
    }

    public bool OnFieldCardSelect(FieldCard fieldCard, bool isPlayerSelecting)
    {
        if (isPlayerSelecting && !isSelectingTarget) { return false; }
        if (isPlayerSelecting && (isConfiming || isConfirmingRow)) { return false; }

        if (activeAbilitySelection.IsTargetValid(fieldCard))
        {
            Debug.Log("Valid Field Card Target Selected");

            //Disable Selection
            activeAbilitySelection = null;
            EnableFieldCardSelection(false);

            //Call Event & Begin Next Turn or Ability
            ValidTargetSelected?.Invoke(fieldCard);

            return true;
        }

        return false;
    }

    private void OnComfirmPlay()
    {
        if (!selectedPlayableCard || isSelectingTarget) { return; }

        BattleRow selectedRow = BattleRow.PlayerMelee;

        if (isConfirmingRow)
        {
            if(!currentHoveredComponent) { return; }

            //Grab & set Selected Row
            FieldCard fieldCard = currentHoveredComponent as FieldCard;
            BattlefieldRow battlefieldRow = currentHoveredComponent as BattlefieldRow;

            switch (currentHoveredComponent)
            {
                case FieldCard:
                    selectedRow = fieldCard.battleRow;
                    break;
                case BattlefieldRow:
                    selectedRow = battlefieldRow.GetBattleRow();
                    break;
                default:
                    //If You reach here, the component selected isn't valid
                    return;
            }

            //Check if Selected Row Valid
            bool isSpyCard = selectedPlayableCard.cardData.isSpy;

            //It will always be player turn is this Scenario as AI doesn't need to confirm.
            if(isSpyCard)
            {
                if (selectedRow == BattleRow.PlayerMelee || selectedRow == BattleRow.PlayerRanged)
                {
                    //Not Valid Selection. Can only play spy cards on Opponent side.
                    return;
                }
            }
            else
            {
                if (selectedRow == BattleRow.OpponentMelee || selectedRow == BattleRow.OpponentRanged)
                {
                    //Not Valid Selection. Can only play non-spy cards on your side.
                    return;
                }
            }

        }

        canSelect = false;

        ActivateRowSelection(false);
        isConfiming = false;

        //Play Card & Trigger Deploy abilities.
        selectedPlayableCard.OnPlay(selectedRow, true);
    }

    private void OnComponentConfirm()
    {
        if (!canSelect || currentHoveredComponent == null) return;
        if (!isPlayerTurn && !isMulliganPhase) return;

        //MORE CHECKS MAY BE REQUIRED
        currentHoveredComponent.OnSelect();
    }

    private void OnCancelSelection()
    {
        if (!selectedPlayableCard) return;

        UpdateSelectedPlayable(null);

        //Cancel Row Selection.
        ActivateRowSelection(false);
        isConfiming = false;
    }

    private void ActivateRowSelection(bool activate)
    {
        isConfirmingRow = activate;
        UIManager.ActivateRowSelection(activate);
    }
    //Actions
    private void TriggerNextPhase()
    {
        Debug.Log("Beggining Next Phase");
        //Display Phase UI.

        //Draw For Opponent
        DrawCardUntilCapacity(maxDrawsPerPhase, false);
        //Draw for Player
        DrawCardUntilCapacity(maxDrawsPerPhase, true);

        //Update UI
        UpdateHandCounts();
        UpdateDeckCounts();

        //Update Player Hand
        UIManager.UpdatePlayerHand(playerHand);

        //Begin New Turn
        SwitchTurns();
    }

    private void TriggerVictory(bool playerWon)
    {
        Debug.Log("Triggering Victory. Did Player win: " + playerWon);
    }

    private void TriggerTiebreaker()
    {
        bool playerWon = playerScore > opponentScore;
        bool isTie = playerScore == opponentScore;

        Debug.Log("Triggering Tiebreaker. Did Player win: " + playerWon + " Is it a tie: " + isTie);
    }

    public void CardPlayedFromHand(int cardIndex)
    {
        //Remove From Hand
        List<Card> hand = isPlayerTurn ? playerHand : opponentHand;

        hand.RemoveAt(cardIndex);

        //Update UI
        UIManager.UpdateHandCount(hand.Count, isPlayerTurn);
    }

    public void AddToGraveyard(Card card, bool isPlayerCard)
    {
        List<Card> graveyard = isPlayerCard ? playerGraveyard : opponentGraveyard;
        graveyard.Add(card);

        if (isPlayerCard)
        {
            UIManager.UpdatePlayerGraveyardCount(playerGraveyard.Count);
        }
    }

    public void SpawnAbility(Card.AbilityData abilityData, FieldCard fieldCard,  bool isPlayerCard)
    {
        Transform header = isPlayerCard ? playerAbilityHeader : opponentAbilityHeader;
        GameObject spawnedAbility = Instantiate(abilityData.ability, header);

        spawnedAbility.GetComponent<BaseCardAbility>().OnSpawn(fieldCard, isPlayerCard, abilityData.abilityVariable);

        //Could set Sibling index here to create order of execution of abilities.

    }

    //MULLIGAN PHASE
    private void BeginMulliganPhase()
    {
        canSelect = true;
        isMulliganPhase = true;
        mulliganCount = 0;

        ControlsManager.Instance.SwitchCurrentActionMap(myActionMap);
        UIManager.BeginMulliganPhase(playerHand, this);
    }

    private void EndMulliganPhase()
    {
        isMulliganPhase = false;
        UIManager.EndMulliganPhase();

        //Shuffle Mulliganed Cards into deck
        foreach(Card card in mulliganedCards)
        {
            int randomNum = UnityEngine.Random.Range(0, playerCurrentDeck.Count + 1);

            //Below Method allows old card to be shuffled to bottom of deck.
            if (randomNum > playerCurrentDeck.Count)
            {
                playerCurrentDeck.Add(card);
            }
            else
            {
                playerCurrentDeck.Insert(randomNum, card);
            }
        }

        mulliganedCards.Clear();
        UIManager.UpdatePlayerHand(playerHand);

        ControlsManager.Instance.SwitchCurrentActionMap(myActionMap);
        OnTurnStart();
    }

    public void MulliganCard(int cardIndex)
    {
        mulliganCount++;

        //Replace Card From hand.
        Card oldCard = playerHand[cardIndex];
        playerHand.RemoveAt(cardIndex);
        mulliganedCards.Add(oldCard);

        Card newCard = playerCurrentDeck[0];
        playerHand.Insert(cardIndex, newCard);
        playerCurrentDeck.RemoveAt(0);

        UIManager.CardRedrawn(cardIndex, newCard, mulliganCount);

        if(mulliganCount >= maxMulligans)
        {
            StartCoroutine(EndMulliganPhaseRoutine());
        }
    }

    IEnumerator EndMulliganPhaseRoutine()
    {
        ControlsManager.Instance.DisableControls();

        yield return new WaitForSeconds(delayAfterLastMulligan);

        EndMulliganPhase();
    }

    //Card Components
    public void OnCardComponentEnter(CardComponent component)
    {
        if(!isConfirmingRow && component is BattlefieldRow) { return; } 

        //Update Hovered Current Component.
        currentHoveredComponent = component;

        //Trigger On Hover Method.
        component.OnHover();
    }

    public void OnCardComponentExit(CardComponent component)
    {
        if(currentHoveredComponent == component)
        {
            //Reset it to null
            currentHoveredComponent = null;

            //Hide Tooltip
        }
    }

    //HELPER METHODS

    private void DrawCardUntilCapacity(int maxDrawCount, bool isPlayer)
    {
        List<Card> deck = isPlayer ? playerCurrentDeck : opponentCurrentDeck;
        List<Card> hand = isPlayer ? playerHand : opponentHand;

        int numOfCardsDrawn = 0;

        //Keep drawing until hand is full, or handcapacity reached or Deck is empty.
        while (hand.Count < maxHandCount && deck.Count > 0 && numOfCardsDrawn < maxDrawCount)
        {
            Card topDeck = deck[0];
            deck.RemoveAt(0);

            hand.Add(topDeck);

            numOfCardsDrawn++;
        }
    }

    private List<Card> ShuffleDeck(List<Card> deckToShuffle)
    {
        System.Random rand = new System.Random();

        return deckToShuffle.OrderBy((card) => rand.Next()).ToList();
    }

    public void BattlefieldCountChanged(FieldCard card, bool isDead)
    {
        if (isDead)
        {
            battlefieldCards.Remove(card);
        }
        else
        {
            battlefieldCards.Add(card);
        }
    }

    public void QueueTurnEndAbility(BaseCardAbility ability)
    {
        if (!turnEndAbilityQueue.Contains(ability))
            turnEndAbilityQueue.Add(ability);
    }
    public void RemoveTurnEndAbiltyFromQueue(BaseCardAbility ability)
    {
        turnEndAbilityQueue.Remove(ability);
    }
    
    private void OrderTurnEndQueue()
    {
        if(turnEndAbilityQueue.Count == 0) { return; }

        //Abilities ordered based on field postion. Top to Bottom. Left to Right.

        Debug.Log("Count Before Order: " + turnEndAbilityQueue.Count.ToString());

        List<BaseCardAbility> abilitiesOnOpponentRanged =  turnEndAbilityQueue.Where((ability) => ability.myFieldCard.battleRow == BattleRow.OpponentRanged).ToList();
        List<BaseCardAbility> abilitiesOnOpponentMelee = turnEndAbilityQueue.Where((ability) => ability.myFieldCard.battleRow == BattleRow.OpponentMelee).ToList();

        List<BaseCardAbility> abilitiesOnPlayerMelee = turnEndAbilityQueue.Where((ability) => ability.myFieldCard.battleRow == BattleRow.PlayerMelee).ToList();
        List<BaseCardAbility> abilitiesOnPlayerRanged = turnEndAbilityQueue.Where((ability) => ability.myFieldCard.battleRow == BattleRow.PlayerRanged).ToList();

        List<BaseCardAbility> opponentAbilities = abilitiesOnOpponentRanged.Concat(abilitiesOnOpponentMelee).ToList();
        List<BaseCardAbility> playerAbilities = abilitiesOnPlayerMelee.Concat(abilitiesOnPlayerRanged).ToList();

        turnEndAbilityQueue.Clear();
        turnEndAbilityQueue = opponentAbilities.Concat(playerAbilities).ToList();

        Debug.Log("Turn End Queue Ordered. Count: " + turnEndAbilityQueue.Count.ToString());
    }

    private bool HasCurrentTurnOwnerWon()
    {
        int score = isPlayerTurn ? playerScore : opponentScore;
        return score >= powerScoreToWin;
    }

    private bool CanTriggerNextPhase()
    {
        //If Neither player has cards in hand.
        return playerHand.Count + opponentHand.Count == 0;
    }

    private bool CanTriggerTiebreaker()
    {
        return playerHand.Count + opponentHand.Count + playerCurrentDeck.Count + opponentCurrentDeck.Count == 0;
    }

    private bool IsCurrentHandEmpty()
    {
        int cardsRemaining = isPlayerTurn ? playerHand.Count : opponentHand.Count;
        return cardsRemaining == 0;
    }

    private void SetUIOnGameStart()
    {
        //Set Opponent Cam, Name & Faction

        //Set Player Faction

        //Set Player & Opponent blessing.

        //Reset Scores
        UIManager.ResetScores();

        //Update Hand Count
        UpdateHandCounts();

        //Update Deck Count
        UpdateDeckCounts();

        //Update Player Graveyard
        UIManager.UpdatePlayerGraveyardCount(playerGraveyard.Count);

        //Clear Current Selected Card
        UIManager.SetCurrentSelectedCard(null);

        //Clear Battlefield UI
        UIManager.ClearBattlefield();

        //Disable Row Collider
        UIManager.ActivateRowSelection(false);

        //Hide Player Hand
        UIManager.ShowPlayerHand(false);
    }



    private void UpdateSelectedPlayable(BasePlayableCard selectedCard)
    {
        selectedPlayableCard = selectedCard;

        UIManager.SetCurrentSelectedCard(selectedPlayableCard ? selectedPlayableCard.cardData : null);
    }

    private void OnDisable()
    {
        ScoreChange -= UpdateScore;
    }

    //UI
    private void UpdateHandCounts()
    {
        UIManager.UpdateHandCount(playerHand.Count, true);
        UIManager.UpdateHandCount(opponentHand.Count, false);
    }

    private void UpdateDeckCounts()
    {
        UIManager.UpdateDeckCount(playerCurrentDeck.Count, true);
        UIManager.UpdateDeckCount(opponentCurrentDeck.Count, false);
    }

    //GETTERS & SETTERS
    public List<FieldCard> GetAllBattlefieldCards()
    {
        return battlefieldCards;
    }

    public int GetMaxMulligans()
    {
        return maxMulligans;
    }

    public FieldCard GetFieldCardFromRow(BattleRow row)
    {
        return UIManager.GetFieldCardFromRow(row);
    }

    public bool IsPlayerTurn()
    {
        return isPlayerTurn;
    }

    //Controls
    private void OnMoveCursor(InputAction.CallbackContext context)
    {
        if (context.action.name != "Hover") { return; }
        Vector2 inputMoveValue = context.ReadValue<Vector2>();

        //Move Cursor
        cursor.position += new Vector3(inputMoveValue.x, inputMoveValue.y, 0);

        //Clamp Cursor.
        float clampedXPos = Mathf.Clamp(cursor.position.x, Camera.main.ViewportToScreenPoint(Vector3.zero).x, Camera.main.ViewportToScreenPoint(Vector3.one).x);
        float clampedYPos = Mathf.Clamp(cursor.position.y, Camera.main.ViewportToScreenPoint(Vector3.zero).y, Camera.main.ViewportToScreenPoint(Vector3.one).y);

        cursor.position = new Vector3(clampedXPos, clampedYPos, 0);   
    }

    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            //Debug.Log("Current Hovered Component: " + currentHoveredComponent);
            if(isConfiming || isConfirmingRow)
            {
                OnComfirmPlay();
            }
            else
            {
                OnComponentConfirm();
            }
        }
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed)
        {
            if (isMulliganPhase)
            {
                EndMulliganPhase();
            }
            else
            {
                OnCancelSelection();
            }
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnMoveCursor;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnMoveCursor;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
        }
    }
}
