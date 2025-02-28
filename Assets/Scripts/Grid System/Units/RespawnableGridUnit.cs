using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnableGridUnit : GridUnit, IRespawnable
{
    [Header("Respawnable")]
    [SerializeField] int daysToRespawn = 5;

    public bool isRemoved { get; set; }
    public DateTime removedDate { get; set; }
    public GameObject associatedGameObject { get; set; }
    public IRespawnable.RespawnableState respawnableState { get; set; }
    public bool isDataRestored { get; set; }

    protected override void Awake()
    {
        base.Awake();
        associatedGameObject = gameObject;
    }

    protected override void OnEnable()
    {
        if (!isRemoved)
        {
            base.OnEnable();
            global::Health.UnitKOed += OnUnitKO;
        }
    }

    protected void OnUnitKO(GridUnit unit)
    {
        if(unit != this) { return; }

        global::Health.UnitKOed -= OnUnitKO;
        ((IRespawnable)this).OnRemovedFromRealm(false);
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
