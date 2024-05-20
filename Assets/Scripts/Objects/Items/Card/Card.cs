using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


[CreateAssetMenu(fileName = "New Card", menuName = "Item/Card", order = 6)]
public class Card : Item
{

    [Header("Card Art")]
    public Sprite art;
    [Header("Card Data")]
    public Faction faction;
    [Space(10)]
    public CardColor color;
    public CardType cardType;
    [Header("Unit Card Stats")]
    [ShowIf("cardType", CardType.Unit)]
    public int basePower;
    [ShowIf("cardType", CardType.Unit)]
    public int baseHealth;
    [ShowIf("cardType", CardType.Unit)]
    public Race race;
    [Header("Ability")]
    [Tooltip("Is this card played on the opponent's side?")]
    public bool isSpy;
    public List<AbilityData> abilityData = new List<AbilityData>();

    public enum CardColor
    {
        Bronze,
        Gold
    }

    public enum CardType
    {
        Unit,
        Spell,
        Weather
    }

    public enum Faction
    {
        Neutral,
        Werewolves,
        OrcMermaids,
        VampireFairies,
        ElvesDryad,
        GnomesDemons
    }

    [System.Serializable]
    public class AbilityData
    {
        public GameObject ability;
        public int abilityVariable; //For the damage ability, the variable will be the amount to damage by.
    }

}
