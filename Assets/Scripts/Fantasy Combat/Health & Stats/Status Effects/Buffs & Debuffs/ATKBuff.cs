using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ATKBuff : StatusEffect
{
    public override void OnEffectApplied()
    {
        SpawnVisual();
        CalculateNewStatValue(false);
    }

    protected override void CalculateNewStatValue(bool resetValues)
    {
        myUnit.stats.UpdateBuffMultiplier(ref myUnit.stats.buffSTRmultiplier, StatusEffectManager.Instance.GetBuffMultiplier(currentBuff), resetValues);
        myUnit.stats.UpdateBuffMultiplier(ref myUnit.stats.buffINTmultiplier, StatusEffectManager.Instance.GetBuffMultiplier(currentBuff), resetValues);
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
