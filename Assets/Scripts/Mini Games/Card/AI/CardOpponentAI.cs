using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using System.Linq;

public class CardOpponentAI : MonoBehaviour
{
    [SerializeField] HandCard handCardToPlay;

    CardOpponentData myOpponentData;

    //CARD LISTS
    List<Card> hand = new List<Card>();
    List<Card> deck = new List<Card>();
    List<Card> graveyard = new List<Card>();

    public void BeginTurn(List<Card> hand, List<Card> deck, List<Card> graveyard)
    {
        //Setup Data
        this.hand = hand;
        this.deck = deck;
        this.graveyard = graveyard;

        //Determine if should trigger blessing.
        if (TryTriggerBlessing())
        {
            return;
        }

        ChooseCardToPlay();
    }

    private void ChooseCardToPlay()
    {
        Debug.Log("Opponent Turn");

        //Pick Random Card from hand to play
        int randIndex = Random.Range(0, hand.Count);
        

        PlayCard(randIndex);
    }

    private void PlayCard(int cardHandIndex)
    {
        Card chosenCard = hand[cardHandIndex];
        //Choose Row
        CardGameManager.BattleRow randomRow = ChooseRowToPlay(chosenCard);

        //Setup Hand Card and Index.
        handCardToPlay.Setup(chosenCard, false);
        handCardToPlay.SetOpponentCardIndex(cardHandIndex);

        //Play Card
        handCardToPlay.OnPlay(randomRow, false);
    }

    private CardGameManager.BattleRow ChooseRowToPlay(Card chosenCard)
    {
        //Choose Random Row
        int randNum = chosenCard.isSpy ? Random.Range(2, 4) : Random.Range(0, 2);
        return (CardGameManager.BattleRow)randNum;
    }

    public void SelectAbilityTarget(BaseCardAbility ability)
    {
        List<FieldCard> validTargets = ability.GetValidTargets();

        if (ability.abilityType == BaseCardAbility.AbilityType.Damage)
        {
            //Priotise Enemy
            validTargets = validTargets
                .Where((card) => card.battleRow == CardGameManager.BattleRow.PlayerMelee || card.battleRow == CardGameManager.BattleRow.PlayerRanged).ToList();
  
        }
        else if(ability.abilityType == BaseCardAbility.AbilityType.Heal)
        {
            //Priotise Ally
            validTargets = validTargets
                .Where((card) => card.battleRow == CardGameManager.BattleRow.OpponentMelee || card.battleRow == CardGameManager.BattleRow.OpponentRanged).ToList();
        }

        //Pick RandomCard
        FieldCard randCard = validTargets[Random.Range(0, validTargets.Count)];

        //Select Card
        bool selectionSuccessful = CardGameManager.Instance.OnFieldCardSelect(randCard, false);

        if(!selectionSuccessful)
        {
            Debug.Log("OPPONENT AI CHOSE INVALID TARGET FOR ABILITY OF CARD: " + ability.myPlayableCard.cardData.itemName);
        }
    }

    private bool TryTriggerBlessing()
    {
        return false;
    }


    //GETTERS & SETTERS
    public void SetOpponentData(CardOpponentData cardOpponentData)
    {
        myOpponentData = cardOpponentData;
    }
}
