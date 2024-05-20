using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CAEmpowerTurnEnd : BaseCardAbility
{
    public override void OnTrigger()
    {
        //Empower Self
        myFieldCard.Empower(configurableVariable);

        //Continue
        CardGameManager.Instance.OnTurnEndAbilityComplete();
    }

    public override bool IsTargetValid(BasePlayableCard target){ /* Doesn't require Target Selection */ return true; }

    protected override bool CanTriggerAbility(){ return true; }
}
