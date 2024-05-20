using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Strategic Bonus", menuName = "Configs/Strategic Bonus", order = 1)]
public class StrategicBonus : ScriptableObject
{
    public string bonusName;
    [TextArea(3, 7)]
    public string definition;
    [Range(5, 100)]
    [Tooltip("How Much percentage of Total EXP to give for achieving this bonus")]
    public int expMultiplier = 5;
}
