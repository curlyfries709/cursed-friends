using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FieldCard : BasePlayableCard
{
    public int currentHealth { get; private set; }
    public int currentPower { get; private set; }

    public CardGameManager.BattleRow battleRow { get; private set; }

    //Action
    public Action OnDeath;

    public override void Setup(Card myCard, bool isPlayerCard)
    {
        base.Setup(myCard,isPlayerCard);

        //Setup Card Vitals
        currentHealth = myCard.baseHealth;
        currentPower = myCard.basePower;
    }

    //Actions
    public override void OnPlay(CardGameManager.BattleRow selectedRow, bool didPlayerPlayCard)
    {
        battleRow = selectedRow;
        gameObject.SetActive(true);

        //Inform Manager: New Card added to Battlefield
        CardGameManager.Instance.BattlefieldCountChanged(this, false);

        //Update Score
        CardGameManager.Instance.ScoreChange?.Invoke(isPlayerCard, selectedRow, currentPower);

        //Spawn Turn End Abilties
        SpawnTurnEndAbilities();

        //Trigger Abilities
        CardPlayed();
    }

    public override void OnHover()
    {
        SetTooltip();
    }

    public override void OnSelect()
    {
        CardGameManager.Instance.OnFieldCardSelect(this, true);
    }

    private void SpawnTurnEndAbilities()
    {
        Debug.Log("Spawning Abilities");

        //Spawn Turn End Abilities in Hireachy. 
        foreach (Card.AbilityData abilityData in cardData.abilityData)
        {
            if (abilityData.ability.GetComponent<BaseCardAbility>().IsTurnEndAbility())
            {
                CardGameManager.Instance.SpawnAbility(abilityData, this, isPlayerCard);
            }
        }
    }

    //Abilities

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(currentHealth - damage, 0);

        Debug.Log(cardData.itemName + " Took Damage of: " + damage.ToString());

        if(currentHealth <= 0)
        {
            Die();
            return;
        }

        //Update UI
        UI.SetHealth(currentHealth);
    }

    public void Heal(int heal)
    {
        currentHealth = Mathf.Min(currentHealth + heal, cardData.baseHealth);

        Debug.Log(cardData.itemName + " healed for: " + heal.ToString());

        //Update UI
        UI.SetHealth(currentHealth);
    }

    public void Empower(int powerChange)
    {
        currentPower = currentPower + powerChange;

        //Update UI
        UI.SetPower(currentPower);

        //Update Score
        Debug.Log("Empower method update score");
        CardGameManager.Instance.ScoreChange?.Invoke(isPlayerCard, battleRow, powerChange);
    }

    public void SetPower(int newPower)
    {
        int oldPower = currentPower;
        int powerDifference = newPower - oldPower;

        //Set Power
        currentPower = newPower;

        //Update UI
        UI.SetPower(currentPower);

        //Update Score
        CardGameManager.Instance.ScoreChange?.Invoke(isPlayerCard, battleRow, powerDifference);

    }

    private void Die()
    {
        Debug.Log(cardData.itemName + " Died! RIP...");

        //Update Score
        CardGameManager.Instance.ScoreChange?.Invoke(isPlayerCard, battleRow, -currentPower);

        //Inform Manager: Card removed from Battlefield
        CardGameManager.Instance.BattlefieldCountChanged(this, true);

        //Deactive card
        gameObject.SetActive(false);

        //Send to owner graveyard
        CardGameManager.Instance.AddToGraveyard(cardData, isPlayerCard);

        //Set Sibling index as last.
        transform.SetAsLastSibling();

        //Invoke Event
        OnDeath?.Invoke();
    }

    public void TransformCard(Card newCard)
    {
        //Invoke Event so Any attached Abilities can destroy itself.
        OnDeath?.Invoke();

        //Setup New Card Data
        Setup(newCard, isPlayerCard);

        //Spawn New Abilities
        SpawnTurnEndAbilities();
    }
}
