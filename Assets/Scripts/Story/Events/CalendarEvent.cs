using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalendarEvent : MonoBehaviour
{
    [SerializeField] int eventPriority;

    public void TriggerEvent()
    {

    }


    public int GetPriority()
    {
        return eventPriority;
    }
}
