using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FiredUp : StatusEffect
{
    public override void OnEffectApplied()
    {
        SpawnVisual();

        //Subscribe to turn Start
        myUnit.BeginTurn += OnTurnStart;
        myUnit.Health().SetFiredUp(true);
    }

    public override void IncreaseTurns(int numOfTurns, int buffChange)
    {
        //Method Overridden because this SE cannot stack turns.
    }


    protected override void EffectEnded()
    {
        //Remove VFX.
        myUnit.Health().SetFiredUp(false);
        myUnit.Health().LoseFP(true);

        RemoveStatusEffect();
        myUnit.BeginTurn -= OnTurnStart;
    }

    protected override void OnTurnStart()
    {
        Again.Instance.SetUnitToGoAgain(myUnit);
    }

    protected override void OnTurnEnd()
    {
        DecreaseTurnsRemaining();
    }

    protected override void OnStatusStacked() { }
    protected override void CalculateNewStatValue(bool resetValues) { }
    
}
