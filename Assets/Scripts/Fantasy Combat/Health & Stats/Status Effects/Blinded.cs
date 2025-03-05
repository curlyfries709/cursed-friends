using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blinded : StatusEffect
{
    float finesseMultiplier = 0.25f;
    int tripChance = 25;

    public override void OnEffectApplied()
    {
        SpawnVisual();
        CalculateNewStatValue(false);
        myUnit.CanTriggerSkill += DidNotTrip;
    }

    private bool DidNotTrip()
    {
        int randNum = Random.Range(0, 101);

        if (randNum <= tripChance)
        {
            StatusEffectManager.Instance.TrippedFromBlinded(myUnit);
            return false;
        }

        return true;
    }

    protected override void CalculateNewStatValue(bool resetValues)
    {
        myUnit.stats.UpdateBuffMultiplier(ref myUnit.stats.buffFINmultiplier, finesseMultiplier , resetValues);
    }

    protected override void OnTurnEnd()
    {
        DecreaseTurnsRemaining();
    }

    protected override void EffectEnded()
    {
        myUnit.CanTriggerSkill -= DidNotTrip;
        CalculateNewStatValue(true);
        RemoveStatusEffect();
    }

    protected override void OnStatusStacked() { }


    protected override void OnTurnStart() { }
}
