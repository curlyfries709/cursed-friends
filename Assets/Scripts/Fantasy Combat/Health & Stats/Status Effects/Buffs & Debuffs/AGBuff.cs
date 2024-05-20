using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AGBuff : StatusEffect
{
    public override void OnEffectApplied()
    {
        SpawnVisual();
        CalculateNewStatValue(false);
    }

    protected override void CalculateNewStatValue(bool resetValues) 
    {
        myUnit.stats.UpdateBuffMultiplier(ref myUnit.stats.buffAGmultiplier, StatusEffectManager.Instance.GetBuffMultiplier(currentBuff), resetValues);
    }

    protected override void OnTurnEnd()
    {
        DecreaseTurnsRemaining();
    }

    protected override void EffectEnded()
    {
        CalculateNewStatValue(true);
        RemoveStatusEffect();
    }

    protected override void OnStatusStacked() { }
    

    protected override void OnTurnStart() { }
}
