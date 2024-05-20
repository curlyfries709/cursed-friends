using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardOpponentData : MonoBehaviour
{
    [Header("Deck Data")]
    [SerializeField] Card.Faction deckFaction;
    [SerializeField] List<Card> deck;




    //GETTERS
    public Card.Faction GetFaction() { return deckFaction; }
    public List<Card> GetDeck() { return deck; }
}
