
using UnityEngine;
using MoreMountains.Feedbacks;
using System;

public class PickableIngredient : Interact, IRespawnable
{
    [Header("Ingredient")]
    [SerializeField] Ingredient ingredient;
    [SerializeField] int daysToRespawn = 5;
    [Space(10)]
    [SerializeField] MMF_Player pickupFeedback;

    public bool isRemoved { get; set; } = false;
    public DateTime removedDate { get; set; }
    public GameObject associatedGameObject { get; set; }
    public IRespawnable.RespawnableState respawnableState { get; set; } = new IRespawnable.RespawnableState();
    public bool isDataRestored { get; set; } = false;

    private void Awake()
    {
        associatedGameObject = gameObject;
    }

    public override void HandleInteraction(bool inCombat)
    {
        if (inCombat) { return; }
        ((IRespawnable)this).OnRemovedFromRealm();
    }

    public int GetDaysToRespawn()
    {
        return daysToRespawn;
    }

    //SAVING
    public object CaptureState()
    {
        return ((IRespawnable)this).CaptureRespawnableState();
    }

    public void RestoreState(object state)
    {
        ((IRespawnable)this).RestoreRespawanableState(state);
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
