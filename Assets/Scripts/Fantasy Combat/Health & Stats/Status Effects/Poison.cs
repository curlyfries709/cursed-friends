using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Poison : StatusEffect, ITurnEndEvent
{
    public override void OnEffectApplied()
    {
        //Display Visual Effect.
        SpawnVisual();

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
        if (turnsRemaining > 0)// OnTurnStart Gets called before the destroy so this is important. 
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
        ApplyStatusEffectDamageToUnit(5);
        myUnit.CharacterHealth().TakeSPLoss(5);

        StatusEffectManager.Instance.PlayDamageTurnEndEvent(myUnit);
    }

    public void OnEventCancelled()
    {
        Debug.LogError("POISON EVENT CANCELLED!");
    }

    protected override void CalculateNewStatValue(bool resetValues) { }
    protected override void OnStatusStacked() { }

    public float GetTurnEndEventOrder()
    {
        return 90;
    }

    public List<Type> GetEventTypesThatCancelThis()
    {
        return new List<Type>();
    }
}
