using System.Collections.Generic;
using UnityEngine;
using System;

public class Burning : StatusEffect, ITurnEndEvent
{
    public int turnEndEventOrder { get; set; }

    public override void OnEffectApplied()
    {
        //Display Visual Effect.
        turnEndEventOrder = 85;
        SpawnVisual();

        //Burning Cancels out Frozen
        StatusEffectManager.Instance.CureStatusEffect(myUnit, "Frozen");

        //This is necessary, if SE applied during a counterattack.
        if (FantasyCombatManager.Instance.GetCurrentTurnOwner() == myUnit)
            OnTurnStart();
    }

    protected override void EffectEnded()
    {
        //Remove Visual Effect
        RemoveStatusEffect();
    }

    protected override void OnTurnStart()
    {
        if (turnsRemaining >  0)// OnTurnStart Gets called before the destroy so this is important. 
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
        myUnit.Health().TakeStatusEffectDamage(inflictor, 10);
        StatusEffectManager.Instance.PlayDamageTurnEndEvent(myUnit);
    }

    public void OnEventCancelled()
    {
        //Event Cannot be cancelled so do nothing
        Debug.LogError("BURNING EVENT CANCELLED!");
    }

    protected override void CalculateNewStatValue(bool resetValues) { }
    protected override void OnStatusStacked(){}

    public List<Type> GetEventTypesThatCancelThis()
    {
        return new List<Type>();
    }
}
