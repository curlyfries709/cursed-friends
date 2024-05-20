using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BaseCardAbility : MonoBehaviour
{
    [Header("Ability Data")]
    [Tooltip("Used my deploy abilities to sequence them correctly. Note: Turn End Abilities always sequence from Top To Bottom. Left To Right.")]
    [SerializeField]
    [Range(1, 99)] int priority = 1;
    [SerializeField] bool isTurnEndAbility;
    [Space(10)]
    [Tooltip("This is necessary for Opponent AI as it must know what the ability does to determine who to select when selecting")]
    public AbilityType abilityType;
    [Header("Selection Data")]
    public bool canTargetAllies;
    public bool canTargetFoes;

    public BasePlayableCard myPlayableCard { get; private set; }

    public FieldCard myFieldCard { get; private set; }
    public bool isPlayerCard { get; private set; }

    protected int configurableVariable;

    bool subscribedToTurnStartEvent = false;

    public enum AbilityType
    {
        Damage,
        Heal,
        Empower,
        Summon,
        Unity
    }

    //Abstract Methods
    public abstract void OnTrigger();

    protected abstract bool CanTriggerAbility();

    public abstract bool IsTargetValid(BasePlayableCard target);


    //Methods

    public void OnSpawn(BasePlayableCard playableCard, bool isPlayerCard, int configurableVariable)
    {
        this.configurableVariable = configurableVariable;
        this.isPlayerCard = isPlayerCard;
        myPlayableCard = playableCard;

        myFieldCard = myPlayableCard as FieldCard;

        if (isTurnEndAbility)
        {
            //Subscribe to Events
            SubscribeToTurnStartEvents(true);
            
            //Add Ability to queue on Spawn as this is called after Turn Start.
            OnTurnStart(CardGameManager.Instance.IsPlayerTurn());
        }    
    }


    private void OnTurnStart(bool isPlayerTurn)
    {
        //This Method only gets called for TurnEndAbility so validationon that isn't required.

        //Only wanna add Ability on Start of Owner's turn.
        if (isPlayerTurn == isPlayerCard)
            CardGameManager.Instance.QueueTurnEndAbility(this);
    }

    private void OnFieldCardDeath()
    {
        Debug.Log("Ability Field Card Died");
        CardGameManager.Instance.RemoveTurnEndAbiltyFromQueue(this);

        //Unsubscribe from Events.
        SubscribeToTurnStartEvents(false);

        //Destroy object.
        Destroy(gameObject);
    }


    private void OnDestroy()
    {
        if(isTurnEndAbility && subscribedToTurnStartEvent)
        {
            SubscribeToTurnStartEvents(false);
        }
    }
    //HELPERS

    protected bool IsValidFieldCard(BasePlayableCard target)
    {
        FieldCard fieldcard = target as FieldCard;

        if (!fieldcard) { return false; }

        //Check if Target friend or foe
        bool isAlly = fieldcard.isPlayerCard == isPlayerCard;

        return isAlly ? canTargetAllies : canTargetFoes;
    }


    private void SubscribeToTurnStartEvents(bool subscribe)
    {
        subscribedToTurnStartEvent = subscribe;

        if (subscribe)
        {
            CardGameManager.Instance.TurnStarted += OnTurnStart;
            myFieldCard.OnDeath += OnFieldCardDeath;
        }
        else
        {
            CardGameManager.Instance.TurnStarted -= OnTurnStart;
            myFieldCard.OnDeath -= OnFieldCardDeath;
        }
    }

    //GETTERS
    public bool IsTurnEndAbility()
    {
        return isTurnEndAbility;
    }

    public int Priority()
    {
        return priority;
    }

    public List<FieldCard> GetValidTargets()
    {
        List<FieldCard> validTargets = new List<FieldCard>();

        if (canTargetAllies)
        {
            var newList = CardGameManager.Instance.GetAllBattlefieldCards().Where((card) => card.isPlayerCard == isPlayerCard).ToList();
            validTargets = validTargets.Concat(newList).ToList();
        }

        if (canTargetFoes)
        {
            var newList = CardGameManager.Instance.GetAllBattlefieldCards().Where((card) => card.isPlayerCard != isPlayerCard).ToList();
            validTargets = validTargets.Concat(newList).ToList();
        }

        //Remove Self from list
        if(myFieldCard)
            validTargets.Remove(myFieldCard);

        return validTargets;
    }

}
