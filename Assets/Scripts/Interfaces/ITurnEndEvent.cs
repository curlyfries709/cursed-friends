using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface ITurnEndEvent
{
    public int turnEndEventOrder { get; set; }
    void PlayTurnEndEvent();
    void OnEventCancelled(); //Called when another turn end event cancels it.

    List<System.Type> GetEventTypesThatCancelThis();
}
