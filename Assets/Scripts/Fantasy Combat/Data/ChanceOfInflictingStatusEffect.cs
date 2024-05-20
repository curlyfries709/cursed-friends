using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChanceOfInflictingStatusEffect
{
    [Range(0, 100)]
    public int bonusPercentOfInflictingEffect = 0;
    public StatusEffectData statusEffect;
    [Tooltip("Only applies Potions & Orbs. Effects inflicted by character's have duration based on inflictor's wisdom")]
    public int numOfTurns;
    [Tooltip("-2: Super Debuff; -1: Debuff; 0: NO BUFF; 1: buff; 2: Super Buff")]
    [Range(-2, 2)]
    public int buffChange = 0;
}
