using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CADeployDamage : BaseCardAbility
{
    public override void OnTrigger()
    {
        if (!CanTriggerAbility())
        {
            Debug.Log("Could not trigger Ability of: " + this.ToString());
            myPlayableCard.TriggerDeployAbility(); 
            return;
        }

        //Subscribe to Event
        CardGameManager.Instance.ValidTargetSelected += OnTargetSelected;

        //Enable Field Selection
        CardGameManager.Instance.BeginFieldSelection(this);
    }

    protected void OnTargetSelected(BasePlayableCard selectedCard)
    {
        FieldCard fieldCard = selectedCard as FieldCard;

        //Unsubscribe self from Event
        CardGameManager.Instance.ValidTargetSelected -= OnTargetSelected;

        //Deal Damage
        fieldCard.TakeDamage(configurableVariable);

        //End of ability, trigger next if any
        myPlayableCard.TriggerDeployAbility();
    }

    protected override bool CanTriggerAbility()
    {
        return GetValidTargets().Count > 0;
    }

    public override bool IsTargetValid(BasePlayableCard target)
    {
        return IsValidFieldCard(target);

    }


}
