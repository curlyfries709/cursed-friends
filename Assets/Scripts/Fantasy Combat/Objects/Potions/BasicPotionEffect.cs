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

        if (potionData.hpGain > 0 || potionData.spGain > 0 || potionData.fpGain > 0)
        {
            RestoreAndRevive(drinker);
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

    private void RestoreAndRevive(CharacterGridUnit target)
    {
        //Show VFX too.

        HealData healData = new HealData(target, potionData.hpGain, potionData.spGain, potionData.fpGain, potionData.canRevive);
        target.CharacterHealth().Heal(healData);
        Health.RaiseHealthChangeEvent(true);

        hasStartedHealthCountdown = true;
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
            target.CharacterHealth().SetBuffsToApplyVisual(potionData.inflictedStatusEffects);

        StatusEffectManager.Instance.TriggerNewlyAppliedEffects(target);

        if (potionData.inflictedStatusEffects.Any((effect) => effect.statusEffect.isStatBuffOrDebuff))
            target.CharacterHealth().GetHealthUI().ShowBuffsOnly();
    }

    private void FullCharge(CharacterGridUnit drinker, FantasyCombatCollectionManager collectionManager)
    {
        collectionManager.ChargeAllChargeables(drinker);
    }

    private void BeginCountdown()
    {
        if (!hasStartedHealthCountdown)
        {
            //FantasyCombatManager.Instance.BeginHealthUICountdown();
            hasStartedHealthCountdown = true;
        }
    }
}
