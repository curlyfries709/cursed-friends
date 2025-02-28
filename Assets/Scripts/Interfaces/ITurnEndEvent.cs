using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface ITurnEndEvent
{
    void PlayTurnEndEvent();
    void OnEventCancelled(); //Called when another turn end event cancels it.

    public float GetTurnEndEventOrder();

    List<System.Type> GetEventTypesThatCancelThis();
}
