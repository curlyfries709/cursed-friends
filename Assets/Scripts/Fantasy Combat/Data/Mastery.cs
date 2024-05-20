using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


[CreateAssetMenu(fileName = "New Mastery", menuName = "Configs/Mastery/Mastery", order = 0)]
public class Mastery : ScriptableObject
{
    public Attribute masteryAttribute;
    [Space(10)]
    [ListDrawerSettings(Expanded = true, ShowIndexLabels = true)]
    public List<MasteryProgression> sequencedProgressions;

}
