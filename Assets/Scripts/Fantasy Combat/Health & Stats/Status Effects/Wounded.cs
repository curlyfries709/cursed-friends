using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wounded : StatusEffect
{
    float finesseMultiplier = 0.5f;
    float attackMultiplier = 0.25f;
    float agilityMultiplier = 0.25f;

    public override void OnEffectApplied()
    {
        SpawnVisual();
        CalculateNewStatValue(false);
    }


    protected override void CalculateNewStatValue(bool resetValues)
    {
        //ATTACK DOWN
        myUnit.stats.UpdateBuffMultiplier(ref myUnit.stats.buffSTRmultiplier, attackMultiplier, resetValues);
        myUnit.stats.UpdateBuffMultiplier(ref myUnit.stats.buffINTmultiplier, attackMultiplier, resetValues);

        //FIN DOWN
        myUnit.stats.UpdateBuffMultiplier(ref myUnit.stats.buffFINmultiplier, finesseMultiplier, resetValues);

        //AG DOWN
        myUnit.stats.UpdateBuffMultiplier(ref myUnit.stats.buffAGmultiplier, agilityMultiplier, resetValues);
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
