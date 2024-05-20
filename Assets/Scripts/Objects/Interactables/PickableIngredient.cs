using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.Serialization;
using MoreMountains.Feedbacks;

public class PickableIngredient : Interact, ISaveable
{
    [Header("Ingredient")]
    [SerializeField] Ingredient ingredient;
    [SerializeField] int daysToRespawn = 5;
    [Space(10)]
    [SerializeField] MMF_Player pickupFeedback;

    //Saving Data
    [SerializeField, HideInInspector]
    private IngredientState ingredientState = new IngredientState();
    public bool AutoRestoreOnNewTerritoryEntry { get; set; } = true;

    bool picked = false;
    DateTime pickedDate;


    public override void HandleInteraction(bool inCombat)
    {
        if (inCombat) { return; }
        OnPick();
    }


    private void OnPick()
    {
        pickupFeedback?.PlayFeedbacks();

        InventoryManager.Instance.AddToInventory(PartyData.Instance.GetLeader(), ingredient);
        picked = true;

        //Set Picked Date
        pickedDate = StoryManager.Instance.currentDate;

        gameObject.SetActive(false);
    }

    private void Respawn()
    {
        if (StoryManager.Instance.GetDaysPassed(pickedDate) >= daysToRespawn || !picked)
        {
            picked = false;
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
        ingredientState.pickedDay = pickedDate.Day;
        ingredientState.pickedMonth = pickedDate.Month;
        ingredientState.pickedYear = pickedDate.Year;

        ingredientState.isPicked = picked;


        return SerializationUtility.SerializeValue(ingredientState, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        if (state == null) { return; }

        byte[] bytes = state as byte[];
        ingredientState = SerializationUtility.DeserializeValue<IngredientState>(bytes, DataFormat.Binary);

        //Restore Date
        picked = ingredientState.isPicked;
        pickedDate = new DateTime(ingredientState.pickedYear, ingredientState.pickedMonth, ingredientState.pickedDay);

        Respawn();
    }
}
