using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Overburdened : StatusEffect
{
    const float buffMultiplier = 0.25f;

    public override void OnEffectApplied()
    {
        //Movement is reduced to 1. Speed is reduced. Evasion & Technique is reduced. 
        SpawnVisual();
        CalculateNewStatValue(false);
    }

    protected override void CalculateNewStatValue(bool resetValues)
    {
        //Reduce Agility
        myUnit.stats.UpdateBuffMultiplier(ref myUnit.stats.buffAGmultiplier, buffMultiplier, resetValues);
        //Reduce Evasion & Technique
        myUnit.stats.UpdateBuffMultiplier(ref myUnit.stats.buffFINmultiplier, buffMultiplier, resetValues);
    }

    protected override void OnTurnEnd(){}

    protected override void EffectEnded()
    {
        //Remov Debuffs
        CalculateNewStatValue(true);
        RemoveStatusEffect();
    }

    protected override void OnStatusStacked() { }

    protected override void OnTurnStart() { }
}
