using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardOpponentSorter : MonoBehaviour
{
    public void PrepareGame()
    {
        CardDataManager.Instance.ChallengeOpponent(GetData());
    }


    private CardOpponentData GetData()
    {
        //For Not Return first in child
        return GetComponentInChildren<CardOpponentData>();
    }
}
