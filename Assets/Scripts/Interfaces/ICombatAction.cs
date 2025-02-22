using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICombatAction 
{
    public bool isActive { get; set;}

    public void ActionAnimEventRaised(GridUnitAnimNotifies.EventType eventType)
    {
        if (eventType == GridUnitAnimNotifies.EventType.Evade || eventType == GridUnitAnimNotifies.EventType.EvadeAndDamage)
        {
            Evade.Instance.TriggerEvadeEvent?.Invoke(isActive);
        }

        if (eventType == GridUnitAnimNotifies.EventType.Damage || eventType == GridUnitAnimNotifies.EventType.EvadeAndDamage)
        {
            IDamageable.RaiseHealthChangeEvent(isActive);
        }
    }


}
