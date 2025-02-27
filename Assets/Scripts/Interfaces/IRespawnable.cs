using System;
using UnityEngine;
using Sirenix.Serialization;

public interface IRespawnable : ISaveable
{
    //Respawn Data
    public bool isRemoved { get; set; }
    public DateTime removedDate {  get; set; }

    public GameObject associatedGameObject { get; set; }

    //Saving Data
    [SerializeField, HideInInspector]
    public RespawnableState respawnableState { get; set;}
    public bool isDataRestored { get; set; }

    public int GetDaysToRespawn();

    public virtual void OnRemovedFromRealm(GameObject gameObjectToDeactivate)
    {
        isRemoved = true;

        //Set Picked Date
        removedDate = CalendarManager.Instance.currentDate;

        gameObjectToDeactivate?.SetActive(false);
    }

    protected void Respawn(GameObject gameObjectToActivate)
    {
        if (!isRemoved || CalendarManager.Instance.GetDaysPassed(removedDate) >= GetDaysToRespawn())
        {
            isRemoved = false;
            gameObjectToActivate.SetActive(true);
        }
        else
        {
            gameObjectToActivate.SetActive(false);
        }
    }

    //Saving
    [System.Serializable]
    public class RespawnableState
    {
        public bool isPicked = false;

        //Date
        public int pickedDay;
        public int pickedMonth;
        public int pickedYear;
    }

    public object CaptureRespawnableState()
    {
        respawnableState.pickedDay = removedDate.Day;
        respawnableState.pickedMonth = removedDate.Month;
        respawnableState.pickedYear = removedDate.Year;

        respawnableState.isPicked = isRemoved;


        return SerializationUtility.SerializeValue(respawnableState, DataFormat.Binary);
    }

    public void RestoreRespawanableState(object state)
    {
        isDataRestored = true;

        if (state == null) { return; }

        byte[] bytes = state as byte[];
        respawnableState = SerializationUtility.DeserializeValue<RespawnableState>(bytes, DataFormat.Binary);

        //Restore Date
        isRemoved = respawnableState.isPicked;
        removedDate = new DateTime(respawnableState.pickedYear, respawnableState.pickedMonth, respawnableState.pickedDay);

        Respawn(associatedGameObject);
    }
}
