using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Talent", menuName = "Configs/Talent", order = 6)]
public class Talent : ScriptableObject
{
    public string talentName;
    [Space(10)]
    [TextArea(5, 10)]
    public string talentDescription;
}
