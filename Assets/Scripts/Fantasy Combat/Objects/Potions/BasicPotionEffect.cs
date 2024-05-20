using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BasicPotionEffect : MonoBehaviour
{
    public Potion potionData;
    public float dataDisplayExtension;

    bool hasStartedHealthCountdown = false;

    public virtual void ActivateEffect(CharacterGridUnit drinker, CharacterGridUnit passer, FantasyCombatCollectionManager collectionManager)
    {
        hasStartedHealthCountdown = false;

        FantasyCombatManager.Instance.UpdateDamageDataDisplayTime(Affinity.None, false, false, dataDisplayExtension);

        if (potionData.hpGain > 0 || potionData.spGain > 0)
        {
            HealAndRevive(drinker);
        }

        if(potionData.fpGain > 0)
        {
            GainFP(drinker);
        }

        if(potionData.statusEffectsToCure.Count > 0)
        {
            Cure(drinker);
        }

        if(potionData.inflictedStatusEffects.Count > 0)
        {
            ApplyStatusEffects(drinker, passer);
        }

        if (potionData.chargeAllItems)
        {
            FullCharge(drinker, collectionManager);
        }

        BeginCountdown();
    }

    private void HealAndRevive(CharacterGridUnit target)
    {
        //Show VFX too.
        if (potionData.canRevive && target.Health().isKOed)
        {
            target.Health().Revive(potionData.hpGain, potionData.spGain, false);
        }
        else
        {
            target.Health().Heal(potionData.hpGain, potionData.spGain, false);
        }

        hasStartedHealthCountdown = true;
    }

    private void GainFP(CharacterGridUnit target)
    {
        target.Health().GainFP(potionData.fpGain);
    }

    private void Cure(CharacterGridUnit target)
    {
        //Show VFX too.
        foreach(StatusEffectData statusEffect in potionData.statusEffectsToCure)
        {
            StatusEffectManager.Instance.CureStatusEffect(target, statusEffect);
        }
    }

    private void ApplyStatusEffects(CharacterGridUnit target, CharacterGridUnit giver)
    {
        foreach (ChanceOfInflictingStatusEffect effect in potionData.inflictedStatusEffects)
        {
            StatusEffectManager.Instance.ApplyStatusEffect(effect.statusEffect, target, giver, effect.numOfTurns, effect.buffChange);
        }

        if (potionData.inflictedStatusEffects.Any((effect) => effect.statusEffect.isStatBuffOrDebuff))
            target.Health().SetBuffsToApplyVisual(potionData.inflictedStatusEffects);

        StatusEffectManager.Instance.TriggerNewlyAppliedEffects(target);

        if (potionData.inflictedStatusEffects.Any((effect) => effect.statusEffect.isStatBuffOrDebuff))
            target.Health().ActivateBuffHealthVisual();
    }

    private void FullCharge(CharacterGridUnit drinker, FantasyCombatCollectionManager collectionManager)
    {
        collectionManager.ChargeAllChargeables(drinker);
    }

    private void BeginCountdown()
    {
        if (!hasStartedHealthCountdown)
        {
            FantasyCombatManager.Instance.BeginHealthUICountdown();
            hasStartedHealthCountdown = true;
        }
    }
}
