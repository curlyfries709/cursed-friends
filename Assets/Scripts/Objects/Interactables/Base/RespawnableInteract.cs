using System;
using Sirenix.Serialization;
using UnityEngine;

public abstract class RespawnableInteract : Interact, ISaveable
{
    [Header("Respawn")]
    [SerializeField] protected int daysToRespawn = 5;

    //Respawn Data
    protected bool isRemoved = false;
    protected DateTime removedDate;

    //Saving Data
    [SerializeField, HideInInspector]
    private IngredientState ingredientState = new IngredientState();

    protected bool isDataRestored = false;

    protected virtual void OnRemovedFromRealm(bool deactiveImmediately)
    {
        isRemoved = true;

        //Set Picked Date
        removedDate = CalendarManager.Instance.currentDate;

        if(deactiveImmediately)
            gameObject.SetActive(false);
    }

    protected void Respawn()
    {
        if (!isRemoved || CalendarManager.Instance.GetDaysPassed(removedDate) >= daysToRespawn)
        {
            isRemoved = false;
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    //Saving
    [System.Serializable]
    public class IngredientState
    {
        public bool isPicked = false;

        //Date
        public int pickedDay;
        public int pickedMonth;
        public int pickedYear;
    }

    public object CaptureState()
    {
        ingredientState.pickedDay = removedDate.Day;
        ingredientState.pickedMonth = removedDate.Month;
        ingredientState.pickedYear = removedDate.Year;

        ingredientState.isPicked = isRemoved;


        return SerializationUtility.SerializeValue(ingredientState, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        isDataRestored = true;

        if (state == null) { return; }

        byte[] bytes = state as byte[];
        ingredientState = SerializationUtility.DeserializeValue<IngredientState>(bytes, DataFormat.Binary);

        //Restore Date
        isRemoved = ingredientState.isPicked;
        removedDate = new DateTime(ingredientState.pickedYear, ingredientState.pickedMonth, ingredientState.pickedDay);

        Respawn();
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
