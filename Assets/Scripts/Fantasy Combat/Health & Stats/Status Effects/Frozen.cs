using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Frozen : StatusEffect
{
    private float applyFreezeDelay = 0.3f;
    public override void OnEffectApplied()
    {
        StartCoroutine(ApplyFreezeRoutine());
    }

    IEnumerator ApplyFreezeRoutine()
    {
        yield return new WaitForSeconds(applyFreezeDelay);

        SpawnVisual();
        myUnit.unitAnimator.Freeze(true);
        myUnit.Health().Guard(false);

        //Frozen Cancels Out Burning
        StatusEffectManager.Instance.CureStatusEffect(myUnit, "Burning");

        //Listen To Event To Make It Critcal
        myUnit.AlterDamageReceived += OnAlterUnitDamageReceived;
    }

    private DamageReceivedAlteration OnAlterUnitDamageReceived()
    {
        DamageReceivedAlteration alteration = new DamageReceivedAlteration(1);
        alteration.isCritical = true;

        return alteration;
    }

    protected override void OnTurnEnd()
    {
        DecreaseTurnsRemaining();
    }

    protected override void EffectEnded()
    {
        myUnit.AlterDamageReceived -= OnAlterUnitDamageReceived;
        myUnit.unitAnimator.Freeze(false);
        RemoveStatusEffect();
    }

    protected override void OnStatusStacked() { }
    protected override void CalculateNewStatValue(bool resetValues) {}

    protected override void OnTurnStart(){}
}
