using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITurnStartEvent
{
    public int turnStartEventOrder { get; set; }
    void PlayTurnStartEvent();

    public void EventComplete()
    {
        FantasyCombatManager.Instance.StartTurnOrPlayEvent();
    }
}
