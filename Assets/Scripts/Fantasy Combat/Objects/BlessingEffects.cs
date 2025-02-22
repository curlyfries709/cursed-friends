using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BlessingEffects : MonoBehaviour, ITurnStartEvent
{

    [Header("UI")]
    [SerializeField] TextMeshProUGUI blessingName;
    [Space(5)]
    [SerializeField] GameObject blessUI;
    [SerializeField] FadeUI blessingFlasher;
    [Header("Timers")]
    [SerializeField] float activateBlessingDuration = 1.5f;
    [SerializeField] float flashDuration = 0.1f;
    [Header("Components")]
    public Blessing activeBlessing;
    public FantasyCombatCollectionManager collectionManager;

    int turnCounter = 0;

    public int turnStartEventOrder { get; set; } = 10;

    private void Awake()
    {
        blessUI.GetComponentInChildren<CombatEventCanvas>().Setup(activateBlessingDuration, false);
    }

    private void OnAnyUnitTurnStart(CharacterGridUnit actingUnit, int turnNumber)
    {
        if (IsAlliedUnit())
        {
            FantasyCombatManager.Instance.AddTurnStartEventToQueue(this);
        }
    }

    private void OnAnyUnitTurnEnd()
    {
        if (IsAlliedUnit())
        {
            turnCounter--;

            if (turnCounter <= 0)
            {
                ResetEffect();
            }

            HUDManager.Instance.UpdateBlessing(activeBlessing, turnCounter);
        }
    }

    public void ActivateEffect(PlayerGridUnit blesser)
    {
        if (HasRecoveryEffect())
            FantasyCombatManager.Instance.OnNewTurn += OnAnyUnitTurnStart;

        FantasyCombatManager.Instance.OnTurnFinished += OnAnyUnitTurnEnd;

        turnCounter = activeBlessing.duration;

        StartCoroutine(UIRoutine(blesser));

        //Apply Buffs
        ApplyAndActivateBuffs(blesser);

        //Alter Affinities
        AlterAffinities(false);

        //Update Blessing Multipliers
        foreach (PlayerGridUnit player in PartyManager.Instance.GetActivePlayerParty())
        {
            UpdateUnitBlessingMultipliers(player.stats, false);
        }
    }

    IEnumerator UIRoutine(PlayerGridUnit blesser)
    {
        ControlsManager.Instance.DisableControls();

        //Prep healing
        Heal(blesser);

        blessingName.text = activeBlessing.nickname;
        blessingFlasher.Fade(true);
        yield return new WaitForSeconds(flashDuration);
        blessUI.SetActive(true);
        blesser.GetPhotoShootSet().PlayBlessingUI();
        yield return new WaitForSeconds(activateBlessingDuration);
        blessUI.SetActive(false);
        blesser.GetPhotoShootSet().DeactivateSet();
        FantasyCombatManager.Instance.TacticActivated();
        HUDManager.Instance.UpdateBlessing(activeBlessing, turnCounter);
        FantasyCombatCollectionManager.BlessingUsed?.Invoke(blesser);

        //Activate healing
        IDamageable.RaiseHealthChangeEvent(true);


        collectionManager.BeginBlessingCooldown(activeBlessing, blesser);
    }

    public void ResetEffect()
    {
        if (HasRecoveryEffect())
            FantasyCombatManager.Instance.OnNewTurn -= OnAnyUnitTurnStart;

        FantasyCombatManager.Instance.OnTurnFinished -= OnAnyUnitTurnEnd;

        //Reset Affinities
        AlterAffinities(true);

        //Reset Blessing Multipliers
        foreach (PlayerGridUnit player in PartyManager.Instance.GetActivePlayerParty())
        {
            UpdateUnitBlessingMultipliers(player.stats, true);
        }

        //Reset Data
        turnCounter = 0;
        activeBlessing = null;
    }

    //Recovery Effects
    public void PlayTurnStartEvent()
    {
        Heal(FantasyCombatManager.Instance.GetActiveUnit() as PlayerGridUnit);
        IDamageable.RaiseHealthChangeEvent(true);
    }

    private void Heal(PlayerGridUnit unitToHeal)
    {
        if (activeBlessing.hpIncrease > 0 || activeBlessing.spIncrease > 0)
        {
            //Heal Acting Unit.
            int HPHeal = Mathf.RoundToInt(unitToHeal.stats.Vitality * ((float)activeBlessing.hpIncrease / 100));
            int SPHeal = Mathf.RoundToInt(unitToHeal.stats.Stamina * ((float)activeBlessing.spIncrease / 100));

            HealData healData = new HealData(unitToHeal, HPHeal, SPHeal, 0, false);
            unitToHeal.Health().Heal(healData);
        }
    }

    //Doers
    private void ApplyAndActivateBuffs(PlayerGridUnit blesser)
    {
        foreach (PlayerGridUnit player in PartyManager.Instance.GetActivePlayerParty())
        {
            foreach (ChanceOfInflictingStatusEffect buff in activeBlessing.statusEffectsToApply)
            {
                StatusEffectManager.Instance.ApplyAndActivateStatusEffect(buff.statusEffect, player, blesser, blesser.stats.SEDuration, buff.buffChange);
            }
        }
    }

    private void UpdateUnitBlessingMultipliers(UnitStats playerStat, bool reset)
    {
        if (activeBlessing.damageDealtIncrease > 0)
        {
            float multiplier = reset ? 1 : GetMultiplier(activeBlessing.damageDealtIncrease);
            playerStat.UpdateBlessingMultiplier(out playerStat.blessingDamageMultiplier, multiplier);
        }

        if (activeBlessing.damageReceivedReduction > 0)
        {
            float multiplier = reset ? 1 : GetMultiplier(activeBlessing.damageReceivedReduction, true);
            playerStat.UpdateBlessingMultiplier(out playerStat.blessingDamageReductionMultiplier, multiplier);
        }

        if (activeBlessing.techniqueIncrease > 0)
        {
            float multiplier = reset ? 1 : GetMultiplier(activeBlessing.techniqueIncrease);
            playerStat.UpdateBlessingMultiplier(out playerStat.blessingTechniqueMultiplier, multiplier);
        }

        if (activeBlessing.evasionIncrease > 0)
        {
            float multiplier = reset ? 1 : GetMultiplier(activeBlessing.evasionIncrease);
            playerStat.UpdateBlessingMultiplier(out playerStat.blessingEvasionMultiplier, multiplier);
        }

        if (activeBlessing.speedIncrease > 0)
        {
            float multiplier = reset ? 1 : GetMultiplier(activeBlessing.speedIncrease);
            playerStat.UpdateBlessingMultiplier(out playerStat.blessingSpeedMultiplier, multiplier);
        }

        if (activeBlessing.movementIncrease > 0)
        {
            int adder = reset ? 0 : activeBlessing.movementIncrease;
            playerStat.UpdateBlessingAddition(out playerStat.blessingMovementIncrease, adder);
        }

        if (activeBlessing.healIncrease> 0)
        {
            float multiplier = reset ? 1 : GetMultiplier(activeBlessing.healIncrease);
            playerStat.UpdateBlessingMultiplier(out playerStat.blessingHealEfficacyMultiplier, multiplier);
        }

        if (activeBlessing.statusEffectsDuration > 0)
        {
            int adder = reset ? 0 : activeBlessing.statusEffectsDuration;
            playerStat.UpdateBlessingAddition(out playerStat.blessingSEDurationIncrease, adder);
        }

        if (activeBlessing.critChanceIncrease> 0)
        {
            int adder = reset ? 0 : activeBlessing.critChanceIncrease;
            playerStat.UpdateBlessingAddition(out playerStat.blessingStatusEffectInflictChanceIncrease, adder);
            playerStat.UpdateBlessingAddition(out playerStat.blessingCritChanceIncrease, adder);
        } 
    }


    private void AlterAffinities(bool reset)
    {
        foreach (PlayerGridUnit player in PartyManager.Instance.GetActivePlayerParty())
        {
            foreach (ElementAffinity affinity in activeBlessing.elementAffinitiesToAlter)
            {
                if (reset)
                {
                    player.stats.ResetElementAffinity(affinity.element);
                }
                else
                {
                    player.stats.AlterElementAffinity(affinity);
                }
            }
        }
    }


    private float GetMultiplier(int percentage, bool isReduction=false)
    {
        if (isReduction)
        {
            return 1 - ((float)percentage / 100);
        }

        return 1 + ((float)percentage / 100);
    }

    private bool IsAlliedUnit()
    {
        return FantasyCombatManager.Instance.GetActiveUnit().team == CombatTeam.Keenan;
    }

    private bool HasRecoveryEffect()
    {
        return activeBlessing.hpIncrease > 0 || activeBlessing.spIncrease > 0;
    }

}
