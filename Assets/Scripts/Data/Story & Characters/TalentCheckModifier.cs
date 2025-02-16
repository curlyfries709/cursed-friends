using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TalentCheckModifier
{
    public EventCondition condition;
    [Range(-95, 95)]
    [Tooltip("For Level checks, use values between - 12 to 12. For Roll checks, use values between -95 to 95")]
    public int valueModifier;
}
