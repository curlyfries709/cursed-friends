using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IDamageable
{
    //Variables
    public int currentHealth { get; set; }
    public int currentSP { get; set; }
    public int currentFP { get; set; }

    //Events
    public static Action<bool> unitAttackComplete;
    public static Action<DamageData> unitHit;

    //Methods
    public Affinity TakeDamage(AttackData damageData);

    public void TakeBumpDamage(int damage);

    public void ActivateHealthVisual(bool show);

    public void DeactivateHealthVisualImmediate();

    public void ResetStateToBattleStart(int healthAtStart, int spAtStart, int fpAtStart);

    public AffinityFeedback GetDamageFeedbacks(Transform transformToPlayVFX, GameObject VFXToPlay);



    //Health Methods

    public void BeginHealthUICountdown(bool beginCountdown)
    {
        if (!beginCountdown) { return; }
        //Always Call this last so FantasyManager Can set Extensions if necessary.
        FantasyCombatManager.Instance.BeginHealthUICountdown();
    }


    public void SetVFXToPlay(GridUnit myUnit, AffinityFeedback feedbacks, Transform transformToPlayVFX, GameObject VFXToPlay)
    {
        //Deactive all children so VFX Doesn't trigger when detached
        foreach (Transform child in feedbacks.spawnVFXHeader)
        {
            child.gameObject.SetActive(false);
        }

        //Unparent Children
        feedbacks.spawnVFXHeader.DetachChildren();

        if(!VFXToPlay) { return; }

        VFXToPlay.transform.parent = null;

        Vector3 spawnHitDestination = myUnit.GetClosestPointOnColliderToPosition(transformToPlayVFX.position) + (transformToPlayVFX.forward.normalized * 0.25f);

        VFXToPlay.transform.position = spawnHitDestination;
        VFXToPlay.transform.rotation = transformToPlayVFX.rotation;

        VFXToPlay.transform.parent = feedbacks.spawnVFXHeader;
    }

}
