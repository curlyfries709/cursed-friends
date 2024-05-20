using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandCard : BasePlayableCard
{
    protected int opponentCardIndex; //THIS IS SET BY OPPONENT AI! NEVER USED BY PLAYER!

    public override void OnHover()
    {
        SetTooltip();
        //Also Enlarge card in hand.
    }

    public override void OnSelect()
    {
        Debug.Log(cardData.itemName + " at index " + transform.GetSiblingIndex().ToString() + " selected!");
        CardGameManager.Instance.OnPlayableCardSelect(this);
    }

    public override void OnPlay(CardGameManager.BattleRow selectedRow, bool didPlayerPlayCard)
    {
        //Remove Card From Hand
        int index = didPlayerPlayCard ? transform.GetSiblingIndex() : opponentCardIndex;
        CardGameManager.Instance.CardPlayedFromHand(index);

        if (didPlayerPlayCard)
        {
            gameObject.SetActive(false);
            //This is to ensure transform indices match list indices in Card Manager by moving deactivated cards to bottom of list.
            transform.SetAsLastSibling();
        }

        if (cardData.cardType == Card.CardType.Unit)
        {
            //Grab A field Card from the row 
            FieldCard fieldCard = CardGameManager.Instance.GetFieldCardFromRow(selectedRow);

            //Just cos player played the card does not mean they own the card due to spy cards.
            bool isPlayerCard = selectedRow == CardGameManager.BattleRow.PlayerRanged || selectedRow == CardGameManager.BattleRow.PlayerMelee;

            fieldCard.Setup(cardData, isPlayerCard);
            fieldCard.OnPlay(selectedRow, didPlayerPlayCard);
        }
        else
        {
            CardPlayed();
        }
    }
    

    public void SetOpponentCardIndex(int index)
    {
        opponentCardIndex = index;
    }
}
