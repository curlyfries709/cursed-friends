
using System;
using UnityEngine;

[RequireComponent(typeof(SaveableEntity))]
public class RespawnableCombatDirectInteractable : CombatDirectInteractable, IRespawnable
{
    [Header("Respawnable")]
    [SerializeField] int daysToRespawn = 5;

    public bool isRemoved { get; set; } = false;
    public DateTime removedDate { get; set; }
    public GameObject associatedGameObject { get; set; }
    public IRespawnable.RespawnableState respawnableState { get; set; } = new IRespawnable.RespawnableState();
    public bool isDataRestored { get; set; } = false;

    protected void Awake()
    {
        associatedGameObject = gameObject;
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
