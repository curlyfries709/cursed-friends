using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System;

public class CardUIManager : MonoBehaviour
{
    [Header("Scores")]
    [SerializeField] TextMeshProUGUI playerScore;
    [SerializeField] TextMeshProUGUI opponentScore;
    [Space(10)]
    [SerializeField] List<RowScore> rowScores = new List<RowScore>(4);
    [Header("Player Hand")]
    [SerializeField] Transform playerHandHeader;
    [Header("Battlefield")]
    [SerializeField] Transform playerMeleeRow;
    [SerializeField] Transform playerRangedRow;
    [Space(10)]
    [SerializeField] Transform opponentMeleeRow;
    [SerializeField] Transform opponetRangedRow;
    [Header("Counts")]
    [SerializeField] TextMeshProUGUI playerHandCount;
    [SerializeField] TextMeshProUGUI opponentHandCount;
    [Space(5)]
    [SerializeField] TextMeshProUGUI playerDeckCount;
    [SerializeField] TextMeshProUGUI opponentDeckCount;
    [Space(5)]
    [SerializeField] TextMeshProUGUI playerGraveyardCount;
    [Header("Current Card Area")]
    [SerializeField] GameObject currentCardArea;
    [Space(5)]
    [SerializeField] CardUI currentCardUI;
    [Space(5)]
    [SerializeField] TextMeshProUGUI currentCardName;
    [SerializeField] TextMeshProUGUI currentCardDescription;
    [Header("Components")]
    [SerializeField] GameObject playerDeckUI;
    [SerializeField] GameObject opponentDeckUI;
    [Space(10)]
    [SerializeField] List<BoxCollider2D> rowColliders = new List<BoxCollider2D>(4);
    [Header("Mulligan UI")]
    [SerializeField] GameObject mulliganCanvas;
    [Space(5)]
    [SerializeField] Transform mulliganCardsHeader;
    [SerializeField] TextMeshProUGUI redrawsRemainingText;


    CardGameManager gameManager;

    [System.Serializable]
    public class RowScore
    {
        public CardGameManager.BattleRow battleRow;
        public TextMeshProUGUI rowScoreText;
    }

    // 

    //Player Hand

    public void UpdatePlayerHand(List<Card> hand)
    {
        ShowPlayerHand(true);

        foreach(Transform card in playerHandHeader)
        {
            int index = card.GetSiblingIndex();
            bool shouldActivate = index < hand.Count;

            card.gameObject.SetActive(shouldActivate);

            if(!shouldActivate) { continue; }

            //Set the Card Data.
            card.GetComponent<HandCard>().Setup(hand[index], true);
        }

        UpdateHandCount(hand.Count, true);
    }

    public void ShowPlayerHand(bool show)
    {
        playerHandHeader.gameObject.SetActive(show);
    }

    //Battlefield 

    public void ActivateRowSelection(bool activate)
    {
        foreach(BoxCollider2D collider in rowColliders)
        {
            collider.enabled = activate;
        }
    }

    public void ClearBattlefield()
    {
        List<Transform> battlefield = new List<Transform> { opponetRangedRow, opponentMeleeRow, playerMeleeRow, playerRangedRow };
        
        foreach(Transform row in battlefield)
        {
            foreach(Transform card in row)
            {
                card.gameObject.SetActive(false);
            }
        }
    }

    //HUD
    public void UpdateRowScore(CardGameManager.BattleRow row, int scoreChange)
    {
        //Get Current Score
        TextMeshProUGUI scoreToUpdate = rowScores.First((pair) => pair.battleRow == row).rowScoreText;
        int currentScore = Int32.Parse(scoreToUpdate.text);

        scoreToUpdate.text = (currentScore + scoreChange).ToString();
    }

    public void UpdateTotalScore(bool isPlayerScore, int newScore)
    {
        TextMeshProUGUI scoreToUpdate = isPlayerScore ? playerScore : opponentScore;
        scoreToUpdate.text = newScore.ToString();
    }

    public void SetCurrentSelectedCard(Card card)
    {
        currentCardArea.SetActive(card);

        if (card)
        {
            currentCardUI.SetBaseCardData(card);

            currentCardName.text = card.itemName;
            currentCardDescription.text = card.description;
        }
    }

    public void UpdateHandCount(int count, bool isPlayer)
    {
        TextMeshProUGUI textToUpdate = isPlayer ? playerHandCount : opponentHandCount;
        textToUpdate.text = count.ToString();
    }

    public void UpdateDeckCount(int count, bool isPlayer)
    {
        TextMeshProUGUI textToUpdate = isPlayer ? playerDeckCount : opponentDeckCount;
        textToUpdate.text = count.ToString();

        GameObject objToUpdate = isPlayer ? playerDeckUI : opponentDeckUI;
        objToUpdate.SetActive(count > 0);
    }

    public void UpdatePlayerGraveyardCount(int count)
    {
        playerGraveyardCount.text = count.ToString();
    }

    public void ResetScores()
    {
        UpdateTotalScore(false, 0);
        UpdateTotalScore(true, 0);

        foreach(RowScore pair in rowScores)
        {
            pair.rowScoreText.text = "0";
        }
    }

    //MULLIGAN PHASE
    public void BeginMulliganPhase(List<Card> playerHand, CardGameManager cardGameManager)
    {
        gameManager = cardGameManager;

        mulliganCanvas.SetActive(true);

        redrawsRemainingText.text = "0/" + gameManager.GetMaxMulligans().ToString();

        foreach(Transform card in mulliganCardsHeader)
        {
            int index = card.GetSiblingIndex();
            bool shouldActivate = index < playerHand.Count;

            card.gameObject.SetActive(shouldActivate);

            if (shouldActivate)
            {
                //Set the Card Data.
                card.GetComponent<CardUI>().SetBaseCardData(playerHand[index]);
            }
        }
    }

    public void CardRedrawn(int index, Card newCard, int mulliganCount)
    {
        redrawsRemainingText.text = mulliganCount.ToString() + "/" + gameManager.GetMaxMulligans().ToString();

        //Set the Card Data.
        mulliganCardsHeader.GetChild(index).GetComponent<CardUI>().SetBaseCardData(newCard);
    }

    public void EndMulliganPhase()
    {
        mulliganCanvas.SetActive(false);
    }

    //GETTERS && SETTERS
    public FieldCard GetFieldCardFromRow(CardGameManager.BattleRow row)
    {
        Transform rowHeader = GetRowHeader(row);

        foreach(Transform card in rowHeader)
        {
            if (!card.gameObject.activeInHierarchy)
            {
                return card.GetComponent<FieldCard>();
            }
        }

        //If you reach here, there are no available field cards 
        //So Make one and return that.
        return null;
    }

    private Transform GetRowHeader(CardGameManager.BattleRow row)
    {
        switch (row)
        {
            case CardGameManager.BattleRow.OpponentMelee:
                return opponentMeleeRow;
            case CardGameManager.BattleRow.OpponentRanged:
                return opponetRangedRow;
            case CardGameManager.BattleRow.PlayerMelee:
                return playerMeleeRow;
            case CardGameManager.BattleRow.PlayerRanged: 
                return playerRangedRow;
            default:
                return playerMeleeRow;
        }
    }
}
