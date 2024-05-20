using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CRITBuff : StatusEffect
{
    public override void OnEffectApplied()
    {
        SpawnVisual();
        CalculateNewStatValue(false);
    }

    protected override void CalculateNewStatValue(bool resetValues)
    {
        myUnit.stats.UpdateBuffMultiplier(ref myUnit.stats.buffCHRmultiplier, StatusEffectManager.Instance.GetBuffMultiplier(currentBuff), resetValues);
    }

    protected override void OnTurnEnd()
    {
        DecreaseTurnsRemaining();
    }

    protected override void EffectEnded()
    {
        //Reset Buff
        CalculateNewStatValue(true);
        RemoveStatusEffect();
    }

    protected override void OnStatusStacked() { }


    protected override void OnTurnStart() { }
}
