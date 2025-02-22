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
        myUnit.ModifyDamageReceived += OnModifyUnitDamageReceived;
    }

    private DamageModifier OnModifyUnitDamageReceived(DamageData damageData)
    {
        DamageModifier damageModifier = new DamageModifier();
        damageModifier.isCrit = new HealthModifier.Modifier<bool>(true, HealthModifier.Priority.Absolute);

        return damageModifier;
    }

    protected override void OnTurnEnd()
    {
        DecreaseTurnsRemaining();
    }

    protected override void EffectEnded()
    {
        myUnit.ModifyDamageReceived -= OnModifyUnitDamageReceived;
        myUnit.unitAnimator.Freeze(false);
        RemoveStatusEffect();
    }

    protected override void OnStatusStacked() { }
    protected override void CalculateNewStatValue(bool resetValues) {}

    protected override void OnTurnStart(){}
}
