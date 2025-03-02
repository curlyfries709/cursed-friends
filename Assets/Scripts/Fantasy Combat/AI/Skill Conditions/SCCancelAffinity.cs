using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SCCancelAffinity : MultiAttackCancellationCondition
{
    [SerializeField] List<Affinity> affinitiesToCancel = new List<Affinity>();

    public override bool IsConditionMet(GridUnit attacker, GridUnit target, DamageData targetCalculateDamageData)
    {
        Affinity targetAffinity = targetCalculateDamageData.affinityToAttack;
        return affinitiesToCancel.Contains(targetAffinity);
    }
}
