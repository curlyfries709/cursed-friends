using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ConfirmationBorder : TerritoryBorder
{
    [Header("Confirmation Border Config")]
    [SerializeField] ChoiceReferences choiceRefRequiredToContinue;
    [Space(10)]
    [SerializeField] UnityEvent eventToTriggerOnConfirmation;


    protected override void OnEntryDialogueEndEvent()
    {
        //Unsubscribe
        DialogueManager.Instance.DialogueEnded -= OnEntryDialogueEndEvent;
        
        if (StoryManager.Instance.MadeDecision(choiceRefRequiredToContinue))
        {
            gameObject.SetActive(false);
            eventToTriggerOnConfirmation?.Invoke();
        }
        else
        {
            MovePlayerAway();
        }
    }
}
