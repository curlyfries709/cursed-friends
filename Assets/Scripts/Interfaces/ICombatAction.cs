using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICombatAction 
{
    public bool isActive { get; set;}

    //Abstract funcs
    public void BeginAction();

    //public void PauseAction();
    //public void ContinueAction();
    //public void CancelAction();

    public void DisplayUnitHealthUIComplete();
    public void EndAction();

    //HELPERS
    public void ActionAnimEventRaised(GridUnitAnimNotifies.EventType eventType)
    {
        switch (eventType)
        {
            case GridUnitAnimNotifies.EventType.Evade:
                Evade.Instance.TriggerEvadeEvent?.Invoke(isActive);
                break;
            case GridUnitAnimNotifies.EventType.EvadeAndDamage:
                Evade.Instance.TriggerEvadeEvent?.Invoke(isActive);
                goto default;
            default:
                Health.RaiseHealthChangeEvent(isActive);
                break;

        }
    }
}
