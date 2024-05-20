using System.Collections.Generic;
using UnityEngine;
using System;

public class MercifulDoom : StatusEffect, ITurnEndEvent
{
    public int turnEndEventOrder { get; set; }

    const int mercifulDoomDuration = 3;

    public override void OnEffectApplied()
    {
        turnsRemaining = mercifulDoomDuration; //Overwrite turns remaining as this SE is special and should only last 3 turns.

        //Display Visual Effect.
        turnEndEventOrder = 70;
        SpawnVisual();

        //This is necessary, if SE applied during a counterattack.
        if (FantasyCombatManager.Instance.GetCurrentTurnOwner() == myUnit)
            OnTurnStart();
    }

    public override void IncreaseTurns(int numOfTurns, int buffChange)
    {
        //Method Overridden because this SE cannot stack turns.
    }

    protected override void EffectEnded()
    {
        //Remove Visual Effect
        RemoveStatusEffect();
    }

    protected override void OnTurnStart()
    {
        bool isLastTurn = turnsRemaining == 1;

        if (isLastTurn)
        {
            FantasyCombatManager.Instance.AddTurnEndEventToQueue(this);
        }
    }

    protected override void OnTurnEnd()
    {
        DecreaseTurnsRemaining();
    }

    public void PlayTurnEndEvent()
    {
        myUnit.Health().TakeStatusEffectDamage(inflictor, 100);
        StatusEffectManager.Instance.PlayDamageTurnEndEvent(myUnit);
    }

    public void OnEventCancelled()
    {
        //Event Cannot be cancelled so do nothing
        Debug.LogError("MERCIFUL DOOM EVENT CANCELLED!");
    }

    protected override void CalculateNewStatValue(bool resetValues) { }
    protected override void OnStatusStacked() { }

    public List<Type> GetEventTypesThatCancelThis()
    {
        return new List<Type>();
    }
}
