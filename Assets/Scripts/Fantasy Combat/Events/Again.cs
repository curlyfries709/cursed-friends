using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Again : MonoBehaviour, ITurnEndEvent
{
    public static Again Instance { get; private set; }

    [SerializeField] CombatEventCanvas againCanvas;
    [Space(5)]
    [SerializeField] float againCanvasDisplayTime;

    CharacterGridUnit unitGoingAgain;

    //Event
    public Action<CharacterGridUnit> UnitGoingAgain;

    private void Awake()
    {
        Instance = this;
        againCanvas.SetDuration(againCanvasDisplayTime);
    }

    public void SetUnitToGoAgain(CharacterGridUnit unit)
    {
        if (StatusEffectManager.Instance.IsUnitDisabled(unit) || unit.CharacterHealth().isKOed) { return; }
        unitGoingAgain = unit;
        FantasyCombatManager.Instance.AddTurnEndEventToQueue(this);
    }

    public void PlayTurnEndEvent()
    {
        if (StatusEffectManager.Instance.IsUnitDisabled(unitGoingAgain) || unitGoingAgain.CharacterHealth().isKOed)
        {
            //Could Be KOed due to counterattack. Could be disabled due to Status Effect from counterattack.
            FantasyCombatManager.Instance.ActionComplete();
        }
        else
        {
            StartCoroutine(GoAgainRoutine());
        } 
    }

    IEnumerator GoAgainRoutine()
    {
        againCanvas.Show(true);
        yield return new WaitForSeconds(againCanvasDisplayTime);
        UnitGoingAgain?.Invoke(unitGoingAgain);
        BeginAgainTurn();
    }


    private void BeginAgainTurn()
    {
        FantasyCombatManager.Instance.BeginGoAgainTurn(unitGoingAgain);
    }

    public List<Type> GetEventTypesThatCancelThis()
    {
        return new List<Type>();
    }

    public float GetTurnEndEventOrder()
    {
        return transform.GetSiblingIndex();
    }

    public void OnEventCancelled()
    {
        //Event Cannot be cancelled so do nothing
        Debug.Log("AGAIN EVENT CANCELLED!");
    }

}
