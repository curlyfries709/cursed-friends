using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CombatAction : MonoBehaviour
{
    //State Variables
    public bool isActionActive { get; set;}

    //Storage
    protected List<GridUnit> actionTargets { get; private set; } = new List<GridUnit>();

    //Counters
    protected int numOfHealthUIDisplay = 0;
    protected int healthUIDisplayedCounter = 0;

    protected bool anyTargetsWithReflectAffinity = false;

    //Abstract funcs


    //public void PauseAction();
    //public void ContinueAction();
    //public void CancelAction();

    public virtual void BeginAction()
    {
        FantasyCombatManager.Instance.BeginAction(this);

        if (IsTactic())
        {
            FantasyCombatManager.Instance.GetActiveUnit().UsedTactic();
        }
    }

    public virtual void DisplayUnitHealthUIComplete()
    {
        if (!isActionActive || !ListenForUnitHealthUIComplete()) { return; }

        int skillReflectIncrement = 1;

        if (this is ITeamSkill teamSkill)
        {
            skillReflectIncrement = teamSkill.GetAttackers().Count;
        }

        int totalToCheck = anyTargetsWithReflectAffinity ? numOfHealthUIDisplay + skillReflectIncrement : numOfHealthUIDisplay;

        healthUIDisplayedCounter++;

        Debug.Log("Health UI Complete Count: " + healthUIDisplayedCounter + " Total: " + totalToCheck);

        if (healthUIDisplayedCounter >= totalToCheck)
        {
            OnAllHealthUIComplete();

            //Reset health Data
            ResetHealthUIData();
        }
    }
    protected virtual void OnAllHealthUIComplete()
    {
        EndAction();
    }

    public virtual void EndAction()
    {
        ResetData();
        FantasyCombatManager.Instance.ActionComplete?.Invoke(this);
    }

    protected void SetActionTargets(List<GridUnit> targets)
    {
        actionTargets = new List<GridUnit>(targets);
        numOfHealthUIDisplay = AffectTargetsIndividually() ? 1 : actionTargets.Count;
    }

    protected virtual void ResetData()
    {
        ResetHealthUIData();
        actionTargets.Clear();
    }

    protected void ResetHealthUIData()
    {
        numOfHealthUIDisplay = 0;
        healthUIDisplayedCounter = 0;
        anyTargetsWithReflectAffinity = false;
    }


    //FEEDBACK EVENTS
    public void RaiseHealthChangeEvent(int eventType) //Helpful event for feedbacks to raise. 
    {
        ActionAnimEventRaised((GridUnitAnimNotifies.EventType)eventType);
    }
    //END FEEDBACKS

    //HELPERS
    public void ActionAnimEventRaised(GridUnitAnimNotifies.EventType eventType)
    {
        switch (eventType)
        {
            case GridUnitAnimNotifies.EventType.Evade:
                Evade.Instance.TriggerEvadeEvent?.Invoke(isActionActive);
                break;
            case GridUnitAnimNotifies.EventType.EvadeAndDamage:
                Evade.Instance.TriggerEvadeEvent?.Invoke(isActionActive);
                goto default;
            default:
                Health.RaiseHealthChangeEvent(isActionActive);
                break;

        }
    }

    //GETTERS
    public virtual bool AffectTargetsIndividually()
    {
        return false;
    }

    protected virtual bool ListenForUnitHealthUIComplete()
    {
        return true;
    }

    public virtual bool IsTactic()
    {
        return false;
    }
}
