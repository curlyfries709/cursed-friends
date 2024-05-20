using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public abstract class BasePlayableCard : CardComponent
{
    public Card cardData {  get; protected set; }
    public bool isPlayerCard { get; protected set; }

    protected CardUI UI;
    

    protected List<Card.AbilityData> orderedAbilities = new List<Card.AbilityData>();

    public abstract void OnPlay(CardGameManager.BattleRow selectedRow, bool didPlayerPlayCard);

    public virtual void Setup(Card myCard, bool isPlayerCard) 
    {
        cardData = myCard;
        UI = GetComponent<CardUI>();
        this.isPlayerCard = isPlayerCard;

        if(UI)
            UI.SetBaseCardData(cardData);
    }

    protected void CardPlayed()
    {
        CardGameManager.Instance.CardPlayed?.Invoke(this);
        //Trigger Abilities.
        PrepareDeployAbilities();
    }

    protected void PrepareDeployAbilities()
    {
        //Set Ability List
        orderedAbilities.Clear();

        foreach(Card.AbilityData abilityData in cardData.abilityData)
        {
            if(!abilityData.ability.GetComponent<BaseCardAbility>().IsTurnEndAbility())
                orderedAbilities.Add(abilityData);
        }

        //Order Abilities By Priority.
        if(orderedAbilities.Count > 0)
            orderedAbilities.OrderBy((ability) => ability.ability.GetComponent<BaseCardAbility>().Priority());

        //Subscribe to Event
        CardGameManager.Instance.AbilityCompleted += TriggerDeployAbility;

        //Trigger Abilities
        TriggerDeployAbility();
    }

    public void TriggerDeployAbility()
    {
        if(orderedAbilities.Count > 0)
        {
            //Trigger Ability
            Card.AbilityData abilityData = orderedAbilities[0];

            //Remove first item from ordered abilities list.
            orderedAbilities.RemoveAt(0);

            //Setup Ability
            BaseCardAbility ability = abilityData.ability.GetComponent<BaseCardAbility>();
            ability.OnSpawn(this, isPlayerCard, abilityData.abilityVariable);

            //Trigger Ability
            ability.OnTrigger();
        }
        else
        {
            //Unsubscribe to Event
            CardGameManager.Instance.AbilityCompleted -= TriggerDeployAbility;

            if (cardData.cardType == Card.CardType.Spell || cardData.cardType == Card.CardType.Weather)
            {
                //Add Card to Graveyard
                CardGameManager.Instance.AddToGraveyard(cardData, isPlayerCard);
            }

            //Notifiy Manager that all Abilities are complete.
            CardGameManager.Instance.DeployAbilitiesComplete();
        }
    }



}
