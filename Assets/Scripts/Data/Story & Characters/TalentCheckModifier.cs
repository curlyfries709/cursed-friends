using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TalentCheckModifier
{
    public EventCondition condition;
    [Range(-95, 95)]
    public int percentageModifier;
}
